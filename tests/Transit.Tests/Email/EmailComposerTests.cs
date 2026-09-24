using Transit.Core.Email;
using Transit.Core.Entities;
using Xunit;

namespace Transit.Tests.Email;

public class EmailComposerTests
{
    [Fact]
    public void Render_replaces_month_year_and_name_tokens()
        => Assert.Equal("Reiskostendeclaratie augustus 2026 Rens van Eck",
            EmailComposer.Render("Reiskostendeclaratie {maand} {jaar} {naam}", 2026, 8, "Rens van Eck"));

    [Fact]
    public void Render_capitalised_month_token_capitalises_the_month()
        => Assert.Equal("Augustus / augustus", EmailComposer.Render("{Maand} / {maand}", 2026, 8, "x"));

    [Fact]
    public void Render_leaves_unknown_tokens_intact()
        => Assert.Equal("{foo} 2026", EmailComposer.Render("{foo} {jaar}", 2026, 8, "x"));

    [Fact]
    public void Render_keeps_the_name_token_visible_when_no_name_is_configured()
        => Assert.Equal("augustus {naam}", EmailComposer.Render("{maand} {naam}", 2026, 8, employeeName: null));

    [Fact]
    public void Compose_renders_subject_and_body_and_trims_the_recipient()
    {
        var settings = new AppSettings
        {
            EmailRecipient = " finance@example.com ",
            EmailSubjectTemplate = "Declaratie {Maand} {jaar}",
            EmailBodyTemplate = "Beste,\n\nVoor {maand}.",
        };

        var draft = EmailComposer.Compose(settings, 2026, 8, "Rens van Eck");

        Assert.Equal("finance@example.com", draft.To);
        Assert.Equal("Declaratie Augustus 2026", draft.Subject);
        Assert.Equal("Beste,\r\n\r\nVoor augustus.", draft.Body);   // CRLF for Outlook
    }

    [Fact]
    public void Compose_mailto_percent_encodes_spaces_and_newlines()
    {
        var settings = new AppSettings
        {
            EmailRecipient = "finance@example.com",
            EmailSubjectTemplate = "Declaratie {Maand}",
            EmailBodyTemplate = "Beste,\nTot ziens & dank",
        };

        var draft = EmailComposer.Compose(settings, 2026, 8, null);

        Assert.Equal(
            "mailto:finance@example.com?subject=Declaratie%20Augustus&body=Beste%2C%0D%0ATot%20ziens%20%26%20dank",
            draft.MailtoUri);
    }

    [Fact]
    public void Compose_mailto_without_recipient_still_prefills_subject_and_body()
    {
        var settings = new AppSettings { EmailRecipient = null, EmailSubjectTemplate = "S", EmailBodyTemplate = "B" };
        Assert.Equal("mailto:?subject=S&body=B", EmailComposer.Compose(settings, 2026, 8, null).MailtoUri);
    }

    [Fact]
    public void Default_settings_carry_the_existing_email_wording()
    {
        var draft = EmailComposer.Compose(new AppSettings(), 2026, 8, "Rens van Eck");

        Assert.Equal("Reiskostendeclaratie Augustus 2026 Rens van Eck", draft.Subject);
        Assert.StartsWith("Beste,\r\n\r\nBij deze mijn reiskostendeclaratie voor augustus 2026.", draft.Body);
    }
}
