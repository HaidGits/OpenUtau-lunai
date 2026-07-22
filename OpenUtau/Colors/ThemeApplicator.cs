using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using OpenUtau.App;
using OpenUtau.Core.Util;

namespace OpenUtau.Colors;

public static class ThemeApplicator {
    public static void ApplyCustomBase() {
        if (Application.Current == null) {
            return;
        }
        if (Application.Current.Resources["themes-custom"] is not IResourceDictionary custom) {
            return;
        }
        foreach (var item in custom) {
            Application.Current.Resources[item.Key] = item.Value;
        }
    }

    public static void Apply(ThemeYaml yaml) {
        if (Application.Current == null) {
            return;
        }
        yaml.ApplyToResources();
        ThemeTemperature.ApplyToCurrentResources(Preferences.Default.ThemeTemperature);
        ThemeTint.ApplyToCurrentResources(Preferences.Default.ThemeTintAmount, Preferences.Default.ThemeTintColor);
        Application.Current.RequestedThemeVariant = yaml.IsDarkMode
            ? ThemeVariant.Dark
            : ThemeVariant.Light;
        ThemeManager.LoadTheme();
    }
}
