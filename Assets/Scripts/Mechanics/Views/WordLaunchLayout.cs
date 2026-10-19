using System;
using UnityEngine;

namespace Gameplay.Rhythm.Views
{
    // Scene-specific launch directions; musical event times and travel radius remain unchanged.
    public sealed class WordLaunchLayout : MonoBehaviour
    {
        [SerializeField] private Vector3[] directions = Array.Empty<Vector3>();
        public Vector3[] Directions => (Vector3[])directions.Clone();

        public void Configure(Vector3[] clearDirections)
        {
            if (clearDirections == null || clearDirections.Length == 0
                || Array.Exists(clearDirections, d => !float.IsFinite(d.x) || !float.IsFinite(d.y)
                    || !float.IsFinite(d.z) || d.sqrMagnitude < 0.01f))
                throw new ArgumentException("Word lanes must contain finite, nonzero directions");
            directions = (Vector3[])clearDirections.Clone();
        }

        public Vector3 PositionFor(float angle, Vector3 player, float distance)
        {
            if (directions.Length == 0) throw new InvalidOperationException("No clear word launch lanes configured");
            Vector3 selected = directions[0];
            float best = float.PositiveInfinity;
            foreach (var direction in directions)
            {
                float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                float difference = Mathf.Abs(Mathf.DeltaAngle(angle, yaw));
                if (difference < best) { best = difference; selected = direction; }
            }
            Vector3 world = Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * selected.normalized;
            return player + world * distance;
        }
    }
}
