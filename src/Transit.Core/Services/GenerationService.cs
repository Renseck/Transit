using Transit.Core.Abstractions;
using Transit.Core.Declarations;
using Transit.Core.Entities;

namespace Transit.Core.Services;

/// <summary>
/// Orchestrates a generate: active profile → month draft → fill xlsx → convert pdf. Persists
/// nothing (slice 5 owns durable artifacts + run history). PDF failure is non-fatal — the xlsx
/// is still returned with a null PdfBytes.
/// </summary>
public sealed class GenerationService
{
    private readonly ITemplateProfileStore _profiles;
    private readonly DeclarationService _declarations;
    private readonly IExcelWriter _writer;
    private readonly IPdfConverter _pdf;

    public GenerationService(
        ITemplateProfileStore profiles, DeclarationService declarations,
        IExcelWriter writer, IPdfConverter pdf)
    {
        _profiles = profiles;
        _declarations = declarations;
        _writer = writer;
        _pdf = pdf;
    }

    public async Task<GeneratedDeclaration> GenerateAsync(int year, int month, CancellationToken ct = default)
    {
        var profile = await _profiles.GetActiveAsync(ct)
            ?? throw new InvalidOperationException("No active template. Set one up on the Template page first.");

        var draft = await _declarations.BuildDraftAsync(year, month, ct);

        await using var templateBytes = await _profiles.OpenTemplateAsync(profile.Id, ct);
        var xlsx = await _writer.FillAsync(profile, templateBytes, draft, ct);

        var stem = BuildFileStem(profile, year, month);
        var pdf = await TryConvertAsync(xlsx, ct);

        return new GeneratedDeclaration(xlsx, pdf, $"{stem}.xlsx", $"{stem}.pdf");
    }

    // e.g. "declaration-RvE-2026-07"; drops the initials segment when no employee name is mapped.
    private static string BuildFileStem(TemplateProfile profile, int year, int month)
    {
        var initials = DeriveInitials(profile);
        var period = $"{year:D4}-{month:D2}";
        return initials is null ? $"declaration-{period}" : $"declaration-{initials}-{period}";
    }

    // Initials from the configured employee-name header cell: "Rens van Eck" -> "RvE".
    private static string? DeriveInitials(TemplateProfile profile)
    {
        var name = profile.CellMappings
            .FirstOrDefault(c => c.Source == TemplateCellSource.EmployeeName && !string.IsNullOrWhiteSpace(c.Value))
            ?.Value;
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var initials = string.Concat(
            name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(word => word[0]));
        return initials.Length == 0 ? null : initials;
    }

    private async Task<byte[]?> TryConvertAsync(byte[] xlsx, CancellationToken ct)
    {
        try
        {
            return await _pdf.ConvertToPdfAsync(xlsx, ct);
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidOperationException or TimeoutException)
        {
            // PDF is best-effort; the caller surfaces the reason and still offers the xlsx.
            return null;
        }
    }
}