using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using OpenUtau.App.Controls;
using OpenUtau.App.ViewModels;
using OpenUtau.Core.Util;

namespace OpenUtau.App.Views {
    public partial class AboutDialog : Window {
        public AboutViewModel ViewModel { get; }

        public AboutDialog() {
            InitializeComponent();
            DataContext = ViewModel = new AboutViewModel();
        }

        void OnOpened(object? sender, System.EventArgs e) {
            Dispatcher.UIThread.Post(
                () => WorkspaceScrollbarHelper.ApplyScrollViewer(
                    ContributorsScroll,
                    WorkspaceScrollbarHelper.UseClassicScrollbars),
                DispatcherPriority.Loaded);
        }

        void OnContributorPointerPressed(object? sender, PointerPressedEventArgs e) {
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) {
                return;
            }
            if (sender is not TextBlock block) {
                return;
            }
            var url = block.Tag as string;
            if (string.IsNullOrWhiteSpace(url) && block.DataContext is Models.ContributorEntry entry) {
                url = entry.ProfileUrl;
            }
            if (!string.IsNullOrWhiteSpace(url)) {
                OS.OpenWeb(url);
                e.Handled = true;
            }
        }

        void OnOpenRepository(object? sender, RoutedEventArgs e) => ViewModel.OpenRepository();

        void OnOpenLunaiRepository(object? sender, RoutedEventArgs e) => ViewModel.OpenLunaiRepository();

        void OnClose(object? sender, RoutedEventArgs e) => Close();
    }
}
