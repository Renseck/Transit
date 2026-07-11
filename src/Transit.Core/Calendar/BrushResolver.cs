namespace Transit.Core.Calendar;

public enum BrushOutcome { Set, Clear }

public static class BrushResolver
{
    // Painting a day that already holds the active location clears it; otherwise (re)paints.
    public static BrushOutcome Resolve(int? currentLocationId, int activeLocationId)
        => currentLocationId == activeLocationId ? BrushOutcome.Clear : BrushOutcome.Set;
}