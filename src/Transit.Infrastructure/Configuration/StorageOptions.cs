namespace Transit.Infrastructure.Configuration;

public sealed class StorageOptions
{
    public const string SectionName = "Transit";

    public string DataRoot { get; set; } = @"C:\ProgramData\Transit";

    public string DatabasePath => Path.Combine(DataRoot, "app.db");
}