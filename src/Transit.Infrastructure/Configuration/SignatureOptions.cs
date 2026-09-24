namespace Transit.Infrastructure.Configuration;

public sealed class SignatureOptions
{
    public const string SectionName = "Transit:Signature";

    // Font for SignatureName cells. Must be installed machine-wide: LibreOffice silently substitutes
    // a plain font when it is missing. Segoe Script ships with Windows.
    public string FontName { get; set; } = "Segoe Script";

    public double FontSize { get; set; } = 16;
}
