using System.Net;
using System.Text;

namespace Transit.Core.Email;

/// <summary>
/// Builds an unsent .eml draft (RFC 5322 + MIME): the rendered email with the signed PDF attached.
/// <c>X-Unsent: 1</c> makes classic Outlook open it as an editable new message with a Send button.
/// The body is multipart/alternative (plain, then HTML): with an HTML part Outlook composes in HTML,
/// so formatting works and Insert → Signature adds the full signature, not its plain-text version.
/// Pure: bytes in, bytes out. Payloads are base64 (76-char lines), the subject is RFC 2047 UTF-8, and
/// header values are stripped of line breaks so nothing can inject a header.
/// </summary>
public static class EmlDraft
{
    private const string Crlf = "\r\n";
    private const string MixedBoundary = "transit-mixed-boundary";         // '-' never occurs in base64 payloads
    private const string AlternativeBoundary = "transit-alternative-boundary";

    // Without an explicit font Outlook renders HTML in Times New Roman; match its compose default.
    private const string HtmlStyle = "font-family:Aptos,Calibri,Arial,sans-serif;font-size:11pt";

    public static byte[] Build(EmailDraft draft, string pdfFileName, byte[] pdf)
    {
        var fileName = HeaderValue(pdfFileName).Replace("\"", "");
        var eml = new StringBuilder();

        eml.Append("X-Unsent: 1").Append(Crlf);
        if (!string.IsNullOrWhiteSpace(draft.To))
        {
            eml.Append("To: ").Append(HeaderValue(draft.To)).Append(Crlf);
        }

        eml.Append("Subject: =?utf-8?B?").Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(draft.Subject))).Append("?=").Append(Crlf);
        eml.Append("MIME-Version: 1.0").Append(Crlf);
        eml.Append("Content-Type: multipart/mixed; boundary=\"").Append(MixedBoundary).Append('"').Append(Crlf);
        eml.Append(Crlf);

        eml.Append("--").Append(MixedBoundary).Append(Crlf);
        eml.Append("Content-Type: multipart/alternative; boundary=\"").Append(AlternativeBoundary).Append('"').Append(Crlf);
        eml.Append(Crlf);
        AppendPart(eml, AlternativeBoundary, "text/plain; charset=\"utf-8\"", disposition: null, Encoding.UTF8.GetBytes(draft.Body));
        AppendPart(eml, AlternativeBoundary, "text/html; charset=\"utf-8\"", disposition: null, Encoding.UTF8.GetBytes(ToHtml(draft.Body)));
        eml.Append("--").Append(AlternativeBoundary).Append("--").Append(Crlf);

        AppendPart(eml, MixedBoundary, $"application/pdf; name=\"{fileName}\"", $"attachment; filename=\"{fileName}\"", pdf);
        eml.Append("--").Append(MixedBoundary).Append("--").Append(Crlf);
        return Encoding.ASCII.GetBytes(eml.ToString());
    }

    // One zero-margin paragraph per line (Outlook's own compose markup); blank lines keep their height.
    private static string ToHtml(string body)
    {
        var paragraphs = body.Replace("\r\n", "\n").TrimEnd('\n').Split('\n')
            .Select(line => line.Length == 0 ? "&nbsp;" : WebUtility.HtmlEncode(line))
            .Select(content => $"<p style=\"margin:0\">{content}</p>");

        return $"<html><head><meta charset=\"utf-8\"></head><body><div style=\"{HtmlStyle}\">"
            + string.Concat(paragraphs)
            + "</div></body></html>";
    }

    private static void AppendPart(StringBuilder eml, string boundary, string contentType, string? disposition, byte[] content)
    {
        eml.Append("--").Append(boundary).Append(Crlf);
        eml.Append("Content-Type: ").Append(contentType).Append(Crlf);
        if (disposition is not null)
        {
            eml.Append("Content-Disposition: ").Append(disposition).Append(Crlf);
        }

        eml.Append("Content-Transfer-Encoding: base64").Append(Crlf);
        eml.Append(Crlf);
        eml.Append(Convert.ToBase64String(content, Base64FormattingOptions.InsertLineBreaks)).Append(Crlf);
    }

    private static string HeaderValue(string value) => value.Replace("\r", "").Replace("\n", "").Trim();
}
