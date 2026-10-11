using System;
using Gameplay.Rhythm.Services;

namespace Gameplay.Rhythm.Models
{
    // Tiempos relativos al clip. HitTime es la llegada al jugador, no una ventana de clic.
    // SpawnTime puede estar en la cuenta de entrada; el proyectil puede interceptarse durante el vuelo.
    public sealed class TargetTiming
    {
        public double SpawnTime { get; }
        public double HitTime { get; }
        public double LeadTime => HitTime - SpawnTime;
        public MusicalActivityProfile MotionProfile { get; }
        public float LateralArcDegrees { get; }

        public TargetTiming(double spawnTime, double hitTime, MusicalActivityProfile motionProfile = null,
            float lateralArcDegrees = 0f)
        {
            if (double.IsNaN(spawnTime) || double.IsInfinity(spawnTime))
                throw new ArgumentOutOfRangeException(nameof(spawnTime));
            if (double.IsNaN(hitTime) || double.IsInfinity(hitTime) || hitTime < 0 || hitTime < spawnTime)
                throw new ArgumentOutOfRangeException(nameof(hitTime));
            SpawnTime = spawnTime;
            HitTime = hitTime;
            MotionProfile = motionProfile;
            if (float.IsNaN(lateralArcDegrees) || float.IsInfinity(lateralArcDegrees) || Math.Abs(lateralArcDegrees) > 30f)
                throw new ArgumentOutOfRangeException(nameof(lateralArcDegrees));
            LateralArcDegrees = lateralArcDegrees;
        }

        public double ProgressAt(double songTime) => MotionProfile == null
            ? (songTime - SpawnTime) / LeadTime
            : (MotionProfile.MotionTimeAt(songTime) - MotionProfile.MotionTimeAt(SpawnTime))
                / (MotionProfile.MotionTimeAt(HitTime) - MotionProfile.MotionTimeAt(SpawnTime));
    }
}
