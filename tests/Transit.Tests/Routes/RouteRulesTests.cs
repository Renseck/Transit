using Transit.Core.Entities;
using Transit.Core.Routes;
using Xunit;

namespace Transit.Tests.Routes;

public class RouteRulesTests
{
    private static WorkLocation Loc(int id, string name) => new()
    {
        Id = id,
        Name = name,
        ColorHex = "#E4572E",
        CountsAsTravel = true,
        IsDefaultForUnmarked = false,
    };

    private static Route Rt(int id, int fromId, int toId, string name, bool isDefault = false) => new()
    {
        Id = id,
        Name = name,
        FromLocationId = fromId,
        ToLocationId = toId,
        OutboundKm = 10m,
        ReturnKm = 12m,
        IsDefault = isDefault,
    };

    private static readonly IReadOnlyList<WorkLocation> Active =
        [Loc(1, "Home"), Loc(2, "Office"), Loc(3, "Site")];

    [Fact]
    public void NormalizeName_trims() => Assert.Equal("Office", RouteRules.NormalizeName("  Office  "));

    [Fact]
    public void Validate_accepts_a_fresh_route()
        => RouteRules.ValidateNewOrRenamed("Home → Office", 1, 2, 10m, 12m, [], Active);

    [Fact]
    public void Validate_rejects_empty_name()
        => Assert.Throws<InvalidOperationException>(
            () => RouteRules.ValidateNewOrRenamed("  ", 1, 2, 10m, 12m, [], Active));

    [Fact]
    public void Validate_rejects_duplicate_name_case_insensitively()
    {
        var existing = new[] { Rt(1, 1, 2, "Home → Office") };
        Assert.Throws<InvalidOperationException>(
            () => RouteRules.ValidateNewOrRenamed("home → office", 1, 3, 5m, 5m, existing, Active));
    }

    [Fact]
    public void Validate_allows_editing_route_to_keep_its_own_name()
    {
        var existing = new[] { Rt(1, 1, 2, "Home → Office") };
        RouteRules.ValidateNewOrRenamed("Home → Office", 1, 2, 10m, 12m, existing, Active, editingId: 1);
    }

    [Fact]
    public void Validate_rejects_same_from_and_to()
        => Assert.Throws<InvalidOperationException>(
            () => RouteRules.ValidateNewOrRenamed("Loop", 2, 2, 10m, 12m, [], Active));

    [Fact]
    public void Validate_rejects_unknown_endpoint()
        => Assert.Throws<InvalidOperationException>(
            () => RouteRules.ValidateNewOrRenamed("X", 1, 99, 10m, 12m, [], Active));

    [Fact]
    public void Validate_rejects_negative_leg()
        => Assert.Throws<InvalidOperationException>(
            () => RouteRules.ValidateNewOrRenamed("X", 1, 2, -1m, 12m, [], Active));

    [Fact]
    public void Validate_rejects_zero_total()
        => Assert.Throws<InvalidOperationException>(
            () => RouteRules.ValidateNewOrRenamed("X", 1, 2, 0m, 0m, [], Active));

    [Fact]
    public void ResolveDefaultFor_returns_the_flagged_default()
    {
        var routes = new[] { Rt(1, 1, 2, "A"), Rt(2, 1, 2, "B", isDefault: true) };
        Assert.Equal(2, RouteRules.ResolveDefaultFor(2, routes)!.Id);
    }

    [Fact]
    public void ResolveDefaultFor_falls_back_to_the_sole_route()
    {
        var routes = new[] { Rt(1, 1, 2, "A") };
        Assert.Equal(1, RouteRules.ResolveDefaultFor(2, routes)!.Id);
    }

    [Fact]
    public void ResolveDefaultFor_returns_null_when_no_route_to_destination()
    {
        var routes = new[] { Rt(1, 1, 2, "A") };
        Assert.Null(RouteRules.ResolveDefaultFor(3, routes));
    }

    [Fact]
    public void PickPromotionAfterDelete_returns_a_sibling_to_the_same_destination()
    {
        var routes = new[] { Rt(1, 1, 2, "A", isDefault: true), Rt(2, 1, 2, "B") };
        Assert.Equal(2, RouteRules.PickPromotionAfterDelete(1, routes)!.Id);
    }

    [Fact]
    public void PickPromotionAfterDelete_returns_null_when_no_sibling()
    {
        var routes = new[] { Rt(1, 1, 2, "A", isDefault: true) };
        Assert.Null(RouteRules.PickPromotionAfterDelete(1, routes));
    }
}