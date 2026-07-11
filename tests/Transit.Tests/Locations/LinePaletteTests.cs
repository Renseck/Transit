using Transit.Core.Locations;
using Xunit;

namespace Transit.Tests.Locations;

public class LinePaletteTests
{
    [Fact]
    public void All_has_ten_distinct_colours()
    {
        Assert.Equal(10, LinePalette.All.Count);
        Assert.Equal(10, LinePalette.All.Select(c => c.Hex.ToUpperInvariant()).Distinct().Count());
    }

    [Fact]
    public void Signal_amber_is_excluded()
        => Assert.DoesNotContain(LinePalette.All, c => string.Equals(c.Hex, "#F5A623", StringComparison.OrdinalIgnoreCase));

    [Fact]
    public void Contains_is_case_insensitive()
    {
        Assert.True(LinePalette.Contains("#e4572e"));
        Assert.True(LinePalette.Contains("#E4572E"));
    }

    [Fact]
    public void Contains_rejects_a_non_palette_colour()
        => Assert.False(LinePalette.Contains("#123456"));

    [Fact]
    public void Find_returns_the_named_colour_or_null()
    {
        Assert.Equal("Coral", LinePalette.Find("#E4572E")!.Value.Name);
        Assert.Null(LinePalette.Find("#123456"));
    }
}