using Transit.Core.Abstractions;
using Transit.Core.Email;
using Transit.Core.Entities;
using Transit.Core.Templates;

namespace Transit.Core.Services;

/// <summary>
/// Loads the email settings and the employee name (from the active template's EmployeeName cell),
/// then delegates to the pure <see cref="EmailComposer"/>. No logic of its own beyond loading.
/// </summary>
public sealed class DeclarationEmailService
{
    private readonly ISettingsStore _settings;
    private readonly ITemplateProfileStore _profiles;

    public DeclarationEmailService(ISettingsStore settings, ITemplateProfileStore profiles)
    {
        _settings = settings;
        _profiles = profiles;
    }

    // The email for a declared month, from the saved settings.
    public async Task<EmailDraft> ComposeAsync(int year, int month, CancellationToken ct = default)
        => await PreviewAsync(await _settings.GetAsync(ct), year, month, ct);

    // The email for unsaved settings (the /settings live preview).
    public async Task<EmailDraft> PreviewAsync(AppSettings settings, int year, int month, CancellationToken ct = default)
    {
        var profile = await _profiles.GetActiveAsync(ct);
        var employeeName = profile is null ? null : TemplateMapping.EmployeeName(profile.CellMappings);
        return EmailComposer.Compose(settings, year, month, employeeName);
    }
}
