namespace OpenUtau.Colors;

using System;
using Avalonia.Media;

/// <summary>
/// Built-in theme snapshots. Values must stay in sync with DarkTheme.axaml / LightTheme.axaml.
/// ResourceInclude dictionaries are not enumerable reliably at theme-creation time.
/// </summary>
public static class BuiltInThemeLoader {
    public const string LightThemeName = "Light";
    public const string DarkThemeName = "Dark";
    public const string GrayThemeName = "Gray";
    public const string ColdThemeName = "Cold";
    public const string WarmThemeName = "Warm";

    public static readonly string[] BaseThemeNames = [
        LightThemeName,
        DarkThemeName,
    ];

    public static readonly string[] BuiltInCustomThemeNames = [
        GrayThemeName,
        ColdThemeName,
        WarmThemeName,
    ];

    public static ThemeYaml CreateFromBuiltIn(string baseTheme, string name) {
        var yaml = string.Equals(baseTheme, DarkThemeName, System.StringComparison.OrdinalIgnoreCase)
            ? CreateDark()
            : CreateLight();
        yaml.Name = name;
        return yaml;
    }

    public static bool IsBuiltInTheme(string? name) {
        return TryCreateThemeByName(name, out _);
    }

    public static bool TryCreateThemeByName(string? name, out ThemeYaml yaml) {
        switch (name) {
            case LightThemeName:
                yaml = CreateLight();
                return true;
            case DarkThemeName:
                yaml = CreateDark();
                return true;
            case GrayThemeName:
                yaml = CreateGray();
                return true;
            case ColdThemeName:
                yaml = CreateCold();
                return true;
            case WarmThemeName:
                yaml = CreateWarm();
                return true;
            default:
                yaml = default!;
                return false;
        }
    }

    public static ThemeYaml CreateDark() {
        return new ThemeYaml {
            IsDarkMode = true,
            BackgroundColor = "#212121",
            BackgroundColorPointerOver = "#2F2F2F",
            TransportToolbarOffHoverColor = "#3A3A3A",
            BackgroundColorPressed = "#2F2F2F",
            BackgroundColorDisabled = "#212121",
            ForegroundColor = "#F0F0F0",
            ForegroundColorPointerOver = "#FCFCFC",
            ForegroundColorPressed = "#FCFCFC",
            ForegroundColorDisabled = "#A6A6A6",
            TextControlBorderColorDisabled = "#4D4D4D",
            BorderColor = "#E0E0E0",
            BorderColorPointerOver = "#FCFCFC",
            SystemAccentColor = "#7271C9",
            SystemAccentColorLight1 = "#E0E0E0",
            SystemAccentColorDark1 = "#B6B6B6",
            NeutralAccentColor = "#FCFCFC",
            NeutralAccentColorPointerOver = "#FCFCFC",
            AccentColor1 = "#E0E0E0",
            AccentColor1Note = "#4C4C7A",
            AccentColor2 = "#7271C9",
            AccentColor3 = "#FCFCFC",
            NoteBorderColor = "#7B79D9",
            NoteBorderColorPressed = "#ADACFC",
            TickLineColor = "#060606",
            BarNumberColor = "#E0E0E0",
            FinalPitchColor = "#CAFFFFFF",
            TrackBackgroundAltColor = "#2B2B2B",
            WarningColor = "#433519",
            ToolbarCheckedHoverColor = "#D8D8D8",
            ToolTipForegroundColor = "#FFFFFF",
            WorkspaceCanvasColor = "#141414",
            WorkspaceCardColor = "#212121",
            WorkspaceElevatedSurfaceColor = "#2B2B2B",
            MutedIconColor = "#808080",
            PianoRollWaveformPeakColor = "#59B5B5B5",
            PianoRollToolbarStripColor = "#1F1F1F",
            PianoRollToolbarButtonHoverColor = "#313131",
            PianoRollTimelineStripColor = "#191919",
            AppTopBarTransportStripColor = "#323232",
            AppTopBarTransportHoverColor = "#454545",
            AppTopBarValueStripColor = "#1C1C1C",
            AppTopBarValueDividerColor = "#3A3A3A",
            WhiteKeyColorLeft = "#C0C0C0",
            WhiteKeyColorRight = "#E0E0E0",
            WhiteKeyNameColor = "#212121",
            CenterKeyColorLeft = "#A9A6CD",
            CenterKeyColorRight = "#D3CFFF",
            CenterKeyNameColor = "#4433FF",
            BlackKeyColorLeft = "Transparent",
            BlackKeyColorRight = "Transparent",
            BlackKeyNameColor = "#FCFCFC",
        };
    }

    public static ThemeYaml CreateLight() {
        return new ThemeYaml {
            IsDarkMode = false,
            BackgroundColor = "#FCFCFC",
            BackgroundColorPointerOver = "#F0F0F0",
            TransportToolbarOffHoverColor = "#F6F6F6",
            BackgroundColorPressed = "#E0E0E0",
            BackgroundColorDisabled = "#D0D0D0",
            ForegroundColor = "#111111",
            ForegroundColorPointerOver = "#111111",
            ForegroundColorPressed = "#202020",
            ForegroundColorDisabled = "#808080",
            TextControlBorderColorDisabled = "#BDBDBD",
            BorderColor = "#707070",
            BorderColorPointerOver = "#B0B0B0",
            SystemAccentColor = "#635EA6",
            SystemAccentColorLight1 = "#393957",
            SystemAccentColorDark1 = "#2B2B46",
            NeutralAccentColor = "#A1A1B3",
            NeutralAccentColorPointerOver = "#8A8A9A",
            AccentColor1 = "#A9A6CD",
            AccentColor1Note = "#A9A6CD",
            AccentColor2 = "#635EA6",
            AccentColor3 = "#736DC1",
            NoteBorderColor = "#7B79D9",
            NoteBorderColorPressed = "#4C4B98",
            TickLineColor = "#C0C0CA",
            BarNumberColor = "#4F4F6E",
            FinalPitchColor = "#FF8979FF",
            TrackBackgroundAltColor = "#F0F0F0",
            WarningColor = "#FFF4CE",
            ToolbarCheckedHoverColor = "#E0E0E0",
            ToolTipForegroundColor = "#FFFFFF",
            WorkspaceCanvasColor = "#E4E4E8",
            WorkspaceCardColor = "#FCFCFC",
            WorkspaceElevatedSurfaceColor = "#E8E8EC",
            MutedIconColor = "#808080",
            PianoRollWaveformPeakColor = "#59999999",
            PianoRollToolbarStripColor = "#202020",
            PianoRollToolbarButtonHoverColor = "#313131",
            PianoRollTimelineStripColor = "#D8D8DE",
            AppTopBarTransportStripColor = "#E8E8EC",
            AppTopBarTransportHoverColor = "#F4F4F8",
            AppTopBarValueStripColor = "#D8D8DE",
            AppTopBarValueDividerColor = "#C0C0C8",
            WhiteKeyColorLeft = "#FCFCFC",
            WhiteKeyColorRight = "#FCFCFC",
            WhiteKeyNameColor = "#343434",
            CenterKeyColorLeft = "#A9A6CD",
            CenterKeyColorRight = "#D3CFFF",
            CenterKeyNameColor = "#111111",
            BlackKeyColorLeft = "#45445C",
            BlackKeyColorRight = "#45445C",
            BlackKeyNameColor = "#FCFCFC",
        };
    }

    public static ThemeYaml CreateGray() {
        var yaml = CreateDark();
        yaml.WorkspaceCanvasColor = "#1A1A1A";
        yaml.WorkspaceCardColor = "#323232";
        yaml.WorkspaceElevatedSurfaceColor = "#3F3F3F";
        yaml.BackgroundColor = "#303030";
        yaml.BackgroundColorPointerOver = "#3F3F3F";
        yaml.BackgroundColorPressed = "#464646";
        yaml.BackgroundColorDisabled = "#303030";
        yaml.TransportToolbarOffHoverColor = "#4A4A4A";
        yaml.TrackBackgroundAltColor = "#454545";
        yaml.TextControlBorderColorDisabled = "#626262";
        yaml.PianoRollToolbarStripColor = "#121212";
        yaml.PianoRollTimelineStripColor = "#323232";
        yaml.AppTopBarTransportStripColor = "#414141";
        yaml.AppTopBarTransportHoverColor = "#545454";
        yaml.AppTopBarValueStripColor = "#242424";
        yaml.AppTopBarValueDividerColor = "#4E4E4E";
        yaml.PianoRollToolbarButtonHoverColor = "#3F3F3F";
        yaml.TickLineColor = "#080808";
        yaml.BarNumberColor = "#F0F0F0";
        ApplyPresetTint(yaml, surfaceR: 0, surfaceG: 0, surfaceB: 0, accentR: 5, accentG: 5, accentB: 5);
        return yaml;
    }

    public static ThemeYaml CreateCold() {
        var yaml = CreateDark();
        ApplyPresetTint(yaml, surfaceR: -1, surfaceG: -1, surfaceB: 2, accentR: 0, accentG: 0, accentB: 1);
        return yaml;
    }

    public static ThemeYaml CreateWarm() {
        var yaml = CreateDark();
        ApplyPresetTint(yaml, surfaceR: 2, surfaceG: 0, surfaceB: -1, accentR: 1, accentG: 0, accentB: -1);
        return yaml;
    }

    static readonly string[] SurfaceTintKeys = [
        nameof(ThemeYaml.BackgroundColor),
        nameof(ThemeYaml.BackgroundColorPointerOver),
        nameof(ThemeYaml.BackgroundColorPressed),
        nameof(ThemeYaml.BackgroundColorDisabled),
        nameof(ThemeYaml.TextControlBorderColorDisabled),
        nameof(ThemeYaml.TransportToolbarOffHoverColor),
        nameof(ThemeYaml.ToolbarCheckedHoverColor),
        nameof(ThemeYaml.WorkspaceCanvasColor),
        nameof(ThemeYaml.WorkspaceCardColor),
        nameof(ThemeYaml.WorkspaceElevatedSurfaceColor),
        nameof(ThemeYaml.TrackBackgroundAltColor),
        nameof(ThemeYaml.MutedIconColor),
        nameof(ThemeYaml.PianoRollToolbarStripColor),
        nameof(ThemeYaml.PianoRollToolbarButtonHoverColor),
        nameof(ThemeYaml.PianoRollTimelineStripColor),
        nameof(ThemeYaml.AppTopBarTransportStripColor),
        nameof(ThemeYaml.AppTopBarTransportHoverColor),
        nameof(ThemeYaml.AppTopBarValueStripColor),
        nameof(ThemeYaml.AppTopBarValueDividerColor),
        nameof(ThemeYaml.TickLineColor),
        nameof(ThemeYaml.BarNumberColor),
        nameof(ThemeYaml.PianoRollWaveformPeakColor),
        nameof(ThemeYaml.WarningColor),
    ];

    static readonly string[] AccentTintKeys = [
        nameof(ThemeYaml.SystemAccentColor),
        nameof(ThemeYaml.SystemAccentColorLight1),
        nameof(ThemeYaml.SystemAccentColorDark1),
        nameof(ThemeYaml.NeutralAccentColor),
        nameof(ThemeYaml.NeutralAccentColorPointerOver),
        nameof(ThemeYaml.AccentColor1),
        nameof(ThemeYaml.AccentColor1Note),
        nameof(ThemeYaml.AccentColor2),
        nameof(ThemeYaml.AccentColor3),
        nameof(ThemeYaml.NoteBorderColor),
        nameof(ThemeYaml.NoteBorderColorPressed),
    ];

    static void ApplyPresetTint(
        ThemeYaml yaml,
        int surfaceR, int surfaceG, int surfaceB,
        int accentR, int accentG, int accentB) {
        foreach (var key in SurfaceTintKeys) {
            yaml.SetColor(key, TintHex(yaml.GetColor(key), surfaceR, surfaceG, surfaceB));
        }
        foreach (var key in AccentTintKeys) {
            yaml.SetColor(key, TintHex(yaml.GetColor(key), accentR, accentG, accentB));
        }
    }

    static string TintHex(string? color, int deltaR, int deltaG, int deltaB) {
        if (string.IsNullOrWhiteSpace(color)
            || string.Equals(color, "Transparent", StringComparison.OrdinalIgnoreCase)) {
            return color ?? "#000000";
        }
        if (!ThemeColorStorage.TryParse(color, out var parsed)) {
            return color;
        }
        return ThemeColorStorage.ToStorageString(Color.FromArgb(
            parsed.A,
            (byte)Math.Clamp(parsed.R + deltaR, 0, 255),
            (byte)Math.Clamp(parsed.G + deltaG, 0, 255),
            (byte)Math.Clamp(parsed.B + deltaB, 0, 255)));
    }
}
