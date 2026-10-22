using System;
using UnityEngine;

namespace Gameplay.Rhythm.Views
{
    public sealed class CreaturePlacementLayout : MonoBehaviour
    {
        [SerializeField] private Transform[] points = Array.Empty<Transform>();
        [SerializeField] private float[] launchClearances = Array.Empty<float>();
        public Transform[] Points => (Transform[])points.Clone();
        public void Configure(Transform[] positions, float[] clearances = null)
        {
            if (positions == null || positions.Length != 5 || Array.Exists(positions, p => p == null)
                ) throw new ArgumentException("Five ground positions required");
            points = (Transform[])positions.Clone();
            if (clearances != null && (clearances.Length != 5 || Array.Exists(clearances, c => c < 0f || float.IsNaN(c) || float.IsInfinity(c))))
                throw new ArgumentException("Five valid launch clearances required");
            launchClearances = clearances == null ? Array.Empty<float>() : (float[])clearances.Clone();
        }
        public float? LaunchClearanceFor(float angle) => launchClearances.Length == 5
            ? launchClearances[Mathf.Clamp(Mathf.RoundToInt((angle + 60f) / 30f), 0, 4)] : (float?)null;
        public float MaximumMouthDistance(Vector3 player)
        {
            float maximum = 0f;
            foreach (var point in points) if (point != null)
                maximum = Mathf.Max(maximum, Vector3.Distance(player, point.position + Vector3.up * 1.392f));
            return maximum;
        }
        public Vector3 GroundFor(float angle)
        {
            int index = Mathf.Clamp(Mathf.RoundToInt((angle + 60f) / 30f), 0, 4);
            if (points.Length != 5 || points[index] == null)
                throw new InvalidOperationException("Creature ground layout is incomplete");
            return points[index].position;
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            foreach (var point in points) if (point != null)
            { Gizmos.DrawWireSphere(point.position, 0.3f); Gizmos.DrawLine(point.position, point.position + Vector3.up * 2.4f); }
        }
    }
}
