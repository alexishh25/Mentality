using System;
using System.Collections.Generic;
using Gameplay.Rhythm.Models;
using Gameplay.Rhythm.Services;
using UnityEngine;

namespace Gameplay.Rhythm.Views
{
    // Static-scene validation data. Rebuild after changing scenery or word prefab geometry.
    [DisallowMultipleComponent]
    public sealed class AudienceWordRouteLayout : MonoBehaviour
    {
        [Serializable]
        public sealed class GeometryStamp
        {
            public MeshRenderer Renderer;
            public Matrix4x4 Matrix;
            public Mesh Mesh;
            public Bounds MeshBounds;
            public bool Visible;
            public bool Matches() => Renderer != null && Renderer.localToWorldMatrix == Matrix &&
                (Renderer.enabled && Renderer.gameObject.activeInHierarchy) == Visible &&
                Renderer.GetComponent<MeshFilter>()?.sharedMesh == Mesh && (Mesh == null || Mesh.bounds == MeshBounds);
        }
        [Serializable]
        public sealed class Route
        {
            public AudienceWordEmitter Emitter;
            public Vector3 Origin;
            public bool Visible;
            public bool Clear;
            public string Obstacle;
            public float LaunchClearance;
            public string Row;
        }
        [SerializeField] private Vector3 playerPosition;
        [SerializeField] private float arrivalDistance;
        [SerializeField] private float angularWidth;
        [SerializeField] private bool placementEnabled;
        [SerializeField] private int flightRevision;
        [SerializeField] private float evasionAngle;
        [SerializeField] private GameObject[] wordPrefabs = Array.Empty<GameObject>();
        [SerializeField] private Route[] routes = Array.Empty<Route>();
        [SerializeField] private GeometryStamp[] geometry = Array.Empty<GeometryStamp>();
        private Dictionary<AudienceWordEmitter, Route> lookup;
        private bool geometryChecked;
        private bool geometryValid;
        private string geometryIssue;
        public string LastRejection { get; private set; }
        private void OnEnable() { lookup = null; geometryChecked = false; }
        private void OnValidate() { lookup = null; geometryChecked = false; }

        public int Count => routes.Length;
        public float ClearanceFor(AudienceWordEmitter emitter) { EnsureLookup(); return lookup.TryGetValue(emitter, out var route) ? route.LaunchClearance : 0f; }
        public string RowFor(AudienceWordEmitter emitter) { EnsureLookup(); return lookup.TryGetValue(emitter, out var route) ? route.Row : null; }
        private void EnsureLookup()
        {
            if (lookup != null) return;
            lookup = new Dictionary<AudienceWordEmitter, Route>();
            foreach (var route in routes) if (route.Emitter != null) lookup[route.Emitter] = route;
        }
        public int ClearCount { get { int count = 0; foreach (var route in routes) if (route.Clear) count++; return count; } }
        private static float Evasion(RhythmGameConfig config) => config.Evasion != null && config.Evasion.Enabled ? config.Evasion.MaximumAngleDegrees : 0f;

        public void Configure(Vector3 player, RhythmGameConfig config, Route[] results, MeshRenderer[] scenery)
        {
            var unique = new HashSet<AudienceWordEmitter>();
            if (results == null || results.Length == 0) throw new ArgumentException("Route results are required.");
            foreach (var route in results)
                if (route == null || route.Emitter == null || !unique.Add(route.Emitter))
                    throw new ArgumentException("Routes must have unique emitters.");
            playerPosition = player;
            arrivalDistance = config.ArrivalDistance;
            angularWidth = config.Placement.TargetAngularWidthDegrees;
            placementEnabled = config.Placement.Enabled;
            flightRevision = AudienceWordFlight.Revision;
            evasionAngle = Evasion(config);
            wordPrefabs = (GameObject[])config.WordPrefabs.Clone();
            routes = (Route[])results.Clone();
            geometry = new GeometryStamp[scenery.Length];
            for (int i = 0; i < scenery.Length; i++)
            {
                var renderer = scenery[i];
                var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                geometry[i] = new GeometryStamp { Renderer = renderer, Matrix = renderer.localToWorldMatrix,
                    Mesh = mesh, MeshBounds = mesh == null ? default : mesh.bounds,
                    Visible = renderer.enabled && renderer.gameObject.activeInHierarchy };
            }
            lookup = null;
            geometryChecked = false;
        }

        public bool IsClear(AudienceWordEmitter emitter, Vector3 player, RhythmGameConfig config)
        {
            LastRejection = "Route, origin or configuration no longer matches the validated layout.";
            if ((player - playerPosition).sqrMagnitude > 0.000001f)
                LastRejection = "Player moved: " + player.ToString("F5") + "; validated=" + playerPosition.ToString("F5");
            if (!isActiveAndEnabled || emitter == null || emitter.WordOrigin == null ||
                flightRevision != AudienceWordFlight.Revision ||
                (player - playerPosition).sqrMagnitude > 0.000001f ||
                Mathf.Abs(arrivalDistance - config.ArrivalDistance) > 0.0001f ||
                Mathf.Abs(angularWidth - config.Placement.TargetAngularWidthDegrees) > 0.0001f ||
                placementEnabled != config.Placement.Enabled ||
                Mathf.Abs(evasionAngle - Evasion(config)) > 0.0001f ||
                config.WordPrefabs.Length != wordPrefabs.Length) return false;
            for (int i = 0; i < wordPrefabs.Length; i++) if (wordPrefabs[i] != config.WordPrefabs[i]) return false;
            // Scenery is static during play. Validate once per session, or on each editor query.
            if (!geometryChecked || !Application.isPlaying)
            {
                geometryValid = true;
                geometryIssue = null;
                foreach (var stamp in geometry) if (!stamp.Matches())
                { geometryValid = false; geometryIssue = "Scenery changed: " + (stamp.Renderer == null ? "missing renderer" : stamp.Renderer.name); break; }
                geometryChecked = true;
            }
            if (!geometryValid) { LastRejection = geometryIssue; return false; }
            if (lookup == null)
            {
                lookup = new Dictionary<AudienceWordEmitter, Route>();
                foreach (var route in routes) if (route.Emitter != null) lookup[route.Emitter] = route;
            }
            bool clear = lookup.TryGetValue(emitter, out var result) && result.Visible && result.Clear &&
                (emitter.WordOrigin.position - result.Origin).sqrMagnitude <= 0.000001f;
            LastRejection = clear ? null : "Emitter route is blocked or its origin moved.";
            return clear;
        }

        private void OnDrawGizmosSelected()
        {
            foreach (var route in routes)
            {
                Gizmos.color = route.Clear ? Color.green : new Color(1f, 0.3f, 0.2f, 0.35f);
                Gizmos.DrawLine(route.Origin, playerPosition);
            }
        }
    }
}
