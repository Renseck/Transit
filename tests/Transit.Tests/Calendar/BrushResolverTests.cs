using Transit.Core.Calendar;
using Xunit;

namespace Transit.Tests.Calendar;

public class BrushResolverTests
{
    // --- Click (single press+release; toggle-to-clear applies) ---

    [Fact]
    public void Click_unmarked_day_with_a_location_paints_it()
        => Assert.Equal(BrushOutcome.Set, BrushResolver.ResolveClick(currentLocationId: null, activeLocationId: 5));

    [Fact]
    public void Click_day_holding_a_different_location_repaints_it()
        => Assert.Equal(BrushOutcome.Set, BrushResolver.ResolveClick(currentLocationId: 3, activeLocationId: 5));

    [Fact]
    public void Click_day_holding_the_active_location_clears_it()
        => Assert.Equal(BrushOutcome.Clear, BrushResolver.ResolveClick(currentLocationId: 3, activeLocationId: 3));

    [Fact]
    public void Click_marked_day_with_eraser_clears_it()
        => Assert.Equal(BrushOutcome.Clear, BrushResolver.ResolveClick(currentLocationId: 3, activeLocationId: null));

    [Fact]
    public void Click_unmarked_day_with_eraser_is_a_noop()
        => Assert.Equal(BrushOutcome.NoOp, BrushResolver.ResolveClick(currentLocationId: null, activeLocationId: null));

    // --- Drag (day entered mid-stroke; no toggle) ---

    [Fact]
    public void Drag_over_unmarked_day_with_a_location_paints_it()
        => Assert.Equal(BrushOutcome.Set, BrushResolver.ResolveDrag(currentLocationId: null, activeLocationId: 5));

    [Fact]
    public void Drag_over_day_already_holding_the_active_location_is_a_noop()
        => Assert.Equal(BrushOutcome.NoOp, BrushResolver.ResolveDrag(currentLocationId: 5, activeLocationId: 5));

    [Fact]
    public void Drag_over_day_holding_a_different_location_repaints_it()
        => Assert.Equal(BrushOutcome.Set, BrushResolver.ResolveDrag(currentLocationId: 3, activeLocationId: 5));

    [Fact]
    public void Drag_over_marked_day_with_eraser_clears_it()
        => Assert.Equal(BrushOutcome.Clear, BrushResolver.ResolveDrag(currentLocationId: 3, activeLocationId: null));

    [Fact]
    public void Drag_over_unmarked_day_with_eraser_is_a_noop()
        => Assert.Equal(BrushOutcome.NoOp, BrushResolver.ResolveDrag(currentLocationId: null, activeLocationId: null));
}