using System.Diagnostics;
using Microsoft.Extensions.Options;
using Transit.Core.Abstractions;
using Transit.Infrastructure.Configuration;

namespace Transit.Infrastructure.Pdf;

/// <summary>
/// Converts .xlsx to .pdf by shelling out to LibreOffice headless. Writes the input to a private
/// temp directory, runs `soffice --headless --convert-to pdf`, reads the result, and cleans up.
/// </summary>
public sealed class LibreOfficePdfConverter : IPdfConverter
{
    private readonly PdfConverterOptions _options;

    public LibreOfficePdfConverter(IOptions<PdfConverterOptions> options) => _options = options.Value;

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
        var psi = new ProcessStartInfo
        {
            FileName = _options.SofficePath,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("--headless");
        psi.ArgumentList.Add("--convert-to");
        psi.ArgumentList.Add("pdf");
        psi.ArgumentList.Add("--outdir");
        psi.ArgumentList.Add(workDir);
        psi.ArgumentList.Add(inputPath);

        using var process = new Process { StartInfo = psi };
        process.Start();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);
        try
        {
            await process.WaitForExitAsync(linked.Token);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            TryKill(process);
            throw new TimeoutException($"LibreOffice conversion exceeded {_options.TimeoutSeconds}s.");
        }

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(ct);
            throw new InvalidOperationException($"LibreOffice failed (exit {process.ExitCode}): {error}");
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