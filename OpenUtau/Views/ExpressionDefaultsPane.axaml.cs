using System;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using OpenUtau.App.Controls;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;
using ReactiveUI;

namespace OpenUtau.App.Views {
    public partial class ExpressionDefaultsPane : UserControl {
        int scrollStyleApplyGeneration;

        public ExpressionDefaultsPane() {
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
            WorkspaceScrollbarHelper.ApplyScrollViewer(ContentScroll, WorkspaceScrollbarHelper.UseClassicScrollbars);
        }

        bool IsHostedInPianoRollDock() {
            return this.GetVisualAncestors().OfType<PianoRoll>().Any();
        }

        void OnCloseDockedPanel(object? sender, RoutedEventArgs e) {
            var pianoRoll = this.GetVisualAncestors().OfType<PianoRoll>().FirstOrDefault();
            if (pianoRoll?.ViewModel != null) {
                pianoRoll.ViewModel.ShowExpressionDefaultsPanel = false;
            }
        }

        void OnParameterNamePressed(object? sender, PointerPressedEventArgs e) {
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) {
                return;
            }
            if (sender is not Control { DataContext: ExpressionDefaultItem item }) {
                return;
            }
            var pianoRoll = this.GetVisualAncestors().OfType<PianoRoll>().FirstOrDefault();
            if (pianoRoll?.ViewModel == null) {
                return;
            }
            pianoRoll.ViewModel.NotesViewModel.ShowExpressions = true;
            int selectorIndex = DocManager.Inst.Project.expPrimary;
            DocManager.Inst.ExecuteCmd(new SelectExpressionNotification(item.Abbr, selectorIndex, true));
            e.Handled = true;
        }
    }
}
