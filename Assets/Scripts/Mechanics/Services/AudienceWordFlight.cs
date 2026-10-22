using UnityEngine;

namespace Gameplay.Rhythm.Services
{
    public static class AudienceWordFlight
    {
        public const int Revision = 2;
        public const float InitialScale = 0.08f;
        public const float GrowthDistance = 1.5f;
        public const float EvasionStartDistance = 2.5f;
        public const float EvasionRampDistance = 1f;
        public static float ReadableTravel(float clearance) => clearance + GrowthDistance * 0.6f;
        public static float ClearanceForDistance(float distance) => Mathf.Max(0f, distance - 6f);

        // The hidden/small part clears the audience first. The readable approach then
        // uses a steady, lower speed, with a continuous velocity at the join.
        public static float TravelProgressAt(float musicalProgress, float travelDistance, float clearance, float readableFraction)
        {
            float progress = Mathf.Clamp01(musicalProgress);
            if (readableFraction <= 0f || travelDistance <= 0f) return progress;
            float splitTime = 1f - Mathf.Clamp(readableFraction, 0.5f, 0.85f);
            float splitDistance = Mathf.Clamp01(ReadableTravel(clearance) / travelDistance);
            if (splitDistance <= splitTime || splitDistance >= 1f) return progress;
            float finalSlope = (1f - splitDistance) / (1f - splitTime);
            if (progress >= splitTime) return splitDistance + (progress - splitTime) * finalSlope;
            float t = progress / splitTime, t2 = t * t, t3 = t2 * t;
            return splitDistance * (-t3 + t2 + t) + finalSlope * splitTime * (t3 - t2);
        }

        public static float ScaleAt(float travelled, float clearance = 0f, bool immediateGrowth = false)
        {
            if (!immediateGrowth || clearance <= 0f) return Mathf.Lerp(InitialScale, 1f,
                Mathf.SmoothStep(0f, 1f, (travelled - clearance) / GrowthDistance));
            const float clearingScale = 0.28f;
            if (travelled <= clearance) return Mathf.Lerp(InitialScale, clearingScale, Mathf.Clamp01(travelled / clearance));
            return Mathf.Lerp(clearingScale, 1f, Mathf.SmoothStep(0f, 1f, (travelled - clearance) / GrowthDistance));
        }
        public static float EvasionAt(float travelled, float clearance = 0f) => Mathf.SmoothStep(0f, 1f,
            (travelled - clearance - EvasionStartDistance) / EvasionRampDistance);
    }

    public interface IAudienceWordFlightView
    {
        void SetAudienceFlightTravel(float travelled);
    }
}
