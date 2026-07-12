using Transit.Core.Declarations;
using Transit.Core.Entities;
using Xunit;

namespace Transit.Tests.Declarations;

public class DeclarationBuilderTests
{
    // July 2026: the 1st is a Wednesday; it has 23 weekdays and 8 weekend days.
    private const int Y = 2026;
    private const int M = 7;
    private static readonly DateOnly Wed1 = new(2026, 7, 1);
    private static readonly DateOnly Sat4 = new(2026, 7, 4);

    private static WorkLocation Home() => new()
    {
        Id = 1, Name = "Home", ColorHex = "#4C566A", CountsAsTravel = false, IsDefaultForUnmarked = true,
    };

    private static WorkLocation Office() => new()
    {
        Id = 2, Name = "Office", ColorHex = "#3B5BDB", CountsAsTravel = true, IsDefaultForUnmarked = false,
    };

    private static Route HomeOffice(bool isDefault = true) => new()
    {
        Id = 10, Name = "Home → Office", FromLocationId = 1, ToLocationId = 2,
        OutboundKm = 11.6m, ReturnKm = 10.2m, IsDefault = isDefault,
    };

    [Fact]
    public void Materialises_every_weekday_and_omits_unmarked_weekends()
    {
        var draft = DeclarationBuilder.Build(Y, M, [], [Home(), Office()], []);
        Assert.Equal(23, draft.Rows.Count);
        Assert.All(draft.Rows, r => Assert.NotEqual(DayOfWeek.Saturday, r.Date.DayOfWeek));
        Assert.All(draft.Rows, r => Assert.NotEqual(DayOfWeek.Sunday, r.Date.DayOfWeek));
    }

    [Fact]
    public void Includes_an_explicitly_marked_weekend_day()
    {
        var entries = new[] { new AttendanceEntry { Date = Sat4, WorkLocationId = 2 } };
        var draft = DeclarationBuilder.Build(Y, M, entries, [Home(), Office()], [HomeOffice()]);
        Assert.Contains(draft.Rows, r => r.Date == Sat4);
    }

    [Fact]
    public void Unmarked_weekday_defaults_to_home_and_is_flagged()
    {
        var draft = DeclarationBuilder.Build(Y, M, [], [Home(), Office()], []);
        var row = draft.Rows.First(r => r.Date == Wed1);
        Assert.Equal(1, row.WorkLocationId);
        Assert.True(row.IsUnmarkedDefault);
        Assert.Equal(0m, row.Kilometers);
    }

    [Fact]
    public void Marked_day_uses_its_entry_and_is_not_flagged_default()
    {
        var entries = new[] { new AttendanceEntry { Date = Wed1, WorkLocationId = 2 } };
        var draft = DeclarationBuilder.Build(Y, M, entries, [Home(), Office()], [HomeOffice()]);
        var row = draft.Rows.First(r => r.Date == Wed1);
        Assert.Equal(2, row.WorkLocationId);
        Assert.False(row.IsUnmarkedDefault);
    }

    [Fact]
    public void Travel_day_with_default_route_sums_outbound_and_return()
    {
        var entries = new[] { new AttendanceEntry { Date = Wed1, WorkLocationId = 2 } };
        var draft = DeclarationBuilder.Build(Y, M, entries, [Home(), Office()], [HomeOffice()]);
        var row = draft.Rows.First(r => r.Date == Wed1);
        Assert.Equal(10, row.SelectedRouteId);
        Assert.Equal(11.6m, row.OutboundKm);
        Assert.Equal(10.2m, row.ReturnKm);
        Assert.Equal(21.8m, row.Kilometers);   // asymmetric legs summed
        Assert.False(row.HasNoRoute);
    }

    [Fact]
    public void Travel_day_with_no_matching_route_is_flagged_and_zero_km()
    {
        var entries = new[] { new AttendanceEntry { Date = Wed1, WorkLocationId = 2 } };
        var draft = DeclarationBuilder.Build(Y, M, entries, [Home(), Office()], []);
        var row = draft.Rows.First(r => r.Date == Wed1);
        Assert.True(row.HasNoRoute);
        Assert.Null(row.SelectedRouteId);
        Assert.Equal(0m, row.Kilometers);
    }

    [Fact]
    public void Non_travel_day_has_zero_km_and_no_route()
    {
        var draft = DeclarationBuilder.Build(Y, M, [], [Home(), Office()], [HomeOffice()]);
        var row = draft.Rows.First(r => r.Date == Wed1);   // defaults to Home (non-travel)
        Assert.False(row.HasNoRoute);
        Assert.Null(row.SelectedRouteId);
        Assert.Equal(0m, row.Kilometers);
    }

    [Fact]
    public void Mark_painted_with_an_archived_location_still_resolves_name_and_colour()
    {
        var archived = new WorkLocation
        {
            Id = 3, Name = "Old Site", ColorHex = "#E64980",
            CountsAsTravel = true, IsDefaultForUnmarked = false, IsArchived = true,
        };
        var entries = new[] { new AttendanceEntry { Date = Wed1, WorkLocationId = 3 } };
        var draft = DeclarationBuilder.Build(Y, M, entries, [Home(), Office(), archived], []);
        var row = draft.Rows.First(r => r.Date == Wed1);
        Assert.Equal("Old Site", row.LocationName);
        Assert.Equal("#E64980", row.ColorHex);
    }

    [Fact]
    public void Picks_the_per_destination_default_among_multiple_routes()
    {
        var scenic = new Route
        {
            Id = 11, Name = "Home → Office (scenic)", FromLocationId = 1, ToLocationId = 2,
            OutboundKm = 20m, ReturnKm = 20m, IsDefault = false,
        };
        var entries = new[] { new AttendanceEntry { Date = Wed1, WorkLocationId = 2 } };
        var draft = DeclarationBuilder.Build(Y, M, entries, [Home(), Office()], [scenic, HomeOffice(isDefault: true)]);
        var row = draft.Rows.First(r => r.Date == Wed1);
        Assert.Equal(10, row.SelectedRouteId);   // the flagged default, not the scenic alt
    }
}