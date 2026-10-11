using System;

namespace Mentality.Gameplay.MG01
{
    // El gameplay escribe (ApplyHit) y el HUD lee.
    public interface ISanityService
    {
        float Current { get; }
        float Max { get; }
        float Normalized { get; }
        bool IsDepleted { get; }

        // (current, max)
        event Action<float, float> Changed;
        // Se dispara una sola vez por partida.
        event Action Depleted;

        // Resta DamagePerHit; no hace nada una vez agotada.
        void ApplyHit();
        void Reset();
    }
}
