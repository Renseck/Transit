using System.Text;
using Transit.Core.Email;
using Xunit;

namespace Transit.Tests.Email;

public class EmlDraftTests
{
    private static readonly byte[] Pdf = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x00, 0xFF];   // "%PDF-1" + binary

    private static EmailDraft Draft(string? to = "finance@example.com") => new(
        to, "Reiskostendeclaratie Augustus 2026 Rens van Eck", "Beste,\r\n\r\nBijgevoegd.\r\n", "mailto:");

    private static string Build(EmailDraft draft)
        => Encoding.ASCII.GetString(EmlDraft.Build(draft, "08_2026_reiskosten_rve.pdf", Pdf));

    // Headers end at the first blank line.
    private static string Headers(string eml) => eml[..eml.IndexOf("\r\n\r\n", StringComparison.Ordinal)];

    // The base64 payload of the MIME part whose headers contain marker.
    private static byte[] Part(string eml, string marker)
    {
        var start = eml.IndexOf(marker, StringComparison.Ordinal);
        var bodyStart = eml.IndexOf("\r\n\r\n", start, StringComparison.Ordinal) + 4;
        var bodyEnd = eml.IndexOf("\r\n--", bodyStart, StringComparison.Ordinal);
        return Convert.FromBase64String(eml[bodyStart..bodyEnd].Replace("\r\n", ""));
    }

    [Fact]
    public void Is_an_unsent_draft_so_outlook_opens_it_for_editing()
        => Assert.StartsWith("X-Unsent: 1\r\n", Build(Draft()));

    [Fact]
    public void Carries_the_recipient()
        => Assert.Contains("\r\nTo: finance@example.com\r\n", Headers(Build(Draft())));

    [Fact]
    public void Omits_the_to_header_without_a_recipient()
        => Assert.DoesNotContain("To:", Headers(Build(Draft(to: null))));

    [Fact]
    public void Subject_is_rfc2047_utf8_encoded()
    {
        var subject = Headers(Build(Draft())).Split("\r\n").Single(l => l.StartsWith("Subject: "));
        var encoded = subject["Subject: =?utf-8?B?".Length..^"?=".Length];
        Assert.Equal("Reiskostendeclaratie Augustus 2026 Rens van Eck", Encoding.UTF8.GetString(Convert.FromBase64String(encoded)));
    }

    [Fact]
    public void Body_round_trips_as_utf8_text()
        => Assert.Equal("Beste,\r\n\r\nBijgevoegd.\r\n",
            Encoding.UTF8.GetString(Part(Build(Draft()), "Content-Type: text/plain")));

    [Fact]
    public void Body_has_an_html_alternative_so_outlook_composes_in_html()
    {
        var eml = Build(Draft());

        Assert.Contains("Content-Type: multipart/alternative;", eml);
        var html = Encoding.UTF8.GetString(Part(eml, "Content-Type: text/html"));
        Assert.Contains("font-family:Aptos", html);
        Assert.Contains("<p style=\"margin:0\">Beste,</p>", html);
        Assert.Contains("<p style=\"margin:0\">&nbsp;</p>", html);   // the blank line survives
        Assert.Contains("<p style=\"margin:0\">Bijgevoegd.</p>", html);
    }

    [Fact]
    public void Plain_text_comes_before_html_so_html_is_the_preferred_alternative()
    {
        var eml = Build(Draft());
        Assert.True(eml.IndexOf("Content-Type: text/plain", StringComparison.Ordinal)
            < eml.IndexOf("Content-Type: text/html", StringComparison.Ordinal));
    }

    [Fact]
    public void Html_body_escapes_markup_from_the_template()
    {
        var draft = Draft() with { Body = "Kosten <10 km> & meer" };
        var html = Encoding.UTF8.GetString(Part(Build(draft), "Content-Type: text/html"));
        Assert.Contains("Kosten &lt;10 km&gt; &amp; meer", html);
    }

    [Fact]
    public void Pdf_is_attached_byte_for_byte_under_its_file_name()
    {
        var eml = Build(Draft());

        Assert.Contains("Content-Disposition: attachment; filename=\"08_2026_reiskosten_rve.pdf\"", eml);
        Assert.Equal(Pdf, Part(eml, "Content-Type: application/pdf"));
    }

    [Fact]
    public void Base64_lines_stay_within_76_characters()
    {
        var eml = Encoding.ASCII.GetString(EmlDraft.Build(Draft(), "x.pdf", new byte[5000]));
        Assert.All(eml.Split("\r\n"), line => Assert.True(line.Length <= 998));
        Assert.DoesNotContain(eml.Split("\r\n"), line => line.Length > 76 && !line.Contains(':'));
    }

    [Fact]
    public void Line_breaks_in_header_values_cannot_inject_headers()
    {
        var eml = Build(Draft(to: "finance@example.com\r\nBcc: someone@example.com"));
        Assert.DoesNotContain("\r\nBcc:", Headers(eml));
    }
}
