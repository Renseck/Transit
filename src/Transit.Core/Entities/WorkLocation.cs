namespace Transit.Core.Entities;

public class WorkLocation
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string ColorHex { get; set; }
    public bool CountsAsTravel { get; set; }
    public bool IsDefaultForUnmarked { get; set; }

    // Soft-delete flag. Archived locations leave the brush bar and
    // active list but stay resolvable for existing marks and future run snapshots.
    public bool IsArchived { get; set; }

    // Brush-bar / list order. Lower sorts first; new locations append.
    public int DisplayOrder { get; set; }

    // Optional postcode of this place; feeds the template's van/naar postcode columns.
    public string? Postcode { get; set; }

    
    // Optional network name (Wi-Fi SSID) seen at this place; the reminder suggests this location when connected to it.
    public string? NetworkHint { get; set; }

}