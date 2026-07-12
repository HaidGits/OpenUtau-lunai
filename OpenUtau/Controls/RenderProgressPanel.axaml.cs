using System;
using Avalonia;
using Avalonia.Controls;

namespace OpenUtau.App.Controls {
    public partial class RenderProgressPanel : UserControl {
        public static readonly StyledProperty<double> ProgressProperty =
            AvaloniaProperty.Register<RenderProgressPanel, double>(nameof(Progress));

        public static readonly StyledProperty<string?> ProgressTextProperty =
            AvaloniaProperty.Register<RenderProgressPanel, string?>(nameof(ProgressText));

        public double Progress {
            get => GetValue(ProgressProperty);
            set => SetValue(ProgressProperty, value);
        }

        public string? ProgressText {
            get => GetValue(ProgressTextProperty);
            set => SetValue(ProgressTextProperty, value);
        }

        static RenderProgressPanel() {
            ProgressProperty.Changed.AddClassHandler<RenderProgressPanel>((panel, _) => panel.UpdateFillLayout());
        }

        public RenderProgressPanel() {
            InitializeComponent();
            HostGrid.SizeChanged += (_, _) => UpdateFillLayout();
            ProgressRow.SizeChanged += (_, _) => UpdateFillLayout();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) {
            base.OnAttachedToVisualTree(e);
            UpdateFillLayout();
        }

        void UpdateFillLayout() {
            if (FillClip == null || ProgressRow == null) {
                return;
            }
            double width = ProgressRow.Bounds.Width;
            if (width <= 0 || double.IsNaN(width)) {
                FillClip.Width = 0;
                return;
            }
            FillClip.Width = Math.Max(0, width * Math.Clamp(Progress, 0, 100) / 100.0);
        }
    }
}
