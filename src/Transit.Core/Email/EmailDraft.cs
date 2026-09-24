namespace Transit.Core.Email;

// A rendered declaration email, ready to copy or open via the mailto link. Body uses CRLF line endings.
public sealed record EmailDraft(string? To, string Subject, string Body, string MailtoUri);
