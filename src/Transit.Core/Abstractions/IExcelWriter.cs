using Transit.Core.Declarations;
using Transit.Core.Entities;

namespace Transit.Core.Abstractions;

public interface IExcelWriter
{
    // Loads the uploaded template from templateBytes, writes the draft's travel rows and header
    // cells per the profile, and returns the filled workbook as bytes. Never rebuilds the workbook.
    Task<byte[]> FillAsync(
        TemplateProfile profile, Stream templateBytes, DeclarationDraft draft, CancellationToken ct = default);
}