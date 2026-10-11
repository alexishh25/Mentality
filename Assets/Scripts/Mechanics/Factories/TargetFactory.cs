using System;
using System.Collections.Generic;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Presenters;
using Gameplay.Rhythm.Services;
using Gameplay.Rhythm.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Gameplay.Rhythm.Factories
{
    public class TargetFactory : IDisposable
    {
        private readonly IObjectResolver _resolver;
        private readonly RhythmGameConfig _config;
        private readonly RhythmGameModel _model;
        private readonly Transform _playerTarget;
        private readonly AudioConductorService _conductor;
        private readonly FalseWordEffectService _falseWordEffects;
        private readonly BullicioSessionService _bullicio;
        private readonly List<TargetView> _targets = new List<TargetView>();
        private readonly List<SpawnRingView> _spawnRings = new List<SpawnRingView>();
        private readonly List<CreatureEmergenceView> _creatures = new List<CreatureEmergenceView>();
        private readonly HashSet<TargetView> _creatureWords = new HashSet<TargetView>();

        public bool HasClearCreatureRoute(Vector3 mouth)
        {
            _ = ActiveTargetCount;
            Vector3 direction = mouth - _playerTarget.position;
            foreach (var target in _targets)
            {
                float separation = Vector3.Angle(direction, target.Position - _playerTarget.position);
                float width = _config.Placement.TargetAngularWidthDegrees;
                // Keep the whole fan away from ordinary chart words; consecutive volley shots
                // may use opposite sides of the fan once their visible centers have separated.
                if (separation < width + (_creatureWords.Contains(target) ? 0f : 18f)) return false;
            }
            return true;
        }

        public void RegisterCreature(CreatureEmergenceView creature)
        {
            _creatures.RemoveAll(item => item == null || item.HasFinished);
            if (creature != null && !_creatures.Contains(creature)) _creatures.Add(creature);
        }

        public TargetView CreateCreatureWord(Vector3 mouth, int shotIndex, double songTime,
            MusicalActivityProfile motionProfile = null)
        {
            if (!_model.IsPlaying || _model.IsPaused || !_conductor.IsPlaying || _conductor.IsPaused) return null;
            if (_config.WordPrefabs == null || _config.WordPrefabs.Length == 0)
                throw new InvalidOperationException("Asigna al menos un prefab de palabra.");
            double distance = Vector3.Distance(mouth, _playerTarget.position) - _config.ArrivalDistance;
            if (distance <= 0d) return null;
            double baseLead = distance / _config.TravelSpeed;
            double arrival = songTime + baseLead;
            if (motionProfile != null)
            {
                double targetMotion = motionProfile.MotionTimeAt(songTime) + baseLead;
                double low = songTime, high = arrival;
                for (int i = 0; i < 48; i++)
                {
                    double midpoint = (low + high) * 0.5d;
                    if (motionProfile.MotionTimeAt(midpoint) < targetMotion) low = midpoint; else high = midpoint;
                }
                arrival = (low + high) * 0.5d;
            }
            var kind = FalseWordSelector.Select(_config.FalseWords, songTime + shotIndex * 0.137d, 0d, 0d, 1d);
            if (kind == WordTargetKind.Normal) return null;
            int prefabIndex = (int)(Math.Abs(Math.Round(songTime * 1000d)) % _config.WordPrefabs.Length);
            Vector3 direction = mouth - _playerTarget.position;
            float originAngle = Mathf.Abs(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg);
            float spread = Mathf.Clamp(_config.Placement.MaximumSideAngle - originAngle, 0f, 18f);
            var word = Create(_config.WordPrefabs[prefabIndex], mouth,
                new TargetTiming(songTime, arrival, motionProfile, shotIndex % 2 == 0 ? spread : -spread), kind, false);
            if (word != null) _creatureWords.Add(word);
            return word;
        }

        public TargetFactory(IObjectResolver resolver, RhythmGameConfig config,
            RhythmGameModel model, Transform playerTarget, AudioConductorService conductor,
            FalseWordEffectService falseWordEffects, BullicioSessionService bullicio)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            if (playerTarget == null)
                throw new ArgumentNullException(nameof(playerTarget));
            _playerTarget = playerTarget;
            _conductor = conductor ?? throw new ArgumentNullException(nameof(conductor));
            _falseWordEffects = falseWordEffects ?? throw new ArgumentNullException(nameof(falseWordEffects));
            _bullicio = bullicio ?? throw new ArgumentNullException(nameof(bullicio));
        }

        public TargetView CreateRandomPosition(TargetTiming timing = null)
        {
            return CreateAtAngle(UnityEngine.Random.Range(-135f, 135f), timing);
        }

        public int ActiveTargetCount
        {
            get
            {
                foreach (var target in _targets)
                    if (target != null) target.RefreshMusicalState();
                _targets.RemoveAll(target => target == null || target.IsResolved);
                _creatureWords.RemoveWhere(target => target == null || target.IsResolved);
                return _targets.Count;
            }
        }

        public TargetView CreateAtAngle(float angleDegrees, TargetTiming timing = null)
        {
            if (!_model.IsPlaying)
                return null;

            if (_playerTarget == null)
                throw new InvalidOperationException("No hay un punto de destino para las palabras.");
            if (_config.WordPrefabs == null || _config.WordPrefabs.Length == 0)
                throw new InvalidOperationException("Asigna al menos un prefab de palabra.");
            if (float.IsNaN(_config.SpawnDistance) || float.IsInfinity(_config.SpawnDistance)
                || _config.SpawnDistance <= _config.ArrivalDistance)
                throw new InvalidOperationException("SpawnDistance debe ser finita y mayor que ArrivalDistance.");
            if (float.IsNaN(angleDegrees) || float.IsInfinity(angleDegrees) || Mathf.Abs(angleDegrees) > 135f)
                throw new ArgumentOutOfRangeException(nameof(angleDegrees));

            int index = UnityEngine.Random.Range(0, _config.WordPrefabs.Length);
            // Arco de 270 grados centrado en el norte (+Z) de la arena.
            // Excluye los 90 grados del sur (-Z), independientemente del giro de la camara.
            float angle = angleDegrees * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            return Create(_config.WordPrefabs[index], _playerTarget.position + direction * _config.SpawnDistance, timing);
        }

        public TargetView Create(GameObject prefab, Vector3 position, TargetTiming timing = null,
            WordTargetKind? forcedKind = null, bool showSpawnRing = true)
        {
            if (!_model.IsPlaying)
                return null;

            if (prefab == null || prefab.GetComponent<TargetView>() == null
                || prefab.GetComponent<Collider>() == null)
                throw new InvalidOperationException("El prefab necesita TargetView y un Collider en su raiz.");

            GameObject instance = _resolver.Instantiate(prefab, position, Quaternion.identity);
            try
            {
                var view = instance.GetComponent<TargetView>();
                if (timing != null && _config.Placement != null && _config.Placement.Enabled)
                    view.ConfigureLaunchSize(_playerTarget.position, _config.Placement);
                var kind = forcedKind ?? (timing == null || _config.FalseWords == null ? WordTargetKind.Normal
                    : FalseWordSelector.Select(_config.FalseWords, timing.HitTime,
                        timing.MotionProfile == null ? 0d : timing.MotionProfile.FrenzyAt(timing.SpawnTime), _bullicio.ProbabilityBonus,
                        timing.MotionProfile?.ManualIntensity?.FalseProbabilityAt(timing.HitTime)));
                var presenter = new TargetPresenter(view, _model, _playerTarget,
                    _config.TravelSpeed, _config.ArrivalDistance, timing, _conductor,
                    timing == null || _config.Evasion == null ? null : new AimEvasionMotion(_config.Evasion, timing.HitTime), kind,
                    _falseWordEffects);
                view.Initialize(presenter);
                if (kind != WordTargetKind.Normal)
                    view.ConfigureDistortion(_config.FalseWords, _conductor);
                instance.SetActive(true);
                if (_config.ShowSpawnRing && showSpawnRing) CreateSpawnRing(view, position);
                _targets.RemoveAll(target => target == null || target.IsResolved);
                _targets.Add(view);
                return view;
            }
            catch
            {
                instance.SetActive(false);
                UnityEngine.Object.Destroy(instance);
                throw;
            }
        }

        public void ClearTargets()
        {
            foreach (var creature in _creatures) if (creature != null) creature.Cancel();
            _creatures.Clear();
            foreach (var target in _targets)
            {
                if (target != null)
                    target.Cancel();
            }
            _targets.Clear();
            _creatureWords.Clear();
            foreach (var ring in _spawnRings)
                if (ring != null) ring.Cancel();
            _spawnRings.Clear();
        }

        public void ResolveAtSongEnd(double songTime)
        {
            foreach (var target in _targets)
            {
                if (!_model.IsPlaying) break;
                if (target != null) target.ResolveAtSongEnd(songTime);
            }
        }

        private void CreateSpawnRing(TargetView view, Vector3 origin)
        {
            var effect = new GameObject("Spawn ring");
            try
            {
                Vector3 direction = origin - _playerTarget.position;
                effect.transform.SetPositionAndRotation(origin, direction.sqrMagnitude > 0.0001f
                    ? Quaternion.LookRotation(direction, Vector3.up) : Quaternion.identity);
                var box = view.GetComponent<BoxCollider>();
                Vector3 scale = view.transform.lossyScale;
                var radius = new Vector2(box.size.x * Mathf.Abs(scale.x) * 0.5f + 0.12f,
                    box.size.y * Mathf.Abs(scale.y) * 0.5f + 0.12f);
                var ring = effect.AddComponent<SpawnRingView>();
                ring.Initialize(_conductor, radius, _config.SpawnRingDurationSeconds);
                _spawnRings.RemoveAll(item => item == null);
                _spawnRings.Add(ring);
            }
            catch
            {
                UnityEngine.Object.Destroy(effect);
                throw;
            }
        }

        public void Dispose() => ClearTargets();
    }
}
