using Transit.Core.Entities;
using Transit.Core.Locations;
using Xunit;

namespace Transit.Tests.Locations;

public class LocationRulesTests
{
    private static WorkLocation Loc(int id, string name, bool isDefault = false, bool archived = false) => new()
    {
        Id = id,
        Name = name,
        ColorHex = "#E4572E",
        CountsAsTravel = false,
        IsDefaultForUnmarked = isDefault,
        IsArchived = archived,
    };

    [Fact]
    public void NormalizeName_trims()
        => Assert.Equal("Office", LocationRules.NormalizeName("  Office  "));

    [Fact]
    public void ValidateNewOrRenamed_accepts_a_fresh_name_and_palette_colour()
    {
        var active = new[] { Loc(1, "Home", isDefault: true) };
        LocationRules.ValidateNewOrRenamed("Office", "#3B5BDB", active); // does not throw
    }

    [Fact]
    public void ValidateNewOrRenamed_rejects_empty_name()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => LocationRules.ValidateNewOrRenamed("   ", "#3B5BDB", []));
        Assert.Contains("name", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateNewOrRenamed_rejects_duplicate_name_case_insensitively()
    {
        var active = new[] { Loc(1, "Office") };
        Assert.Throws<InvalidOperationException>(
            () => LocationRules.ValidateNewOrRenamed("office", "#3B5BDB", active));
    }

    [Fact]
    public void ValidateNewOrRenamed_allows_a_location_to_keep_its_own_name_when_editing()
    {
        var active = new[] { Loc(1, "Office") };
        LocationRules.ValidateNewOrRenamed("Office", "#3B5BDB", active, editingId: 1); // does not throw
    }

    [Fact]
    public void ValidateNewOrRenamed_rejects_a_non_palette_colour()
        => Assert.Throws<InvalidOperationException>(
            () => LocationRules.ValidateNewOrRenamed("Office", "#123456", []));

    [Fact]
    public void EnsureCanArchive_blocks_the_default()
    {
        var all = new[] { Loc(1, "Home", isDefault: true), Loc(2, "Office") };
        Assert.Throws<InvalidOperationException>(() => LocationRules.EnsureCanArchive(1, all));
    }

    [Fact]
    public void EnsureCanArchive_blocks_the_last_active_location()
    {
        // Location 1 is the only active line (2 is archived) and is non-default, so the
        // block comes specifically from the "last active location" rule, not the default rule.
        var all = new[] { Loc(1, "Home"), Loc(2, "Office", isDefault: true, archived: true) };
        Assert.Throws<InvalidOperationException>(() => LocationRules.EnsureCanArchive(1, all));
    }

    [Fact]
    public void EnsureCanArchive_allows_a_non_default_with_others_active()
    {
        var all = new[] { Loc(1, "Home", isDefault: true), Loc(2, "Office"), Loc(3, "Site") };
        LocationRules.EnsureCanArchive(2, all); // does not throw
    }

    [Fact]
    public void EnsureCanBeDefault_blocks_an_archived_target()
    {
        var all = new[] { Loc(1, "Home", isDefault: true), Loc(2, "Office", archived: true) };
        Assert.Throws<InvalidOperationException>(() => LocationRules.EnsureCanBeDefault(2, all));
    }

    [Fact]
    public void ValidateReorder_rejects_unknown_ids()
    {
        var known = new[] { Loc(1, "Home"), Loc(2, "Office") };
        Assert.Throws<InvalidOperationException>(
            () => LocationRules.ValidateReorder(new[] { 1, 99 }, known));
    }

    [Fact]
    public void ValidateReorder_accepts_known_ids()
    {
        var known = new[] { Loc(1, "Home"), Loc(2, "Office") };
        LocationRules.ValidateReorder(new[] { 2, 1 }, known); // does not throw
    }
}