namespace Transit.Core.Calendar;

public enum BrushOutcome { Set, Clear, NoOp }

public static class BrushResolver
{
    // A single click (press + release on one day). The eraser is a null active brush.
    // Toggle-to-clear applies: clicking a day's own location clears it.
    public static BrushOutcome ResolveClick(int? currentLocationId, int? activeLocationId)
    {
        if (activeLocationId is null)
        {
            return currentLocationId is null ? BrushOutcome.NoOp : BrushOutcome.Clear;
        }

        return currentLocationId == activeLocationId ? BrushOutcome.Clear : BrushOutcome.Set;
    }

    // A day entered mid-drag. No toggle: painting sets (idempotent), erasing clears.
    // Returns NoOp when the day already holds the target state (the drag-paint guard).
    public static BrushOutcome ResolveDrag(int? currentLocationId, int? activeLocationId)
    {
        if (activeLocationId is null)
        {
            return currentLocationId is null ? BrushOutcome.NoOp : BrushOutcome.Clear;
        }

        return currentLocationId == activeLocationId ? BrushOutcome.NoOp : BrushOutcome.Set;
    }
}