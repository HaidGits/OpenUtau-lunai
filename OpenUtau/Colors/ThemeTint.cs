using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using OpenUtau.Core.Util;

namespace OpenUtau.Colors;

/// <summary>
/// Blends theme colors toward a user-chosen tint. Amount 0 = off; 100 = strong cast.
/// Applied after <see cref="ThemeTemperature"/>.
/// </summary>
public static class ThemeTint {
    public const int Min = 0;
    public const int Max = 100;
    public const string DefaultColorHex = "#7271C9";

    /// <summary>How far surfaces move toward the tint at amount=100.</summary>
    const double SurfaceMixAtFull = 0.40;
    /// <summary>How far accents / notes move toward the tint at amount=100.</summary>
    const double AccentMixAtFull = 0.58;

    public static int ClampAmount(int value) => Math.Clamp(value, Min, Max);

    public static Color ParseOrDefault(string? hex) {
        if (ThemeColorStorage.TryParse(hex, out var color)) {
            return Color.FromArgb(255, color.R, color.G, color.B);
        }
        return Color.Parse(DefaultColorHex);
    }

    public static void ApplyToCurrentResources(int amount, string? tintHex) {
        amount = ClampAmount(amount);
        if (amount == 0 || Application.Current == null) {
            return;
        }
        var tint = ParseOrDefault(tintHex);
        double tSurface = amount / 100.0 * SurfaceMixAtFull;
        double tAccent = amount / 100.0 * AccentMixAtFull;
        var res = Application.Current.Resources;
        var variant = ThemeVariant.Default;
        foreach (var key in ThemeTemperature.SurfaceKeys) {
            MixResource(res, variant, key, tint, tSurface);
        }
        foreach (var key in ThemeTemperature.AccentKeys) {
            MixResource(res, variant, key, tint, tAccent);
        }
    }

    public static void ApplyToYaml(ThemeYaml yaml, int amount, string? tintHex) {
        amount = ClampAmount(amount);
        if (amount == 0) {
            return;
        }
        var tint = ParseOrDefault(tintHex);
        double tSurface = amount / 100.0 * SurfaceMixAtFull;
        double tAccent = amount / 100.0 * AccentMixAtFull;
        foreach (var key in ThemeTemperature.SurfaceKeys) {
            MixYamlColor(yaml, key, tint, tSurface);
        }
        foreach (var key in ThemeTemperature.AccentKeys) {
            MixYamlColor(yaml, key, tint, tAccent);
        }
    }

    static void MixResource(IResourceDictionary res, ThemeVariant variant, string key, Color tint, double t) {
        if (!res.TryGetResource(key, variant, out var value) || value is not Color color) {
            return;
        }
        res[key] = MixToward(color, tint, t);
    }

    static void MixYamlColor(ThemeYaml yaml, string key, Color tint, double t) {
        var raw = yaml.GetColor(key);
        if (string.IsNullOrWhiteSpace(raw)
            || string.Equals(raw, "Transparent", StringComparison.OrdinalIgnoreCase)
            || !ThemeColorStorage.TryParse(raw, out var color)) {
            return;
        }
        yaml.SetColor(key, ThemeColorStorage.ToStorageString(MixToward(color, tint, t)));
    }

    static Color MixToward(Color from, Color toward, double t) {
        t = Math.Clamp(t, 0, 1);
        // Keep source alpha (waveform peaks etc. store A in the color).
        return Color.FromArgb(
            from.A,
            (byte)Math.Clamp(Math.Round(from.R + (toward.R - from.R) * t), 0, 255),
            (byte)Math.Clamp(Math.Round(from.G + (toward.G - from.G) * t), 0, 255),
            (byte)Math.Clamp(Math.Round(from.B + (toward.B - from.B) * t), 0, 255));
    }
}
