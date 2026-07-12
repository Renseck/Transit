using Transit.Core.Abstractions;
using Transit.Core.Declarations;

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

        var stem = $"declaration-{year:D4}-{month:D2}";
        var pdf = await TryConvertAsync(xlsx, ct);

        return new GeneratedDeclaration(xlsx, pdf, $"{stem}.xlsx", $"{stem}.pdf");
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