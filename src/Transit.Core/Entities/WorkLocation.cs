namespace Transit.Core.Entities;

public class WorkLocation
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string ColorHex { get; set; }
    public bool CountsAsTravel { get; set; }
    public bool IsDefaultForUnmarked { get; set; }
}