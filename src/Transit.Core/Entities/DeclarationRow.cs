namespace Transit.Core.Entities;

// A frozen row of a DeclarationRun — the snapshot of one reviewed DeclarationDraftRow. All reviewed
// rows are frozen (travel + non-travel); only CountsAsTravel rows were written to the sheet.
public class DeclarationRow
{
    public int Id { get; set; }
    public int DeclarationRunId { get; set; }

    public DateOnly Date { get; set; }
    public required string WeekdayName { get; set; }
    public int WorkLocationId { get; set; }
    public required string LocationName { get; set; }
    public required string ColorHex { get; set; }
    public bool CountsAsTravel { get; set; }
    public bool IsUnmarkedDefault { get; set; }

    public int? RouteId { get; set; }
    public string? RouteName { get; set; }               // frozen as a string (slice-3 decision)
    public string? FromLocationName { get; set; }
    public string? FromPostcode { get; set; }
    public string? ToLocationName { get; set; }
    public string? ToPostcode { get; set; }

    public decimal OutboundKm { get; set; }
    public decimal ReturnKm { get; set; }
    public decimal Kilometers { get; set; }
    public bool HasNoRoute { get; set; }
}