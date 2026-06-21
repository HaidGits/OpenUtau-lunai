using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace OpenUtau.App.Controls {
    public partial class PianoRollMainViewToggles : UserControl {
        public static readonly StyledProperty<bool> ShowDiffSingerToolbarProperty =
            AvaloniaProperty.Register<PianoRollMainViewToggles, bool>(
                nameof(ShowDiffSingerToolbar), true);

        public bool ShowDiffSingerToolbar {
            get => GetValue(ShowDiffSingerToolbarProperty);
            set => SetValue(ShowDiffSingerToolbarProperty, value);
        }

        public static readonly StyledProperty<Orientation> LayoutOrientationProperty =
            AvaloniaProperty.Register<PianoRollMainViewToggles, Orientation>(
                nameof(LayoutOrientation), Orientation.Vertical);

        public Orientation LayoutOrientation {
            get => GetValue(LayoutOrientationProperty);
            set => SetValue(LayoutOrientationProperty, value);
        }

        public PianoRollMainViewToggles() {
            InitializeComponent();
        }
    }
}
