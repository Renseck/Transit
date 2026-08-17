using Transit.Core.Declarations;
using Transit.Core.Entities;
using Xunit;

namespace Transit.Tests.Declarations;

public class TravelKmCalculatorTests
{
    private static WorkLocation Loc(int id, bool travel) => new()
    {
        Id = id, Name = $"L{id}", ColorHex = "#000000",
        CountsAsTravel = travel, IsDefaultForUnmarked = false,
    };

    private static Route Rt(int id, int toId, decimal outb, decimal ret, bool def = false) => new()
    {
        Id = id, Name = $"R{id}", FromLocationId = 1, ToLocationId = toId,
        OutboundKm = outb, ReturnKm = ret, IsDefault = def,
    };

    private static readonly IReadOnlyList<WorkLocation> Locations =
        [Loc(1, travel: false), Loc(2, travel: true), Loc(3, travel: true)];

    // Office(2) default route = 20+18; a second non-default route to Office is ignored.
    private static readonly IReadOnlyList<Route> Routes =
        [Rt(10, toId: 2, 5m, 5m), Rt(11, toId: 2, 20m, 18m, def: true)];

    [Fact]
    public void Sums_default_route_total_for_travel_marks_in_the_month()
    {
        var marks = new Dictionary<DateOnly, int>
        {
            [new DateOnly(2026, 8, 4)] = 2,   // Office travel
            [new DateOnly(2026, 8, 6)] = 2,   // Office travel
            [new DateOnly(2026, 8, 5)] = 1,   // Home, non-travel -> 0
        };

        var result = TravelKmCalculator.ForMarks(marks, 2026, 8, Locations, Routes);

        Assert.Equal(76m, result.TotalKm);       // 2 * (20+18)
        Assert.Equal(2, result.TravelDayCount);
    }

    [Fact]
    public void Ignores_marks_outside_the_target_month()
    {
        var marks = new Dictionary<DateOnly, int>
        {
            [new DateOnly(2026, 8, 31)] = 2,  // in month
            [new DateOnly(2026, 9, 1)] = 2,   // next month, excluded
            [new DateOnly(2026, 7, 31)] = 2,  // prev month, excluded
        };

        var result = TravelKmCalculator.ForMarks(marks, 2026, 8, Locations, Routes);

        Assert.Equal(38m, result.TotalKm);
        Assert.Equal(1, result.TravelDayCount);
    }

    [Fact]
    public void Travel_day_with_no_route_counts_but_adds_zero_km()
    {
        var marks = new Dictionary<DateOnly, int> { [new DateOnly(2026, 8, 4)] = 3 }; // Site(3), no route

        var result = TravelKmCalculator.ForMarks(marks, 2026, 8, Locations, Routes);

        Assert.Equal(0m, result.TotalKm);
        Assert.Equal(1, result.TravelDayCount);
    }

    [Fact]
    public void Empty_marks_yield_zero()
    {
        var result = TravelKmCalculator.ForMarks(
            new Dictionary<DateOnly, int>(), 2026, 8, Locations, Routes);

        Assert.Equal(0m, result.TotalKm);
        Assert.Equal(0, result.TravelDayCount);
    }
}