namespace Transit.Core.Locations;

/// <summary>A selectable transit-line colour: a display name and its hex.</summary>
public readonly record struct LineColor(string Name, string Hex);

/// <summary>
/// The curated set of colours a <c>WorkLocation</c> may use. Canonical source for
/// colour *validation*; mirrored for *appearance* into <c>wwwroot/tokens.css</c>
/// (the <c>--tr-line-*</c> reference tokens). Change one, change the other — this
/// is the third dual-source pair alongside tokens.css ↔ TransitTheme.cs.
///
/// Signal amber (#F5A623) is deliberately excluded: it stays reserved for the app's
/// one signal accent (today / now / you-are-here) so a location never impersonates it.
/// </summary>
public static class LinePalette
{
    public static readonly IReadOnlyList<LineColor> All =
    [
        new("Coral",  "#E4572E"),
        new("Rose",   "#E64980"),
        new("Violet", "#7048E8"),
        new("Indigo", "#3B5BDB"),
        new("Azure",  "#1C7ED6"),
        new("Teal",   "#099268"),
        new("Green",  "#2F9E44"),
        new("Lime",   "#5C940D"),
        new("Clay",   "#B25B34"),
        new("Slate",  "#4C566A"),
    ];

    public static bool Contains(string hex)
    {
        foreach (var c in All)
        {
            if (string.Equals(c.Hex, hex, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static LineColor? Find(string hex)
    {
        foreach (var c in All)
        {
            if (string.Equals(c.Hex, hex, StringComparison.OrdinalIgnoreCase))
            {
                return c;
            }
        }

        return null;
    }
}