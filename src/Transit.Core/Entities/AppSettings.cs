namespace Transit.Core.Entities;

// App-wide user settings: a single row (Id = 1). Defaults apply until the user saves.
// Email templates take the tokens {maand}, {Maand}, {jaar} and {naam} (see EmailComposer).
public class AppSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    public string? EmailRecipient { get; set; }

    public string EmailSubjectTemplate { get; set; } = "Reiskostendeclaratie {Maand} {jaar} {naam}";

    public string EmailBodyTemplate { get; set; } =
        "Beste,\n\nBij deze mijn reiskostendeclaratie voor {maand} {jaar}. De declaratie is als PDF bijgevoegd.\n\nMet vriendelijke groet,\n";
}
