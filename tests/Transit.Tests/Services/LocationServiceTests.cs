using Transit.Core.Entities;
using Transit.Core.Services;
using Xunit;

namespace Transit.Tests.Services;

public class LocationServiceTests
{
    private static WorkLocation Loc(int id, bool isDefault) => new()
    {
        Id = id,
        Name = $"L{id}",
        ColorHex = "#000000",
        CountsAsTravel = false,
        IsDefaultForUnmarked = isDefault,
    };

    [Fact]
    public void ResolveDefault_returns_the_single_default()
    {
        var locations = new[] { Loc(1, false), Loc(2, true), Loc(3, false) };

        var result = LocationService.ResolveDefault(locations);

        Assert.Equal(2, result.Id);
    }

    [Fact]
    public void ResolveDefault_throws_when_none_is_default()
    {
        var locations = new[] { Loc(1, false), Loc(2, false) };

        Assert.Throws<InvalidOperationException>(() => LocationService.ResolveDefault(locations));
    }

    [Fact]
    public void ResolveDefault_throws_when_multiple_are_default()
    {
        var locations = new[] { Loc(1, true), Loc(2, true) };

        Assert.Throws<InvalidOperationException>(() => LocationService.ResolveDefault(locations));
    }
}