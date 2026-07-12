namespace Transit.Core.Entities;

public class Route
{
    public int Id { get; set; }
    public required string Name { get; set; }

    // A directed connection between two locations. Outbound = From -> To, Return = To -> From.
    public int FromLocationId { get; set; }
    public int ToLocationId { get; set; }

    public decimal OutboundKm { get; set; }
    public decimal ReturnKm { get; set; }

    // The default among routes sharing this ToLocationId (per-destination, not global).
    public bool IsDefault { get; set; }

    // Computed; not persisted (see TransitDbContext.Ignore).
    public decimal TotalKm => OutboundKm + ReturnKm;
}