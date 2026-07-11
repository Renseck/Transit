using Transit.Core.Calendar;
using Xunit;

namespace Transit.Tests.Calendar;

public class BrushResolverTests
{
    [Fact]
    public void Clicking_a_day_holding_the_active_location_clears_it()
        => Assert.Equal(BrushOutcome.Clear, BrushResolver.Resolve(currentLocationId: 3, activeLocationId: 3));

    [Fact]
    public void Clicking_a_day_holding_a_different_location_repaints_it()
        => Assert.Equal(BrushOutcome.Set, BrushResolver.Resolve(currentLocationId: 3, activeLocationId: 5));

    [Fact]
    public void Clicking_an_unmarked_day_paints_it()
        => Assert.Equal(BrushOutcome.Set, BrushResolver.Resolve(currentLocationId: null, activeLocationId: 5));
}