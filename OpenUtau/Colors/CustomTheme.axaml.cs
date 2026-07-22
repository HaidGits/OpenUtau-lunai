using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia;
using OpenUtau.Core;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau.Colors;

public class CustomTheme {
    public static Dictionary<string, string> Themes = [];
    static HashSet<string> LocalThemes = [];
    static HashSet<string> HubThemes = [];
    static HashSet<string> PackageThemes = [];
    public static ThemeYaml Default;

    public static bool IsPackageTheme(string themeName) => PackageThemes.Contains(themeName);
    public static bool IsHubTheme(string themeName) => HubThemes.Contains(themeName);

    public static IEnumerable<string> LocalThemeNames =>
        LocalThemes.OrderBy(n => n, StringComparer.OrdinalIgnoreCase);

    public static IEnumerable<string> HubThemeNames =>
        HubThemes.OrderBy(n => n, StringComparer.OrdinalIgnoreCase);

    public static void MarkPackageTheme(string themeName) => PackageThemes.Add(themeName);

    internal static void ClearPackageThemes() {
        foreach (var key in PackageThemes) Themes.Remove(key);
        PackageThemes.Clear();
    }

    static CustomTheme() {
        Default = BuiltInThemeLoader.CreateFromBuiltIn("Light", "Custom YAML");
        ListThemes();
    }

    public static void Load(string themeName) {
        if (!string.IsNullOrEmpty(themeName) && Themes.TryGetValue(themeName, out var themePath) && File.Exists(themePath)) {
            Default = ThemeYaml.LoadFromFile(themePath);
            return;
        }

        Preferences.Default.ThemeName = "Light";
        Default = BuiltInThemeLoader.CreateFromBuiltIn("Light", "Custom YAML");
    }

    public static void ListThemes() {
        foreach (var key in LocalThemes) Themes.Remove(key);
        foreach (var key in HubThemes) Themes.Remove(key);
        LocalThemes.Clear();
        HubThemes.Clear();
        Directory.CreateDirectory(PathManager.Inst.ThemesPath);
        Directory.CreateDirectory(ThemeHubClient.HubPath);

        // Local customs: Themes/*.yaml (not Hub subdirectory)
        foreach (var item in Directory.EnumerateFiles(PathManager.Inst.ThemesPath, "*.yaml")) {
            RegisterThemeFile(item, hub: false);
        }

        // Catalog themes: Themes/Hub/*.yaml
        foreach (var item in Directory.EnumerateFiles(ThemeHubClient.HubPath, "*.yaml")) {
            RegisterThemeFile(item, hub: true);
        }
    }

    static void RegisterThemeFile(string path, bool hub) {
        try {
            string baseName = Yaml.DefaultDeserializer.Deserialize<ThemeYaml>(
                File.ReadAllText(path, Encoding.UTF8)).Name;
            if (string.IsNullOrWhiteSpace(baseName)) {
                baseName = Path.GetFileNameWithoutExtension(path);
            }
            if (BuiltInThemeLoader.IsBuiltInTheme(baseName)) {
                return;
            }
            string resolvedName = baseName;
            int dupIter = 1;
            while (Themes.ContainsKey(resolvedName)) {
                resolvedName = $"{baseName} ({dupIter})";
                dupIter++;
            }
            Themes.Add(resolvedName, path);
            if (hub) {
                HubThemes.Add(resolvedName);
            } else {
                LocalThemes.Add(resolvedName);
            }
        } catch (Exception exception) {
            Log.Error(exception, "Failed to parse yaml in {Path}", path);
        }
    }

    public static void ApplyTheme(string themeName) {
        Load(themeName);
        ThemeApplicator.Apply(Default);
    }

    public static string? TryGetAuthor(string themeName) {
        if (!Themes.TryGetValue(themeName, out var path) || !File.Exists(path)) {
            return ThemeHubClient.TryGetAuthorForThemeName(themeName);
        }
        try {
            var yaml = Yaml.DefaultDeserializer.Deserialize<ThemeYaml>(
                File.ReadAllText(path, Encoding.UTF8));
            if (!string.IsNullOrWhiteSpace(yaml?.Author)) {
                return yaml!.Author.Trim();
            }
        } catch {
            // ignore
        }
        return ThemeHubClient.TryGetAuthorForThemeName(themeName);
    }
}
