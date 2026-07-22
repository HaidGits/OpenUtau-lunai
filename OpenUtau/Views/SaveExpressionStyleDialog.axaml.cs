using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using OpenUtau.App.Controls;
using OpenUtau.App.ViewModels;
using OpenUtau.Core.Util;

namespace OpenUtau.App.Views;

public partial class SaveExpressionStyleDialog : Window {
    public Func<ExpressionStyleYaml, bool>? onFinish;

    public SaveExpressionStyleViewModel ViewModel { get; } = new();

    public SaveExpressionStyleDialog() {
        InitializeComponent();
        DataContext = ViewModel;
        OkButton.Click += (_, _) => Finish();
        CancelButton.Click += (_, _) => Close();
        NameBox.AttachedToVisualTree += (_, _) => {
            NameBox.SelectAll();
            NameBox.Focus();
        };
        Opened += (_, _) => ApplyOverlayScrollbar();
        ValuesScroll.AttachedToVisualTree += (_, _) => ApplyOverlayScrollbar();
    }

    void ApplyOverlayScrollbar() {
        if (!WorkspaceScrollbarHelper.IsInVisualTree(ValuesScroll)) {
            return;
        }
        WorkspaceScrollbarHelper.ApplyScrollViewer(ValuesScroll, classic: false);
        Dispatcher.UIThread.Post(
            () => WorkspaceScrollbarHelper.ApplyScrollViewer(ValuesScroll, classic: false),
            DispatcherPriority.Loaded);
    }

    void OnStyleValuePointerPressed(object? sender, PointerPressedEventArgs e) {
        if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed) {
            return;
        }
        if (sender is not Control { DataContext: ExpressionStyleValueItem item }) {
            return;
        }
        item.ResetToFactory();
        e.Handled = true;
    }

    void Finish() {
        var style = ViewModel.BuildStyle();
        if (style == null) {
            return;
        }
        if (onFinish?.Invoke(style) != false) {
            Close();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e) {
        if (e.Key == Key.Escape) {
            e.Handled = true;
            Close();
        } else if (e.Key == Key.Enter && !(e.Source is TextBox { AcceptsReturn: true })) {
            base.OnKeyDown(e);
        } else {
            base.OnKeyDown(e);
        }
    }
}
