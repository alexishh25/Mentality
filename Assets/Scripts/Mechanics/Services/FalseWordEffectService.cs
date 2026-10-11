using System;
using System.Collections.Generic;
using Gameplay.Rhythm.Models;

namespace Gameplay.Rhythm.Services
{
    // Los efectos audiovisuales se suscriben a esta entrada compartida de la sesion.
    public sealed class FalseWordEffectService : IDisposable
    {
        public event Action<WordTargetKind, double> Requested;
        public event Action Cleared;
        public event Action PresentationChanged;
        private readonly RhythmGameModel _model;
        private readonly AudioConductorService _conductor;
        private bool _disposed;
        private bool _isClearing;

        public FalseWordEffectService(RhythmGameModel model, AudioConductorService conductor)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _conductor = conductor ?? throw new ArgumentNullException(nameof(conductor));
        }

        public void Request(WordTargetKind kind)
        {
            if (!Enum.IsDefined(typeof(WordTargetKind), kind) || kind == WordTargetKind.Normal)
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (_disposed || _isClearing || !_model.IsPlaying || _model.IsPaused || !_conductor.IsPlaying
                || _conductor.IsPaused || _conductor.IsWaitingForTimeline) return;
            Requested?.Invoke(kind, _conductor.CurrentSongTime);
        }

        public void Clear()
        {
            if (_disposed || _isClearing) return;
            _isClearing = true;
            try { NotifyAll(Cleared); }
            finally { _isClearing = false; }
        }

        public void RefreshPresentation()
        {
            if (!_disposed) NotifyAll(PresentationChanged);
        }

        private static void NotifyAll(Action listeners)
        {
            if (listeners == null) return;
            List<Exception> failures = null;
            foreach (Action listener in listeners.GetInvocationList())
            {
                try { listener(); }
                catch (Exception error)
                {
                    if (failures == null) failures = new List<Exception>();
                    failures.Add(error);
                }
            }
            // Un receptor defectuoso no impide limpiar o pausar los demas efectos.
            if (failures != null) throw new AggregateException("Fallo al coordinar los efectos de palabras falsas.", failures);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { NotifyAll(Cleared); }
            finally
            {
                Requested = null;
                Cleared = null;
                PresentationChanged = null;
            }
        }
    }
}
