using Transit.Core.Attendance;
using Transit.Core.Entities;
using Xunit;

namespace Transit.Tests.Attendance;

public class NetworkHintMatcherTests
{
    private static WorkLocation Loc(int id, string? hint, int order = 0, bool archived = false) => new()
    {
        Id = id, Name = $"L{id}", ColorHex = "#000000", NetworkHint = hint, DisplayOrder = order, IsArchived = archived,
    };

    [Fact]
    public void Matching_network_suggests_its_location()
    {
        var match = NetworkHintMatcher.Match([Loc(1, null), Loc(3, "TRUSTON-CORP")], ["TRUSTON-CORP"]);

        Assert.NotNull(match);
        Assert.Equal(3, match.Location.Id);
        Assert.Equal("TRUSTON-CORP", match.Network);
    }

    [Fact]
    public void No_matching_network_suggests_nothing()
    {
        var match = NetworkHintMatcher.Match([Loc(3, "TRUSTON-CORP")], ["HomeWifi"]);

        Assert.Null(match);
    }

    [Fact]
    public void Match_ignores_case_and_surrounding_whitespace()
    {
        var match = NetworkHintMatcher.Match([Loc(3, " truston-corp ")], ["TRUSTON-CORP  "]);

        Assert.NotNull(match);
        Assert.Equal(3, match.Location.Id);
    }

    [Fact]
    public void Several_matches_pick_the_lowest_display_order()
    {
        var locations = new[] { Loc(5, "Shared", order: 2), Loc(4, "Shared", order: 1) };

        var match = NetworkHintMatcher.Match(locations, ["Shared"]);

        Assert.NotNull(match);
        Assert.Equal(4, match.Location.Id);
    }

    [Fact]
    public void Archived_locations_are_never_suggested()
    {
        var match = NetworkHintMatcher.Match([Loc(3, "TRUSTON-CORP", archived: true)], ["TRUSTON-CORP"]);

        Assert.Null(match);
    }

    [Fact]
    public void Blank_hints_and_blank_networks_never_match()
    {
        var match = NetworkHintMatcher.Match([Loc(1, ""), Loc(2, "   ")], ["", "  "]);

        Assert.Null(match);
    }
}
