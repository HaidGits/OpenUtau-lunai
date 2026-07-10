using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using OpenUtau.App.ViewModels;

namespace OpenUtau.App.Controls {
    public partial class PianoRollDiffSingerQualityToggles : UserControl {
        public static readonly StyledProperty<Orientation> LayoutOrientationProperty =
            AvaloniaProperty.Register<PianoRollDiffSingerQualityToggles, Orientation>(
                nameof(LayoutOrientation), Orientation.Vertical);

        public Orientation LayoutOrientation {
            get => GetValue(LayoutOrientationProperty);
            set => SetValue(LayoutOrientationProperty, value);
        }

        public PianoRollDiffSingerQualityToggles() {
            InitializeComponent();
        }

        void OnPresetToggleCheckedChanged(object? sender, RoutedEventArgs e) {
            if (sender is not ToggleButton toggle || DataContext is not PianoRollViewModel vm) {
                return;
            }
            bool shouldBeChecked = toggle switch {
                var t when t == HqPresetToggle => vm.DiffSingerHqPresetActive,
                var t when t == MqPresetToggle => vm.DiffSingerMqPresetActive,
                var t when t == LqPresetToggle => vm.DiffSingerLqPresetActive,
                _ => toggle.IsChecked ?? false,
            };
            if (toggle.IsChecked != shouldBeChecked) {
                toggle.IsChecked = shouldBeChecked;
            }
        }
    }
}
