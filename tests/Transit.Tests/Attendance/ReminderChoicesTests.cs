using Transit.Core.Attendance;
using Transit.Core.Entities;
using Xunit;

namespace Transit.Tests.Attendance;

public class ReminderChoicesTests
{
    private static WorkLocation Loc(int id, int order, bool archived = false) => new()
    {
        Id = id, Name = $"L{id}", ColorHex = "#000000", DisplayOrder = order, IsArchived = archived,
    };

    [Fact]
    public void Without_a_suggestion_active_locations_follow_display_order()
    {
        var choices = ReminderChoices.Build([Loc(2, 1), Loc(1, 0)], suggested: null);

        Assert.Equal(new[] { 1, 2 }, choices.Select(l => l.Id));
    }

    [Fact]
    public void The_suggested_location_comes_first_and_is_not_repeated()
    {
        var locations = new[] { Loc(1, 0), Loc(2, 1), Loc(3, 2) };

        var choices = ReminderChoices.Build(locations, suggested: locations[2]);

        Assert.Equal(new[] { 3, 1, 2 }, choices.Select(l => l.Id));
    }

    [Fact]
    public void Choices_are_capped_at_four()
    {
        var locations = Enumerable.Range(1, 6).Select(i => Loc(i, i)).ToList();

        var choices = ReminderChoices.Build(locations, suggested: locations[5]);

        Assert.Equal(new[] { 6, 1, 2, 3 }, choices.Select(l => l.Id));
    }

    [Fact]
    public void Archived_locations_are_excluded()
    {
        var choices = ReminderChoices.Build([Loc(1, 0), Loc(2, 1, archived: true)], suggested: null);

        Assert.Equal(new[] { 1 }, choices.Select(l => l.Id));
    }
}
