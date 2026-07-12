namespace Transit.Core.Abstractions;

public interface IPdfConverter
{
    // Converts an .xlsx byte payload to a .pdf byte payload. Throws on converter failure.
    Task<byte[]> ConvertToPdfAsync(byte[] xlsxBytes, CancellationToken ct = default);
}