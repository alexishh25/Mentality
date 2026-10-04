using System;
using Gameplay.Rhythm.Interfaces;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Services;
using UnityEngine;

namespace Gameplay.Rhythm.Presenters
{
    public class TargetPresenter : IDisposable
    {
        private readonly ITargetView _view;
        private readonly RhythmGameModel _model;
        private readonly Transform _playerTarget;
        private readonly float _travelSpeed;
        private readonly float _arrivalDistance;
        private readonly AudioConductorService _conductor;
        private readonly Vector3 _spawnPosition;
        private readonly Vector3 _arrivalDirection;
        private readonly AimEvasionMotion _evasion;

        public bool IsResolved { get; private set; }
        public TargetTiming Timing { get; }

        public TargetPresenter(ITargetView view, RhythmGameModel model,
            Transform playerTarget, float travelSpeed, float arrivalDistance, TargetTiming timing = null,
            AudioConductorService conductor = null, AimEvasionMotion evasion = null)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            if (playerTarget == null)
                throw new ArgumentNullException(nameof(playerTarget));
            if (float.IsNaN(travelSpeed) || float.IsInfinity(travelSpeed) || travelSpeed <= 0f)
                throw new ArgumentOutOfRangeException(nameof(travelSpeed));
            if (float.IsNaN(arrivalDistance) || float.IsInfinity(arrivalDistance) || arrivalDistance <= 0f)
                throw new ArgumentOutOfRangeException(nameof(arrivalDistance));

            _playerTarget = playerTarget;
            _travelSpeed = travelSpeed;
            _arrivalDistance = arrivalDistance;
            Timing = timing;
            _conductor = conductor;
            _evasion = evasion;
            _spawnPosition = _view.Position;
            _arrivalDirection = (_spawnPosition - _playerTarget.position).normalized;
            if (Timing != null)
            {
                if (_conductor == null)
                    throw new ArgumentNullException(nameof(conductor), "Un objetivo musical necesita el reloj DSP.");
                if (Timing.LeadTime <= 0d)
                    throw new ArgumentOutOfRangeException(nameof(timing), "El recorrido necesita anticipacion positiva.");
                if ((_spawnPosition - _playerTarget.position).sqrMagnitude <= _arrivalDistance * _arrivalDistance)
                    throw new ArgumentException("Un objetivo musical debe aparecer fuera del radio de llegada.", nameof(view));
                UpdateMusicalPosition(_conductor.CurrentSongTime);
            }
            _view.SetTouchListener(OnTouched);
            _view.FacePlayer(_playerTarget.position);
        }

        public void Tick(float deltaTime)
        {
            if (IsResolved)
                return;

            if (!_model.IsPlaying || _playerTarget == null)
            {
                Cancel();
                return;
            }

            if (_model.IsPaused) return;
            if (_conductor != null && (_conductor.IsPaused || _conductor.IsWaitingForTimeline)) return;

            if (Timing != null)
            {
                if (!_conductor.IsPlaying)
                {
                    Cancel();
                    return;
                }
                double songTime = _conductor.CurrentSongTime;
                UpdateMusicalPosition(songTime);
                if (songTime >= Timing.HitTime)
                    ResolveJudgment(RhythmJudgment.Miss, songTime, null);
                return;
            }

            if (HasReachedPlayer())
            {
                Resolve(false);
                return;
            }

            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f)
                return;

            Vector3 destination = _playerTarget.position;
            _view.SetPosition(Vector3.MoveTowards(_view.Position, destination, _travelSpeed * deltaTime));
            _view.FacePlayer(destination);

            if (HasReachedPlayer())
                Resolve(false);
        }

        private bool HasReachedPlayer()
        {
            return (_view.Position - _playerTarget.position).sqrMagnitude
                <= _arrivalDistance * _arrivalDistance;
        }

        private void UpdateMusicalPosition(double songTime)
        {
            double progress = Timing.ProgressAt(songTime);
            float clampedProgress = (float)Math.Max(0d, Math.Min(1d, progress));
            Vector3 arrivalPosition = _playerTarget.position + _arrivalDirection * _arrivalDistance;
            Vector3 position = Vector3.Lerp(_spawnPosition, arrivalPosition, clampedProgress);
            if (_evasion != null && Timing.MotionProfile != null)
            {
                double frenzy = Timing.MotionProfile.FrenzyAt(songTime);
                float radians = (float)(_evasion.AngleAt(songTime, frenzy) * Math.PI / 180d);
                Vector3 tangent = new Vector3(-_arrivalDirection.z, 0f, _arrivalDirection.x);
                Vector3 direction = _arrivalDirection * Mathf.Cos(radians) + tangent * Mathf.Sin(radians);
                float radius = (position - _playerTarget.position).magnitude;
                position = _playerTarget.position + direction * radius;
            }
            _view.SetPosition(position);
            _view.FacePlayer(_playerTarget.position);
        }

        public void SetAimed(bool aimed)
        {
            if (IsResolved || _model.IsPaused || !_model.IsPlaying || Timing?.MotionProfile == null || _evasion == null
                || !_conductor.IsPlaying || _conductor.IsPaused || _conductor.IsWaitingForTimeline) return;
            double time = _conductor.CurrentSongTime;
            _evasion.SetAimed(aimed, time, Timing.MotionProfile.FrenzyAt(time));
            UpdateMusicalPosition(time);
        }

        private void OnTouched()
        {
            if (IsResolved)
                return;

            if (!_model.IsPlaying || _playerTarget == null)
            {
                Cancel();
                return;
            }

            if (_model.IsPaused || (_conductor != null && (_conductor.IsPaused || _conductor.IsWaitingForTimeline))) return;

            if (Timing != null)
            {
                if (!_conductor.IsPlaying)
                {
                    Cancel();
                    return;
                }
                // Actualizar tambien en el clic evita usar una posicion del frame anterior.
                double songTime = _conductor.CurrentSongTime;
                UpdateMusicalPosition(songTime);
                // El instante musical indica llegada: cualquier clic anterior intercepta el proyectil.
                ResolveJudgment(songTime < Timing.HitTime ? RhythmJudgment.Hit : RhythmJudgment.Miss, songTime, null);
                return;
            }

            // Una palabra dentro del radio ya es un fallo, aunque el clic llegue antes del Update.
            Resolve(!HasReachedPlayer());
        }

        private void Resolve(bool hit)
        {
            if (_conductor != null)
            {
                ResolveJudgment(hit ? RhythmJudgment.Hit : RhythmJudgment.Miss, _conductor.CurrentSongTime, null);
                return;
            }
            Dispose();
            if (hit)
                _model.RegisterHit();
            else
                _model.RegisterMiss();

            _view.DestroySelf();
        }

        private void ResolveJudgment(RhythmJudgment judgment, double songTime, double? error)
        {
            Dispose();
            _model.RegisterJudgment(judgment, songTime, error);
            _view.DestroySelf();
        }

        // Retirar objetivos al detener/reiniciar no penaliza al jugador.
        public void Cancel()
        {
            if (IsResolved)
                return;

            Dispose();
            _view.DestroySelf();
        }

        public void Dispose()
        {
            if (IsResolved)
                return;

            IsResolved = true;
            _view.SetTouchListener(null);
        }
    }
}
