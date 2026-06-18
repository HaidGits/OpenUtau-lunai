using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using OpenUtau.App;
using OpenUtau.App.Controls;
using OpenUtau.App.ViewModels;
using OpenUtau.Colors;
using OpenUtau.Core;
using OpenUtau.Core.Util;
using ReactiveUI;

namespace OpenUtau.App.Views {
    public partial class AppearancePreferencesPane : UserControl {
        PreferencesViewModel ViewModel => EnsureViewModel();
        int scrollStyleApplyGeneration;

        public AppearancePreferencesPane() {
            InitializeComponent();
            AttachedToVisualTree += (_, _) => {
                ClosePanelButton.IsVisible = IsHostedInPianoRollDock();
                ScheduleApplyScrollStyle();
            };
            DetachedFromVisualTree += (_, _) => {
                scrollStyleApplyGeneration++;
            };
            MessageBus.Current.Listen<ScrollbarsStyleChangedEvent>()
                .Subscribe(_ => ScheduleApplyScrollStyle());
        }

        void ScheduleApplyScrollStyle() {
            if (!WorkspaceScrollbarHelper.IsInVisualTree(this)) {
                return;
            }
            int generation = ++scrollStyleApplyGeneration;
            Dispatcher.UIThread.Post(() => {
                if (generation != scrollStyleApplyGeneration || !WorkspaceScrollbarHelper.IsInVisualTree(this)) {
                    return;
                }
                ApplyScrollStyle();
            }, DispatcherPriority.Loaded);
        }

        void ApplyScrollStyle() {
            if (!WorkspaceScrollbarHelper.IsInVisualTree(this)) {
                return;
            }
            WorkspaceScrollbarHelper.ApplyScrollViewer(ContentScroll, !Preferences.Default.UseOverlayScrollbars);
        }

        PreferencesViewModel EnsureViewModel() {
            if (DataContext is PreferencesViewModel vm) {
                return vm;
            }
            var prefs = new PreferencesViewModel();
            DataContext = prefs;
            return prefs;
        }

        Window? GetOwnerWindow() => TopLevel.GetTopLevel(this) as Window;

        bool IsHostedInPianoRollDock() {
            return this.GetVisualAncestors().OfType<PianoRoll>().Any();
        }

        void OnCloseDockedPanel(object? sender, RoutedEventArgs e) {
            var pianoRoll = this.GetVisualAncestors().OfType<PianoRoll>().FirstOrDefault();
            if (pianoRoll?.ViewModel != null) {
                pianoRoll.ViewModel.ShowAppearancePanel = false;
            }
        }

        void OpenCustomThemeEditor(string themeName) {
            if (string.IsNullOrEmpty(themeName) || !CustomTheme.Themes.TryGetValue(themeName, out var path)) {
                return;
            }
            if (IsHostedInPianoRollDock()) {
                ThemeEditorWindow.CloseIfOpen();
                MessageBus.Current.SendMessage(new OpenDockedThemeEditorEvent { Path = path });
            } else {
                MessageBus.Current.SendMessage(new CloseDockedThemeEditorEvent());
                ThemeEditorWindow.Show(path);
            }
        }

        void OnThemeTilePointerPressed(object? sender, PointerPressedEventArgs e) {
            if (e.Source is Button) {
                return;
            }
            if (sender is not Border { DataContext: ThemePickerItemViewModel item }) {
                return;
            }
            if (item.IsCreateTile) {
                OnCustomThemeCreate(sender, e);
                e.Handled = true;
                return;
            }
            ViewModel.ThemeName = item.Name;
            e.Handled = true;
        }

        void OnThemeEditClick(object? sender, RoutedEventArgs e) {
            if (sender is not Button { DataContext: ThemePickerItemViewModel item } || !item.IsEditable) {
                return;
            }
            ViewModel.ThemeName = item.Name;
            OpenCustomThemeEditor(item.Name);
            e.Handled = true;
        }

        void OnThemeDeleteClick(object? sender, RoutedEventArgs e) {
            if (sender is not Button { DataContext: ThemePickerItemViewModel item } || !item.IsEditable) {
                return;
            }
            _ = DeleteCustomThemeAsync(item.Name);
            e.Handled = true;
        }

        async System.Threading.Tasks.Task DeleteCustomThemeAsync(string themeName) {
            var vm = ViewModel;
            var owner = GetOwnerWindow();
            if (owner == null) {
                return;
            }
            var result = await MessageBox.Show(
                owner,
                ThemeManager.GetString("prefs.appearance.customtheme.delete.message"),
                ThemeManager.GetString("prefs.appearance.customtheme.delete.title"),
                MessageBox.MessageBoxButtons.YesNo);
            if (result != MessageBox.MessageBoxResult.Yes) {
                return;
            }
            if (string.IsNullOrEmpty(themeName) || !CustomTheme.Themes.TryGetValue(themeName, out var path)) {
                return;
            }
            string previousTheme = vm.ThemeItems.TakeWhile(x => x != themeName).LastOrDefault()
                ?? vm.ThemeItems.FirstOrDefault()
                ?? "Dark";
            File.Delete(path);
            vm.RefreshThemes();
            vm.ThemeName = previousTheme;
        }

        void OnCustomThemeCreate(object? sender, RoutedEventArgs e) {
            var vm = ViewModel;
            var owner = GetOwnerWindow();
            var dialog = new CreateCustomThemeDialog();
            dialog.onFinish = (name, baseTheme) => {
                if (string.IsNullOrEmpty(name)) {
                    if (owner != null) {
                        MessageBox.ShowModal(owner,
                            ThemeManager.GetString("prefs.appearance.customtheme.create.empty"),
                            ThemeManager.GetString("prefs.appearance.customtheme.create.title"));
                    }
                    return;
                }

                string filename = string.Join("", name.Where(c => char.IsLetterOrDigit(c) || c == ' '))
                    .Replace(" ", "-").ToLower() + ".yaml";

                string themePath = Path.Join(PathManager.Inst.ThemesPath, filename);
                if (File.Exists(themePath)) {
                    if (owner != null) {
                        MessageBox.ShowModal(owner,
                            ThemeManager.GetString("prefs.appearance.customtheme.create.exists"),
                            ThemeManager.GetString("prefs.appearance.customtheme.create.title"));
                    }
                    return;
                }
                var themeYaml = BuiltInThemeLoader.CreateFromBuiltIn(baseTheme, name);
                themeYaml.SaveToFile(themePath);
                CustomTheme.ListThemes();
                vm.RefreshThemes();
                var themeKey = CustomTheme.Themes.FirstOrDefault(pair => pair.Value == themePath).Key;
                if (!string.IsNullOrEmpty(themeKey)) {
                    vm.ThemeName = themeKey;
                }
            };
            if (owner != null) {
                dialog.ShowDialog(owner);
            } else {
                dialog.Show();
            }
        }
    }
}
