using MudBlazor;

namespace Transit.Web.Theme;

/// <summary>
/// The MudBlazor mirror of <c>wwwroot/tokens.css</c>. Keeps built-in MudBlazor
/// components on the same palette and type faces as the custom "Wayfinding"
/// vocabulary. tokens.css remains the source of truth; when a token changes
/// here it must change there too.
/// </summary>
public static class TransitTheme
{
    private static readonly string[] Body = ["IBM Plex Sans", "Segoe UI", "system-ui", "sans-serif"];
    private static readonly string[] Display = ["Space Grotesk", "Segoe UI", "system-ui", "sans-serif"];
    private static readonly string[] Mono = ["IBM Plex Mono", "ui-monospace", "Consolas", "monospace"];

    public static readonly MudTheme Instance = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#F5A623",
            PrimaryContrastText = "#1A1F2B",
            Secondary = "#1A1F2B",
            Background = "#F4F5F3",
            Surface = "#FFFFFF",
            AppbarBackground = "#1A1F2B",
            AppbarText = "#E9ECEA",
            DrawerBackground = "#1A1F2B",
            DrawerText = "#C9CDC7",
            DrawerIcon = "#C9CDC7",
            TextPrimary = "#1A1F2B",
            TextSecondary = "#6B7280",
            Divider = "#D8DBD6",
            LinesDefault = "#D8DBD6",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography { FontFamily = Body },
            H1 = new H1Typography { FontFamily = Display },
            H2 = new H2Typography { FontFamily = Display },
            H3 = new H3Typography { FontFamily = Display },
            H4 = new H4Typography { FontFamily = Display },
            H5 = new H5Typography { FontFamily = Display },
            H6 = new H6Typography { FontFamily = Display },
            Subtitle1 = new Subtitle1Typography { FontFamily = Body },
            Subtitle2 = new Subtitle2Typography { FontFamily = Body },
            Body1 = new Body1Typography { FontFamily = Body },
            Body2 = new Body2Typography { FontFamily = Body },
            Button = new ButtonTypography { FontFamily = Display },
            Caption = new CaptionTypography { FontFamily = Mono },
            Overline = new OverlineTypography { FontFamily = Mono },
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "10px",
        },
    };
}
