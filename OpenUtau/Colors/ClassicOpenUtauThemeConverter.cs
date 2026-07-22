using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Media;
using OpenUtau.Core;

namespace OpenUtau.Colors;

/// <summary>
/// Converts classic stakira/OpenUtau YAML themes into OpenUtau Lunai format.
/// Classic themes omit Lunai-only keys; deserialization otherwise leaves Light ctor defaults
/// (wrong for dark themes) and purple note accents.
/// </summary>
public static class ClassicOpenUtauThemeConverter {
    static readonly string[] LunaiMarkerKeys = [
        "accent_color1_note",
        "workspace_canvas_color",
        "note_border_color",
        "transport_toolbar_off_hover_color",
        "piano_roll_toolbar_strip_color",
        "app_top_bar_transport_strip_color",
    ];

    static readonly Regex MadeByComment = new(
        @"^\s*#\s*(?:made\s+by|by|author)\s*[:\-]?\s*(.+?)\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    public static bool LooksLikeClassicTheme(string yamlText) {
        if (string.IsNullOrWhiteSpace(yamlText)) {
            return false;
        }
        var lower = yamlText.ToLowerInvariant();
        if (!lower.Contains("background_color") || !lower.Contains("accent_color1")) {
            return false;
        }
        foreach (var key in LunaiMarkerKeys) {
            if (lower.Contains(key)) {
                return false;
            }
        }
        return true;
    }

    public static ThemeYaml LoadAndConvert(string path) {
        var text = File.ReadAllText(path, Encoding.UTF8);
        var yaml = Yaml.DefaultDeserializer.Deserialize<ThemeYaml>(text)
            ?? BuiltInThemeLoader.CreateFromBuiltIn("Light", "Custom YAML");
        if (LooksLikeClassicTheme(text)) {
            return ConvertClassic(yaml, text);
        }
        NormalizeAllColors(yaml);
        var defaults = BuiltInThemeLoader.CreateFromBuiltIn(yaml.IsDarkMode ? "Dark" : "Light", yaml.Name);
        yaml.FillMissingFrom(defaults);
        ThemePaletteNormalizer.Normalize(yaml);
        return yaml;
    }

    public static ThemeYaml ConvertClassic(ThemeYaml yaml, string? rawYamlText = null) {
        NormalizeAllColors(yaml);
        if (string.IsNullOrWhiteSpace(yaml.Author) && !string.IsNullOrEmpty(rawYamlText)) {
            var match = MadeByComment.Match(rawYamlText);
            if (match.Success) {
                yaml.Author = CleanAuthor(match.Groups[1].Value);
            }
        }
        if (string.IsNullOrWhiteSpace(yaml.Name)) {
            yaml.Name = "Imported Theme";
        }

        DeriveLunaiFields(yaml);
        ThemePaletteNormalizer.Normalize(yaml);
        return yaml;
    }

    static void DeriveLunaiFields(ThemeYaml yaml) {
        var bg = Parse(yaml.BackgroundColor, ParseHex(yaml.IsDarkMode ? "#252525" : "#FFFFFF"));
        var bgOver = Parse(yaml.BackgroundColorPointerOver, yaml.IsDarkMode ? Darken(bg, 0.08) : Darken(bg, 0.06));
        var accent1 = Parse(yaml.AccentColor1, Parse(yaml.SystemAccentColor, Color.FromRgb(0x4E, 0xA6, 0xEA)));
        var accentLight = Parse(yaml.SystemAccentColorLight1, Lighten(accent1, 0.35));
        var border = Parse(yaml.BorderColor, Color.FromRgb(0x70, 0x70, 0x70));
        var fgDisabled = Parse(yaml.ForegroundColorDisabled, Color.FromRgb(0x80, 0x80, 0x80));
        var trackAlt = Parse(yaml.TrackBackgroundAltColor, bgOver);

        yaml.AccentColor1Note = ThemeColorStorage.ToStorageString(accent1);
        yaml.NoteBorderColor = ThemeColorStorage.ToStorageString(Mix(accent1, accentLight, 0.55));
        yaml.NoteBorderColorPressed = ThemeColorStorage.ToStorageString(Lighten(accentLight, 0.2));

        yaml.TransportToolbarOffHoverColor = ThemeColorStorage.ToStorageString(bgOver);
        yaml.TextControlBorderColorDisabled = ThemeColorStorage.ToStorageString(
            Mix(border, Parse(yaml.BackgroundColorDisabled, bg), 0.35));
        yaml.ToolbarCheckedHoverColor = ThemeColorStorage.ToStorageString(accentLight);
        yaml.ToolTipForegroundColor = yaml.IsDarkMode ? "#FFFFFF" : "#111111";
        yaml.MutedIconColor = ThemeColorStorage.ToStorageString(fgDisabled);
        yaml.WarningColor = yaml.IsDarkMode ? "#4A4028" : "#FFF4CE";

        var canvas = yaml.IsDarkMode ? Darken(bg, 0.22) : Darken(bg, 0.1);
        var elevated = yaml.IsDarkMode ? Lighten(bg, 0.08) : Darken(bg, 0.04);
        yaml.WorkspaceCanvasColor = ThemeColorStorage.ToStorageString(canvas);
        yaml.WorkspaceCardColor = ThemeColorStorage.ToStorageString(bg);
        yaml.WorkspaceElevatedSurfaceColor = ThemeColorStorage.ToStorageString(elevated);

        var peak = Color.FromArgb(0x59, accent1.R, accent1.G, accent1.B);
        yaml.PianoRollWaveformPeakColor = ThemeColorStorage.ToStorageString(peak);

        // Light themes keep a dark piano-roll chrome strip (matches Original Light).
        Color strip;
        Color stripHover;
        Color timeline;
        if (yaml.IsDarkMode) {
            strip = Darken(bg, 0.18);
            stripHover = Lighten(strip, 0.18);
            timeline = Darken(bg, 0.12);
        } else {
            strip = Color.FromRgb(0x20, 0x20, 0x20);
            stripHover = Color.FromRgb(0x31, 0x31, 0x31);
            timeline = canvas;
        }
        yaml.PianoRollToolbarStripColor = ThemeColorStorage.ToStorageString(strip);
        yaml.PianoRollToolbarButtonHoverColor = ThemeColorStorage.ToStorageString(stripHover);
        yaml.PianoRollTimelineStripColor = ThemeColorStorage.ToStorageString(timeline);

        yaml.AppTopBarTransportStripColor = ThemeColorStorage.ToStorageString(trackAlt);
        yaml.AppTopBarTransportHoverColor = ThemeColorStorage.ToStorageString(
            yaml.IsDarkMode ? Lighten(trackAlt, 0.12) : Darken(trackAlt, 0.08));
        yaml.AppTopBarValueStripColor = ThemeColorStorage.ToStorageString(canvas);
        yaml.AppTopBarValueDividerColor = ThemeColorStorage.ToStorageString(
            Mix(border, canvas, 0.4));
    }

    static void NormalizeAllColors(ThemeYaml yaml) {
        foreach (var key in ThemeColorCatalog.AllResourceKeys) {
            var value = yaml.GetColor(key);
            if (string.IsNullOrWhiteSpace(value)) {
                continue;
            }
            if (string.Equals(value.Trim(), "Transparent", StringComparison.OrdinalIgnoreCase)) {
                yaml.SetColor(key, "Transparent");
                continue;
            }
            if (ThemeColorStorage.TryParse(value, out var color)) {
                yaml.SetColor(key, ThemeColorStorage.ToStorageString(color));
            }
        }
    }

    static string CleanAuthor(string raw) {
        var author = raw.Trim().Trim('"', '\'');
        // Drop playful suffixes like "tehee" only when they look like trailing fluff after a name.
        author = Regex.Replace(author, @"\s+tehee!?$", "", RegexOptions.IgnoreCase).Trim();
        return author;
    }

    static Color Parse(string? value, Color fallback) =>
        ThemeColorStorage.ParseOrDefault(value, fallback);

    static Color ParseHex(string hex) =>
        ThemeColorStorage.TryParse(hex, out var c) ? c : Color.FromRgb(0, 0, 0);

    static Color Mix(Color a, Color b, double t) =>
        Color.FromRgb(Lerp(a.R, b.R, t), Lerp(a.G, b.G, t), Lerp(a.B, b.B, t));

    static Color Darken(Color color, double amount) => Mix(color, Color.FromRgb(0, 0, 0), amount);

    static Color Lighten(Color color, double amount) => Mix(color, Color.FromRgb(255, 255, 255), amount);

    static byte Lerp(byte a, byte b, double t) =>
        (byte)Math.Clamp((int)Math.Round(a + (b - a) * t), 0, 255);
}
