using System;
using Gameplay.Rhythm.Interfaces;
using Gameplay.Rhythm.Models;
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

        public bool IsResolved { get; private set; }

        public TargetPresenter(ITargetView view, RhythmGameModel model,
            Transform playerTarget, float travelSpeed, float arrivalDistance)
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

        private void OnTouched()
        {
            if (IsResolved)
                return;

            if (!_model.IsPlaying || _playerTarget == null)
            {
                Cancel();
                return;
            }

            // Una palabra dentro del radio ya es un fallo, aunque el clic llegue antes del Update.
            Resolve(!HasReachedPlayer());
        }

        private void Resolve(bool hit)
        {
            Dispose();
            if (hit)
                _model.RegisterHit();
            else
                _model.RegisterMiss();

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
