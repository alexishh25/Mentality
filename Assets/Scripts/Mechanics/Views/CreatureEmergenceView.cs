using System;
using Gameplay.Rhythm.Models;
using UnityEngine;

namespace Gameplay.Rhythm.Views
{
    // The appearance schedule is supplied separately from the creature's visual/attack cycle.
    public sealed class CreatureEmergenceView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Sprite[] standingFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] openMouthFrames = Array.Empty<Sprite>();
        [SerializeField] private Vector2 mouthOffset = new Vector2(0f, 0.58f);
        [SerializeField, Min(0.1f)] private float height = 2.4f;
        [SerializeField, Min(0.1f)] private float emergenceSeconds = 0.8f;
        [SerializeField, Min(0.1f)] private float standingSeconds = 2f;
        [SerializeField, Min(0.1f)] private float retreatSeconds = 0.6f;
        [SerializeField, Min(1f)] private float framesPerSecond = 8f;

        private AudioConductorService _clock;
        private Transform _player;
        private Vector3 _ground;
        private double _start;
        private bool _initialized;
        private Action<Vector3, int, double> _shoot;
        private RhythmGameModel _model;
        private int _shotCount, _nextShot;
        private double _shotInterval;
        private double[] _shotOffsets;
        private AudioSource _appearanceAudio;
        private bool _audioPaused;
        private bool _continuousAttack;
        private double PreparationSeconds => _continuousAttack ? 0d : 0.35d;
        private double RecoverySeconds => _continuousAttack ? 0.12d : 0.35d;
        public float EmergenceSeconds => emergenceSeconds;
        public bool IsAttacking { get; private set; }
        public Vector3 MouthPosition => body.transform.TransformPoint(mouthOffset);
        public bool HasEmerged { get; private set; }
        public bool HasFinished { get; private set; }
        public Vector3 GroundPosition => _ground;

        public void Initialize(Vector3 groundPosition, Transform player, AudioConductorService clock = null)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (body == null || standingFrames == null || standingFrames.Length != 5
                || Array.Exists(standingFrames, frame => frame == null))
                throw new InvalidOperationException("La criatura necesita cinco cuadros y un SpriteRenderer.");
            _ground = groundPosition;
            _player = player;
            _clock = clock;
            _start = Now;
            _initialized = true;
            HasFinished = false;
            HasEmerged = false;
            _nextShot = 0;
            transform.position = _ground;
            body.transform.localScale = Vector3.one * height;
            body.enabled = true;
            SampleAt(0d);
        }

        private double Now => _clock == null ? Time.timeAsDouble : _clock.CurrentSongTime;

        public void PlayAppearanceSound(AudioClip clip, float volume)
        {
            if (!_initialized || HasFinished || clip == null) return;
            if (_appearanceAudio == null)
            {
                _appearanceAudio = gameObject.AddComponent<AudioSource>();
                _appearanceAudio.playOnAwake = false;
                _appearanceAudio.loop = false;
                _appearanceAudio.spatialBlend = 0f;
            }
            _appearanceAudio.Stop();
            _appearanceAudio.clip = clip;
            _appearanceAudio.volume = Mathf.Clamp01(volume);
            _audioPaused = false;
            _appearanceAudio.Play();
        }

        public void ConfigureAttack(int shotCount, double intervalSeconds,
            Action<Vector3, int, double> shoot, RhythmGameModel model = null)
        {
            if (shotCount < 1 || shotCount > 8 || double.IsNaN(intervalSeconds)
                || double.IsInfinity(intervalSeconds) || intervalSeconds < 0.15d || intervalSeconds > 2d)
                throw new ArgumentOutOfRangeException(nameof(shotCount));
            if (openMouthFrames.Length != 5 || Array.Exists(openMouthFrames, frame => frame == null))
                throw new InvalidOperationException("Asignar los cinco cuadros de boca abierta.");
            _shoot = shoot ?? throw new ArgumentNullException(nameof(shoot));
            _continuousAttack = false;
            _shotCount = shotCount;
            _shotInterval = intervalSeconds;
            _shotOffsets = null;
            _nextShot = 0;
            _model = model;
        }

        public void ConfigureMusicalAttack(double appearanceTime, double[] shotTimes,
            Action<Vector3, int, double> shoot, RhythmGameModel model, bool continuousAttack = false)
        {
            if (shotTimes == null || shotTimes.Length == 0) throw new ArgumentException("Rafaga vacia.");
            ConfigureAttack(shotTimes.Length, 0.25d, shoot, model);
            _continuousAttack = continuousAttack;
            _start = appearanceTime;
            _shotOffsets = new double[shotTimes.Length];
            for (int i = 0; i < shotTimes.Length; i++)
            {
                double offset = shotTimes[i] - appearanceTime;
                if (double.IsNaN(offset) || double.IsInfinity(offset) || offset < emergenceSeconds + PreparationSeconds - 0.000001d
                    || (i > 0 && offset - _shotOffsets[i - 1] < 0.15d))
                    throw new ArgumentException("Tiempos de rafaga invalidos.");
                _shotOffsets[i] = offset;
            }
        }

        private void Update()
        {
            if (!_initialized || HasFinished) return;
            if (_player == null || (_model != null && !_model.IsPlaying)
                || (_clock != null && (!_clock.IsPlaying || Now < _start - 0.001d)))
            { Cancel(); return; }
            bool paused = (_model != null && _model.IsPaused) || (_clock != null && (_clock.IsPaused || _clock.IsWaitingForTimeline));
            if (_appearanceAudio != null)
            {
                if (paused && !_audioPaused && _appearanceAudio.isPlaying)
                { _appearanceAudio.Pause(); _audioPaused = true; }
                else if (!paused && _audioPaused)
                { _appearanceAudio.UnPause(); _audioPaused = false; }
            }
            if (paused) return;
            AdvanceAt(Now - _start);
            if (HasFinished) Cancel();
        }

        public void AdvanceAt(double elapsed)
        {
            SampleAt(elapsed);
            if (_shoot == null || HasFinished || (_model != null && (!_model.IsPlaying || _model.IsPaused))) return;
            if (_nextShot >= _shotCount) return;
            double due = _shotOffsets == null ? emergenceSeconds + PreparationSeconds + _nextShot * _shotInterval : _shotOffsets[_nextShot];
            if (elapsed < due) return;
            int index = _nextShot++;
            // Skip stale shots after a long frame rather than unloading a whole burst at once.
            if (elapsed - due <= 0.12d) _shoot(MouthPosition, index, _start + elapsed);
        }

        // Also permits deterministic verification without waiting for an animation.
        public void SampleAt(double elapsed)
        {
            if (!_initialized || double.IsNaN(elapsed) || double.IsInfinity(elapsed) || elapsed < 0d)
                throw new ArgumentOutOfRangeException(nameof(elapsed));
            double retreatStart = emergenceSeconds + standingSeconds;
            if (_shoot != null)
            {
                double attackEnd = (_shotOffsets == null ? emergenceSeconds + PreparationSeconds + (_shotCount - 1) * _shotInterval
                    : _shotOffsets[_shotOffsets.Length - 1]) + RecoverySeconds;
                retreatStart = _continuousAttack ? attackEnd : Math.Max(retreatStart, attackEnd);
            }
            HasEmerged = elapsed >= emergenceSeconds;
            HasFinished = elapsed >= retreatStart + retreatSeconds;
            float visibility = elapsed < emergenceSeconds
                ? Mathf.SmoothStep(0f, 1f, (float)(elapsed / emergenceSeconds))
                : 1f - Mathf.SmoothStep(0f, 1f, (float)((elapsed - retreatStart) / retreatSeconds));
            body.transform.localPosition = Vector3.down * height * (1f - visibility);
            double attackPoseStart = _continuousAttack ? Math.Max(0d, emergenceSeconds - 0.12d) : emergenceSeconds + 0.2d;
            IsAttacking = _shoot != null && elapsed >= attackPoseStart && elapsed < retreatStart;
            var frames = IsAttacking ? openMouthFrames : standingFrames;
            body.sprite = frames[(int)(elapsed * framesPerSecond % frames.Length)];
            body.enabled = !HasFinished;
            Vector3 facing = _player.position - _ground;
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
        }

        public void Cancel()
        {
            HasFinished = true;
            _shoot = null;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        private void OnDisable()
        {
            if (_appearanceAudio != null) _appearanceAudio.Stop();
            _audioPaused = false;
        }
    }
}
