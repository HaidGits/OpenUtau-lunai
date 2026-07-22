using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using OpenUtau.Core.Util;

namespace OpenUtau.Colors;

/// <summary>
/// Shifts theme colors warm (positive) or cold (negative), extending the old Warm/Cold presets
/// so any theme can be temperature-adjusted. 0 = unchanged; ±100 = strong cast.
/// </summary>
public static class ThemeTemperature {
    public const int Min = -100;
    public const int Max = 100;

    public static readonly string[] SurfaceKeys = [
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

    public static readonly string[] AccentKeys = [
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

    public static int Clamp(int value) => Math.Clamp(value, Min, Max);

    /// <summary>Tint colors already written into Application.Current.Resources.</summary>
    public static void ApplyToCurrentResources(int temperature) {
        temperature = Clamp(temperature);
        if (temperature == 0 || Application.Current == null) {
            return;
        }
        GetDeltas(temperature, out int sR, out int sG, out int sB, out int aR, out int aG, out int aB);
        var res = Application.Current.Resources;
        var variant = ThemeVariant.Default;
        foreach (var key in SurfaceKeys) {
            TintResource(res, variant, key, sR, sG, sB);
        }
        foreach (var key in AccentKeys) {
            TintResource(res, variant, key, aR, aG, aB);
        }
    }

    /// <summary>Tint a theme snapshot (e.g. picker previews). Mutates <paramref name="yaml"/>.</summary>
    public static void ApplyToYaml(ThemeYaml yaml, int temperature) {
        temperature = Clamp(temperature);
        if (temperature == 0) {
            return;
        }
        GetDeltas(temperature, out int sR, out int sG, out int sB, out int aR, out int aG, out int aB);
        foreach (var key in SurfaceKeys) {
            TintYamlColor(yaml, key, sR, sG, sB);
        }
        foreach (var key in AccentKeys) {
            TintYamlColor(yaml, key, aR, aG, aB);
        }
    }

    static void GetDeltas(
        int temperature,
        out int surfaceR, out int surfaceG, out int surfaceB,
        out int accentR, out int accentG, out int accentB) {
        // Continuous warm(+)/cold(-) cast; 0.8 keeps full-scale milder than the first pass.
        const double strength = 0.8;
        double t = temperature / 100.0 * strength;
        surfaceR = (int)Math.Round(t * 14);
        surfaceG = (int)Math.Round(t * 3);
        surfaceB = (int)Math.Round(t * -12);
        accentR = (int)Math.Round(t * 18);
        accentG = (int)Math.Round(t * 2);
        accentB = (int)Math.Round(t * -16);
    }

    static void TintResource(IResourceDictionary res, ThemeVariant variant, string key, int dR, int dG, int dB) {
        if (!res.TryGetResource(key, variant, out var value) || value is not Color color) {
            return;
        }
        res[key] = TintColor(color, dR, dG, dB);
    }

    static void TintYamlColor(ThemeYaml yaml, string key, int dR, int dG, int dB) {
        var raw = yaml.GetColor(key);
        if (string.IsNullOrWhiteSpace(raw)
            || string.Equals(raw, "Transparent", StringComparison.OrdinalIgnoreCase)
            || !ThemeColorStorage.TryParse(raw, out var color)) {
            return;
        }
        yaml.SetColor(key, ThemeColorStorage.ToStorageString(TintColor(color, dR, dG, dB)));
    }

    static Color TintColor(Color c, int dR, int dG, int dB) => Color.FromArgb(
        c.A,
        (byte)Math.Clamp(c.R + dR, 0, 255),
        (byte)Math.Clamp(c.G + dG, 0, 255),
        (byte)Math.Clamp(c.B + dB, 0, 255));
}
