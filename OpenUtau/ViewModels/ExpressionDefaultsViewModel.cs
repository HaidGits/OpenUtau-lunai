using System;
using System.Collections.ObjectModel;
using System.Linq;
using DynamicData.Binding;
using OpenUtau.App;
using OpenUtau.Core;
using OpenUtau.Core.Format;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace OpenUtau.App.ViewModels {
    public class ExpressionDefaultItem : ReactiveObject {
        public string Abbr { get; }
        [Reactive] public string Name { get; set; }
        [Reactive] public float Min { get; set; }
        [Reactive] public float Max { get; set; }
        [Reactive] public float DefaultValue { get; set; }
        [Reactive] public float PlayheadValue { get; set; }
        [Reactive] public bool ShowPlayheadMarker { get; set; }

        public ExpressionDefaultItem(UExpressionDescriptor descriptor) {
            Abbr = descriptor.abbr;
            Name = ExpressionSuggestionSync.GetPanelDisplayName(descriptor);
            Min = descriptor.min;
            Max = descriptor.max;
            DefaultValue = SetExpressionCustomDefaultCommand.GetEffectiveDefault(descriptor);
            PlayheadValue = DefaultValue;
            ShowPlayheadMarker = false;
        }

        public void SyncFromDescriptor(UExpressionDescriptor descriptor) {
            Name = ExpressionSuggestionSync.GetPanelDisplayName(descriptor);
            Min = descriptor.min;
            Max = descriptor.max;
            DefaultValue = SetExpressionCustomDefaultCommand.GetEffectiveDefault(descriptor);
        }
    }

    public class ExpressionDefaultsViewModel : ViewModelBase, ICmdSubscriber {
        public ObservableCollectionExtended<ExpressionDefaultItem> ParameterItems { get; } = new();
        public ObservableCollectionExtended<ExpressionDefaultItem> VoiceColorItems { get; } = new();
        public ObservableCollectionExtended<string> VoiceColorOptions { get; } = new();

        [Reactive] public bool HasParameters { get; private set; }
        [Reactive] public bool HasVoiceColors { get; private set; }
        [Reactive] public bool ShowDefaultVoiceColorPicker { get; private set; }
        [Reactive] public int SelectedVoiceColorIndex { get; set; }

        UVoicePart? part;
        int trackNo = -1;
        bool applyingSlider;
        bool applyingVoiceColor;
        string? pendingAbbr;
        float pendingOldValue;
        float pendingNewValue;

        public ExpressionDefaultsViewModel() {
            DocManager.Inst.AddSubscriber(this);
            this.WhenAnyValue(vm => vm.SelectedVoiceColorIndex)
                .Subscribe(index => {
                    if (!applyingVoiceColor) {
                        CommitDefaultVoiceColor(index);
                    }
                });
            RefreshList();
            RefreshPlayheadValues();
        }

        public void AttachPart(UVoicePart? voicePart) {
            part = voicePart;
            trackNo = voicePart?.trackNo ?? -1;
            SyncSuggestionsForOpenTrack();
            RefreshList();
            RefreshPlayheadValues();
        }

        public void BeginEdit(ExpressionDefaultItem item) {
            if (pendingAbbr != null && pendingAbbr != item.Abbr) {
                CommitPendingEdit();
            }
            // PointerPressed can arrive after the first Value change; keep the original baseline.
            if (pendingAbbr == item.Abbr) {
                return;
            }
            pendingAbbr = item.Abbr;
            pendingOldValue = item.DefaultValue;
            pendingNewValue = item.DefaultValue;
        }

        public void PreviewEdit(ExpressionDefaultItem item, float value) {
            applyingSlider = true;
            item.DefaultValue = value;
            pendingAbbr = item.Abbr;
            pendingNewValue = value;
            ApplyLiveDefault(item.Abbr, value);
            applyingSlider = false;
        }

        public void EndEdit(ExpressionDefaultItem item) {
            pendingAbbr = item.Abbr;
            pendingNewValue = item.DefaultValue;
            CommitPendingEdit();
        }

        /// <summary>
        /// Reset project default back to the built-in / singer-suggested factory default.
        /// </summary>
        public void ResetToFactoryDefault(ExpressionDefaultItem item) {
            if (item == null) {
                return;
            }
            if (pendingAbbr != null) {
                CommitPendingEdit();
            }
            var project = DocManager.Inst.Project;
            if (!project.expressions.TryGetValue(item.Abbr, out var descriptor)) {
                return;
            }
            // Refresh factory defaultValue from singer suggestions when available.
            if (trackNo >= 0 && trackNo < project.tracks.Count) {
                ExpressionSuggestionSync.UpsertSuggested(project, project.tracks[trackNo]);
            }
            float factory = SetExpressionCustomDefaultCommand.GetFactoryDefault(descriptor);
            float current = SetExpressionCustomDefaultCommand.GetEffectiveDefault(descriptor);
            if (Math.Abs(current - factory) < 0.0001f) {
                return;
            }
            applyingSlider = true;
            item.DefaultValue = factory;
            applyingSlider = false;
            ApplyLiveDefault(item.Abbr, factory);
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new SetExpressionCustomDefaultCommand(project, item.Abbr, factory, current));
            DocManager.Inst.EndUndoGroup();
            MessageBus.Current.SendMessage(new NotesRefreshEvent());
        }

        void ApplyLiveDefault(string abbr, float value) {
            var project = DocManager.Inst.Project;
            if (!project.expressions.TryGetValue(abbr, out var descriptor)) {
                return;
            }
            SetExpressionCustomDefaultCommand.SetEffectiveDefault(descriptor, value);
            if (string.Equals(abbr, Ustx.CLR, StringComparison.OrdinalIgnoreCase)) {
                foreach (var track in project.tracks) {
                    if (track.VoiceColorExp != null) {
                        float min = track.VoiceColorExp.min;
                        float max = track.VoiceColorExp.max;
                        float clamped = max < min ? value : Math.Clamp(value, min, max);
                        SetExpressionCustomDefaultCommand.SetEffectiveDefault(track.VoiceColorExp, clamped);
                    }
                }
            }
            MessageBus.Current.SendMessage(new NotesRefreshEvent());
        }

        void CommitPendingEdit() {
            if (pendingAbbr == null) {
                return;
            }
            var abbr = pendingAbbr;
            var oldValue = pendingOldValue;
            var newValue = pendingNewValue;
            pendingAbbr = null;
            if (Math.Abs(oldValue - newValue) < 0.0001f) {
                return;
            }
            var project = DocManager.Inst.Project;
            if (!project.expressions.ContainsKey(abbr)) {
                return;
            }
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new SetExpressionCustomDefaultCommand(project, abbr, newValue, oldValue));
            DocManager.Inst.EndUndoGroup();
            MessageBus.Current.SendMessage(new NotesRefreshEvent());
        }

        void CommitDefaultVoiceColor(int index) {
            var project = DocManager.Inst.Project;
            if (trackNo < 0 || trackNo >= project.tracks.Count || !ShowDefaultVoiceColorPicker) {
                return;
            }
            var track = project.tracks[trackNo];
            if (track.VoiceColorExp == null || index < 0 || index > track.VoiceColorExp.max) {
                return;
            }
            float current = SetExpressionCustomDefaultCommand.GetEffectiveDefault(track.VoiceColorExp);
            if (Math.Abs(current - index) < 0.0001f) {
                return;
            }
            DocManager.Inst.StartUndoGroup();
            DocManager.Inst.ExecuteCmd(new SetExpressionCustomDefaultCommand(project, Ustx.CLR, index, current));
            DocManager.Inst.EndUndoGroup();
            MessageBus.Current.SendMessage(new NotesRefreshEvent());
        }

        public void SyncSuggestionsForOpenTrack() {
            var project = DocManager.Inst.Project;
            if (trackNo < 0 || trackNo >= project.tracks.Count) {
                return;
            }
            if (ExpressionSuggestionSync.UpsertSuggested(project, project.tracks[trackNo])) {
                DocManager.Inst.ExecuteCmd(new ExpressionsSuggestedNotification());
            }
        }

        void RefreshList() {
            var project = DocManager.Inst.Project;
            if (trackNo < 0 || trackNo >= project.tracks.Count) {
                ParameterItems.Clear();
                VoiceColorItems.Clear();
                VoiceColorOptions.Clear();
                HasParameters = false;
                HasVoiceColors = false;
                ShowDefaultVoiceColorPicker = false;
                return;
            }
            var track = project.tracks[trackNo];
            RebuildItemList(ParameterItems, ExpressionSuggestionSync.GetPanelParameterDescriptors(project, track));
            RebuildItemList(VoiceColorItems, ExpressionSuggestionSync.GetPanelVoiceColorDescriptors(project, track));
            HasParameters = ParameterItems.Count > 0;
            HasVoiceColors = VoiceColorItems.Count > 0;

            VoiceColorOptions.Clear();
            applyingVoiceColor = true;
            if (track.VoiceColorExp?.options != null && track.VoiceColorExp.options.Length > 0) {
                foreach (var option in track.VoiceColorExp.options) {
                    VoiceColorOptions.Add(option);
                }
                ShowDefaultVoiceColorPicker = true;
                SelectedVoiceColorIndex = Math.Clamp(
                    (int)Math.Round(SetExpressionCustomDefaultCommand.GetEffectiveDefault(track.VoiceColorExp)),
                    0,
                    track.VoiceColorExp.options.Length - 1);
                HasVoiceColors = true;
            } else {
                ShowDefaultVoiceColorPicker = false;
                SelectedVoiceColorIndex = 0;
            }
            applyingVoiceColor = false;
        }

        static void RebuildItemList(
            ObservableCollectionExtended<ExpressionDefaultItem> target,
            System.Collections.Generic.List<UExpressionDescriptor> descriptors) {
            var byAbbr = target.ToDictionary(i => i.Abbr, StringComparer.OrdinalIgnoreCase);
            target.Clear();
            foreach (var descriptor in descriptors) {
                if (byAbbr.TryGetValue(descriptor.abbr, out var existing)) {
                    existing.SyncFromDescriptor(descriptor);
                    target.Add(existing);
                } else {
                    target.Add(new ExpressionDefaultItem(descriptor));
                }
            }
        }

        public void RefreshPlayheadValues() {
            RefreshPlayheadValuesFor(ParameterItems);
            RefreshPlayheadValuesFor(VoiceColorItems);
        }

        void RefreshPlayheadValuesFor(ObservableCollectionExtended<ExpressionDefaultItem> items) {
            var project = DocManager.Inst.Project;
            if (part == null || trackNo < 0 || trackNo >= project.tracks.Count) {
                foreach (var item in items) {
                    item.ShowPlayheadMarker = false;
                    item.PlayheadValue = item.DefaultValue;
                }
                return;
            }
            var track = project.tracks[trackNo];
            int localTick = DocManager.Inst.playPosTick - part.position;
            bool inPart = localTick >= 0 && localTick <= part.Duration;
            UPhoneme? phoneme = null;
            if (inPart) {
                phoneme = part.phonemes.FirstOrDefault(p =>
                    !p.Error && p.Parent != null &&
                    p.position <= localTick && localTick <= p.End);
            }
            foreach (var item in items) {
                if (!track.TryGetExpDescriptor(project, item.Abbr, out var descriptor)) {
                    item.ShowPlayheadMarker = false;
                    continue;
                }
                float baseline = SetExpressionCustomDefaultCommand.GetEffectiveDefault(descriptor);
                float value = baseline;
                bool hasOverride = false;
                if (descriptor.type == UExpressionType.Curve) {
                    var curve = part.curves?.FirstOrDefault(c =>
                        string.Equals(c.abbr, item.Abbr, StringComparison.OrdinalIgnoreCase));
                    if (curve != null && curve.descriptor != null && inPart && curve.xs.Count > 0) {
                        value = curve.Sample(localTick);
                        hasOverride = Math.Abs(value - baseline) > 0.0001f;
                    }
                } else if (descriptor.type == UExpressionType.Numerical && phoneme != null) {
                    var (expValue, overridden) = phoneme.GetExpression(project, track, item.Abbr);
                    value = expValue;
                    hasOverride = overridden || Math.Abs(value - baseline) > 0.0001f;
                }
                item.PlayheadValue = Math.Clamp(value, item.Min, item.Max);
                item.ShowPlayheadMarker = inPart && hasOverride;
            }
        }

        public void OnNext(UCommand cmd, bool isUndo) {
            if (cmd is LoadPartNotification loadPart) {
                AttachPart(loadPart.part as UVoicePart);
                return;
            }
            if (cmd is LoadProjectNotification) {
                part = null;
                trackNo = -1;
                ParameterItems.Clear();
                VoiceColorItems.Clear();
                VoiceColorOptions.Clear();
                HasParameters = false;
                HasVoiceColors = false;
                ShowDefaultVoiceColorPicker = false;
                return;
            }
            if (cmd is TrackChangeSingerCommand changeSinger) {
                if (changeSinger.track.TrackNo == trackNo) {
                    SyncSuggestionsForOpenTrack();
                    RefreshList();
                    RefreshPlayheadValues();
                }
                return;
            }
            if (cmd is TrackChangeRenderSettingCommand changeRenderer) {
                if (changeRenderer.track.TrackNo == trackNo) {
                    SyncSuggestionsForOpenTrack();
                    RefreshList();
                    RefreshPlayheadValues();
                }
                return;
            }
            if (cmd is ConfigureExpressionsCommand ||
                cmd is ExpressionsSuggestedNotification ||
                cmd is SingersRefreshedNotification) {
                RefreshList();
                RefreshPlayheadValues();
                return;
            }
            if (cmd is SetExpressionCustomDefaultCommand setDefault && !applyingSlider) {
                applyingVoiceColor = string.Equals(setDefault.Abbr, Ustx.CLR, StringComparison.OrdinalIgnoreCase);
                RefreshList();
                applyingVoiceColor = false;
                RefreshPlayheadValues();
                return;
            }
            if (cmd is SetPlayPosTickNotification ||
                cmd is SetCurveCommand ||
                cmd is SetNotesSameExpressionCommand ||
                cmd is PhonemizedNotification) {
                RefreshPlayheadValues();
            }
        }
    }
}
