using Transit.Core.Declarations;
using Transit.Core.Entities;
using Xunit;

namespace Transit.Tests.Declarations;

public class DeclarationBuilderApplySelectionsTests
{
    private static readonly DateOnly Wed1 = new(2026, 7, 1);
    private static readonly DateOnly Thu2 = new(2026, 7, 2);

    private static WorkLocation Home() => new()
    {
        Id = 1, Name = "Home", ColorHex = "#4C566A", CountsAsTravel = false,
        IsDefaultForUnmarked = true, Postcode = "1000 AA",
    };

    private static WorkLocation Office() => new()
    {
        Id = 2, Name = "Office", ColorHex = "#3B5BDB", CountsAsTravel = true,
        IsDefaultForUnmarked = false, Postcode = "2000 BB",
    };

    private static Route Default() => new()
    {
        Id = 10, Name = "Home → Office", FromLocationId = 1, ToLocationId = 2,
        OutboundKm = 11m, ReturnKm = 10m, IsDefault = true,
    };

    private static Route Scenic() => new()
    {
        Id = 11, Name = "Home → Office (scenic)", FromLocationId = 1, ToLocationId = 2,
        OutboundKm = 20m, ReturnKm = 20m, IsDefault = false,
    };

    // Wed1 marked Office (travel, defaults to route 10); Thu2 unmarked → Home (non-travel).
    private static DeclarationDraft BaseDraft()
    {
        var entries = new[] { new AttendanceEntry { Date = Wed1, WorkLocationId = 2 } };
        return DeclarationBuilder.Build(2026, 7, entries, [Home(), Office()], [Default(), Scenic()]);
    }

    [Fact]
    public void Override_to_a_different_route_reresolves_name_and_km()
    {
        var draft = DeclarationBuilder.ApplySelections(
            BaseDraft(), new Dictionary<DateOnly, int?> { [Wed1] = 11 }, [Default(), Scenic()], [Home(), Office()]);
        var row = draft.Rows.First(r => r.Date == Wed1);
        Assert.Equal(11, row.SelectedRouteId);
        Assert.Equal("Home → Office (scenic)", row.RouteName);
        Assert.Equal(40m, row.Kilometers);
        Assert.Equal("Home", row.FromLocationName);
        Assert.Equal("2000 BB", row.ToPostcode);
        Assert.False(row.HasNoRoute);
    }

    [Fact]
    public void Override_to_null_makes_a_no_route_row()
    {
        var draft = DeclarationBuilder.ApplySelections(
            BaseDraft(), new Dictionary<DateOnly, int?> { [Wed1] = null }, [Default(), Scenic()], [Home(), Office()]);
        var row = draft.Rows.First(r => r.Date == Wed1);
        Assert.Null(row.SelectedRouteId);
        Assert.Null(row.RouteName);
        Assert.True(row.HasNoRoute);
        Assert.Equal(0m, row.Kilometers);
    }

    [Fact]
    public void Unknown_route_id_falls_back_to_a_no_route_row()
    {
        var draft = DeclarationBuilder.ApplySelections(
            BaseDraft(), new Dictionary<DateOnly, int?> { [Wed1] = 999 }, [Default(), Scenic()], [Home(), Office()]);
        var row = draft.Rows.First(r => r.Date == Wed1);
        Assert.Null(row.SelectedRouteId);
        Assert.True(row.HasNoRoute);
        Assert.Equal(0m, row.Kilometers);
    }

    [Fact]
    public void Dates_absent_from_the_map_keep_their_default_resolution()
    {
        var draft = DeclarationBuilder.ApplySelections(
            BaseDraft(), new Dictionary<DateOnly, int?>(), [Default(), Scenic()], [Home(), Office()]);
        var row = draft.Rows.First(r => r.Date == Wed1);
        Assert.Equal(10, row.SelectedRouteId);   // untouched default
        Assert.Equal(21m, row.Kilometers);
    }

    [Fact]
    public void A_non_travel_row_ignores_a_stray_selection()
    {
        var draft = DeclarationBuilder.ApplySelections(
            BaseDraft(), new Dictionary<DateOnly, int?> { [Thu2] = 10 }, [Default(), Scenic()], [Home(), Office()]);
        var row = draft.Rows.First(r => r.Date == Thu2);
        Assert.False(row.CountsAsTravel);
        Assert.Null(row.SelectedRouteId);
        Assert.Equal(0m, row.Kilometers);
    }
}