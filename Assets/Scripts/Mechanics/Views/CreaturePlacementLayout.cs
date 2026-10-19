using System;
using UnityEngine;

namespace Gameplay.Rhythm.Views
{
    public sealed class CreaturePlacementLayout : MonoBehaviour
    {
        [SerializeField] private Transform[] points = Array.Empty<Transform>();
        public Transform[] Points => (Transform[])points.Clone();
        public void Configure(Transform[] positions)
        {
            if (positions == null || positions.Length != 5 || Array.Exists(positions, p => p == null)
                ) throw new ArgumentException("Five ground positions required");
            points = (Transform[])positions.Clone();
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
