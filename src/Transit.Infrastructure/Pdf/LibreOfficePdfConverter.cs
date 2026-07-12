using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Transit.Core.Abstractions;
using Transit.Infrastructure.Configuration;

namespace Transit.Infrastructure.Pdf;

/// <summary>
/// Converts .xlsx to .pdf by shelling out to LibreOffice headless. Writes the input to a private
/// temp directory, runs `soffice --headless --convert-to pdf` with a per-conversion user profile
/// (so it never contends with another LibreOffice instance's lock and works under an IIS app-pool
/// identity with no roaming profile), reads the result, and cleans up.
/// </summary>
public sealed class LibreOfficePdfConverter : IPdfConverter
{
    private readonly PdfConverterOptions _options;
    private readonly ILogger<LibreOfficePdfConverter> _logger;

    public LibreOfficePdfConverter(
        IOptions<PdfConverterOptions> options, ILogger<LibreOfficePdfConverter> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<byte[]> ConvertToPdfAsync(byte[] xlsxBytes, CancellationToken ct = default)
    {
        if (!File.Exists(_options.SofficePath))
        {
            throw new FileNotFoundException(
                $"LibreOffice was not found at '{_options.SofficePath}'. Install it or set Transit:Pdf:SofficePath.");
        }

        var workDir = Path.Combine(Path.GetTempPath(), "transit-pdf", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        try
        {
            var inputPath = Path.Combine(workDir, "declaration.xlsx");
            await File.WriteAllBytesAsync(inputPath, xlsxBytes, ct);

            await RunSofficeAsync(inputPath, workDir, ct);

            var outputPath = Path.Combine(workDir, "declaration.pdf");
            if (!File.Exists(outputPath))
            {
                throw new InvalidOperationException("LibreOffice did not produce a PDF.");
            }

            return await File.ReadAllBytesAsync(outputPath, ct);
        }
        finally
        {
            if (Directory.Exists(workDir))
            {
                Directory.Delete(workDir, recursive: true);
            }
        }
    }

    private async Task RunSofficeAsync(string inputPath, string workDir, CancellationToken ct)
    {
        // Per-conversion profile: isolates this headless run from any other LibreOffice instance's
        // lock and gives it a writable profile even when the host account has no roaming profile.
        var profileUri = "file:///" + Path.Combine(workDir, "profile").Replace('\\', '/');

        var psi = new ProcessStartInfo
        {
            FileName = _options.SofficePath,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add($"-env:UserInstallation={profileUri}");
        psi.ArgumentList.Add("--headless");
        psi.ArgumentList.Add("--convert-to");
        psi.ArgumentList.Add("pdf");
        psi.ArgumentList.Add("--outdir");
        psi.ArgumentList.Add(workDir);
        psi.ArgumentList.Add(inputPath);

        using var process = new Process { StartInfo = psi };
        var sw = Stopwatch.StartNew();
        process.Start();

        // Drain both pipes concurrently *before* awaiting exit — an undrained redirected pipe that
        // fills its OS buffer would block soffice from exiting and deadlock the wait.
        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);
        try
        {
            await process.WaitForExitAsync(linked.Token);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            TryKill(process);
            _logger.LogError(
                "LibreOffice conversion timed out after {Timeout}s (soffice: {Path}).",
                _options.TimeoutSeconds, _options.SofficePath);
            throw new TimeoutException($"LibreOffice conversion exceeded {_options.TimeoutSeconds}s.");
        }

        var stdout = (await stdoutTask).Trim();
        var stderr = (await stderrTask).Trim();
        sw.Stop();

        _logger.LogInformation(
            "LibreOffice convert-to-pdf exited {ExitCode} in {Elapsed}ms. stdout=[{StdOut}] stderr=[{StdErr}]",
            process.ExitCode, sw.ElapsedMilliseconds, stdout, stderr);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"LibreOffice failed (exit {process.ExitCode}): {stderr}");
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // Process already gone — nothing to kill.
        }
    }
}