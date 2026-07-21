using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;

namespace OpenUtau.App.Controls {
    public partial class PianoRollViewToggles : UserControl {
        bool syncingScroll;

        public PianoRollViewToggles() {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e) {
            if (MainTogglesScroll == null || MainTogglesVScroll == null) {
                return;
            }
            MainTogglesScroll.ScrollChanged += (_, _) => SyncScrollBarFromViewer();
            MainTogglesScroll.SizeChanged += (_, _) => SyncScrollBarFromViewer();
            MainTogglesScroll.LayoutUpdated += (_, _) => SyncScrollBarFromViewer();
            MainTogglesVScroll.Scroll += OnScrollBarScroll;
            // Content size can settle after first layout (DiffSinger toggles, etc.).
            Dispatcher.UIThread.Post(SyncScrollBarFromViewer, DispatcherPriority.Loaded);
            Dispatcher.UIThread.Post(SyncScrollBarFromViewer, DispatcherPriority.Background);
        }

        void OnScrollBarScroll(object? sender, ScrollEventArgs e) {
            if (syncingScroll || MainTogglesScroll == null || MainTogglesVScroll == null) {
                return;
            }
            syncingScroll = true;
            try {
                MainTogglesScroll.Offset = new Vector(MainTogglesScroll.Offset.X, MainTogglesVScroll.Value);
            } finally {
                syncingScroll = false;
            }
        }

        void SyncScrollBarFromViewer() {
            if (syncingScroll || MainTogglesScroll == null || MainTogglesVScroll == null) {
                return;
            }
            syncingScroll = true;
            try {
                double viewport = MainTogglesScroll.Viewport.Height;
                double extent = MainTogglesScroll.Extent.Height;
                double max = Math.Max(0, extent - viewport);
                MainTogglesVScroll.Maximum = max;
                MainTogglesVScroll.ViewportSize = Math.Max(1, viewport);
                MainTogglesVScroll.LargeChange = Math.Max(1, viewport);
                MainTogglesVScroll.SmallChange = 32;
                double value = Math.Clamp(MainTogglesScroll.Offset.Y, 0, max);
                if (Math.Abs(MainTogglesVScroll.Value - value) > 0.01) {
                    MainTogglesVScroll.Value = value;
                }
                MainTogglesVScroll.IsVisible = max > 0.5;
            } finally {
                syncingScroll = false;
            }
        }
    }
}
