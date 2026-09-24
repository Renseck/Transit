using System.Globalization;
using Transit.Core.Entities;

namespace Transit.Core.Email;

/// <summary>
/// Pure token replacement for the declaration email: a fixed token set, plain string replaces, no
/// parser. Unknown tokens stay as typed so a typo shows in the preview; {naam} stays visible when
/// no employee name is configured rather than silently vanishing.
/// </summary>
public static class EmailComposer
{
    private static readonly CultureInfo Dutch = CultureInfo.GetCultureInfo("nl-NL");

    public static string Render(string template, int year, int month, string? employeeName)
    {
        var monthName = new DateTime(year, month, 1).ToString("MMMM", Dutch);
        var rendered = template
            .Replace("{maand}", monthName)
            .Replace("{Maand}", Dutch.TextInfo.ToTitleCase(monthName))
            .Replace("{jaar}", year.ToString(CultureInfo.InvariantCulture));

        return employeeName is null ? rendered : rendered.Replace("{naam}", employeeName);
    }

    public static EmailDraft Compose(AppSettings settings, int year, int month, string? employeeName)
    {
        var to = Recipient(settings.EmailRecipient);
        var subject = Render(settings.EmailSubjectTemplate, year, month, employeeName);
        var body = ToCrLf(Render(settings.EmailBodyTemplate, year, month, employeeName));

        var mailto = $"mailto:{to}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";
        return new EmailDraft(to, subject, body, mailto);
    }

    // Whitespace is never valid in an address; stripping it also keeps the mailto link intact.
    private static string? Recipient(string? value)
    {
        var stripped = string.Concat((value ?? string.Empty).Where(c => !char.IsWhiteSpace(c)));
        return stripped.Length == 0 ? null : stripped;
    }

    private static string ToCrLf(string text) => text.Replace("\r\n", "\n").Replace("\n", "\r\n");
}
