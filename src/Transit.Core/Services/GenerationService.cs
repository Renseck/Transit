using Transit.Core.Abstractions;
using Transit.Core.Declarations;
using Transit.Core.Entities;
using Transit.Core.Templates;

namespace Transit.Core.Services;

/// <summary>
/// Orchestrates a generate: active profile → reviewed draft (defaults + route overrides) → fill xlsx
/// → convert pdf → persist an append-only run + its artifacts via <see cref="DeclarationHistoryService"/>.
/// Only the PDF is signed: the template is filled twice from the same draft — unsigned for the stored
/// xlsx, signed with today's date for the PDF. Generating is the signing act; nothing signs unattended.
/// PDF failure is non-fatal — the xlsx is still returned and persisted with a null PdfBytes/PdfFilePath.
/// </summary>
public sealed class GenerationService
{
    private readonly ITemplateProfileStore _profiles;
    private readonly DeclarationService _declarations;
    private readonly IExcelWriter _writer;
    private readonly IPdfConverter _pdf;
    private readonly DeclarationHistoryService _history;
    private readonly TimeProvider _time;

    public GenerationService(
        ITemplateProfileStore profiles, DeclarationService declarations,
        IExcelWriter writer, IPdfConverter pdf, DeclarationHistoryService history, TimeProvider time)
    {
        _profiles = profiles;
        _declarations = declarations;
        _writer = writer;
        _pdf = pdf;
        _history = history;
        _time = time;
    }

    public async Task<GeneratedDeclaration> GenerateAsync(
        int year, int month, IReadOnlyDictionary<DateOnly, int?> routeSelections, CancellationToken ct = default)
    {
        var profile = await _profiles.GetActiveAsync(ct)
            ?? throw new InvalidOperationException("No active template. Set one up on the Template page first.");

        var draft = await _declarations.BuildReviewedDraftAsync(year, month, routeSelections, ct);

        var template = await ReadTemplateAsync(profile.Id, ct);
        var signedOn = DateOnly.FromDateTime(_time.GetLocalNow().DateTime);
        var xlsx = await _writer.FillAsync(profile, new MemoryStream(template), draft, ct: ct);
        var signedXlsx = await _writer.FillAsync(profile, new MemoryStream(template), draft, signedOn, ct);

        var stem = BuildFileStem(profile, year, month);
        var pdf = await TryConvertAsync(signedXlsx, ct);

        var run = await _history.SaveAsync(draft, profile, xlsx, pdf, ct);

        return new GeneratedDeclaration(xlsx, pdf, $"{stem}.xlsx", $"{stem}.pdf", run.Id);
    }

    // Buffered once: the template is filled twice and a stream reads only once.
    private async Task<byte[]> ReadTemplateAsync(int profileId, CancellationToken ct)
    {
        await using var stream = await _profiles.OpenTemplateAsync(profileId, ct);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);
        return buffer.ToArray();
    }

    // e.g. "07_2026_reiskosten_rve"; drops the initials segment when no employee name is mapped.
    private static string BuildFileStem(TemplateProfile profile, int year, int month)
    {
        var initials = DeriveInitials(profile);
        var period = $"{month:D2}_{year:D4}";
        return initials is null ? $"{period}_reiskosten" : $"{period}_reiskosten_{initials}";
    }

    // Initials from the configured employee-name header cell: "Rens van Eck" -> "rve".
    private static string? DeriveInitials(TemplateProfile profile)
    {
        var name = TemplateMapping.EmployeeName(profile.CellMappings);
        if (name is null)
        {
            return null;
        }

        var initials = string.Concat(
            name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(word => word[0]));
        return initials.Length == 0 ? null : initials.ToLowerInvariant();
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