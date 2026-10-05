using System;
using Gameplay.Ghosts.Services;
using Gameplay.Ghosts.Views;
using Gameplay.Rhythm.Models;
using UnityEngine;

namespace Gameplay.Ghosts.Presenters
{
    public sealed class GhostDecoyPresenter : IDisposable
    {
        private readonly GhostDecoyView _view;
        private readonly Transform _playerTarget;
        private readonly RhythmGameModel _session;
        private readonly IGhostConsequences _consequences;
        private readonly Vector3 _direction;
        private readonly Vector3 _exitPosition;
        private readonly float _speed;
        private bool _clicked;
        private bool _disposed;

        public GhostDecoyPresenter(GhostDecoyView view, Transform playerTarget,
            RhythmGameModel session, IGhostConsequences consequences, float speed, float passThroughDistance)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _playerTarget = playerTarget != null ? playerTarget : throw new ArgumentNullException(nameof(playerTarget));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _consequences = consequences ?? throw new ArgumentNullException(nameof(consequences));
            if (!IsPositiveFinite(speed)) throw new ArgumentOutOfRangeException(nameof(speed));
            if (!IsPositiveFinite(passThroughDistance)) throw new ArgumentOutOfRangeException(nameof(passThroughDistance));
            _speed = speed;

            Vector3 towardPlayer = _playerTarget.position - _view.Position;
            _direction = towardPlayer.sqrMagnitude > 0.0001f ? towardPlayer.normalized : Vector3.forward;
            _exitPosition = _playerTarget.position + _direction * passThroughDistance;
        }

        public void Tick(float deltaTime)
        {
            if (_disposed) return;
            if (!_session.IsPlaying || _session.IsPaused || _playerTarget == null)
            {
                if (!_session.IsPaused) Dispose();
                return;
            }
            if (deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;

            Vector3 next = Vector3.MoveTowards(_view.Position, _exitPosition, _speed * deltaTime);
            _view.SetPosition(next);
            if ((next - _exitPosition).sqrMagnitude <= 0.0001f) Dispose();
        }

        public void OnTouched()
        {
            if (_disposed || _clicked || !_session.IsPlaying || _session.IsPaused) return;
            _clicked = true;
            _consequences.OnGhostClicked();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_view != null) _view.DestroySelf();
        }

        private static bool IsPositiveFinite(float value) => value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
