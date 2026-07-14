using System;
using Mentality.Domain.Interfaces;

namespace Mentality.Domain.Models
{
    /// <summary>
    /// Single Responsibility Principle (SRP).
    /// Esta clase es responsable únicamente de gestionar el estado de la salud y sus reglas asociadas.
    /// No sabe nada sobre UI, rendering, física, de dónde proviene el daño, ni cómo se guarda en disco.
    /// </summary>
    public class Health : IDamageable, IHealable
    {
        public event Action<int> OnHealthChanged;
        public event Action OnDeath;

        public int CurrentHealth { get; private set; }
        public int MaxHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0;

        public Health(int maxHealth, int initialHealth)
        {
            if (maxHealth <= 0)
                throw new ArgumentException("Max health must be greater than 0");

            MaxHealth = maxHealth;
            CurrentHealth = Math.Clamp(initialHealth, 0, maxHealth);
        }

        public void TakeDamage(int amount)
        {
            if (amount < 0 || IsDead) return;

            CurrentHealth = Math.Max(0, CurrentHealth - amount);
            OnHealthChanged?.Invoke(CurrentHealth);

            if (IsDead)
            {
                OnDeath?.Invoke();
            }
        }

        public void Heal(int amount)
        {
            if (amount < 0 || IsDead) return;

            CurrentHealth = Math.Min(MaxHealth, CurrentHealth + amount);
            OnHealthChanged?.Invoke(CurrentHealth);
        }

        /// <summary>
        /// Permite restaurar el estado de salud cargado desde la capa de persistencia.
        /// </summary>
        public void RestoreState(int health)
        {
            CurrentHealth = Math.Clamp(health, 0, MaxHealth);
            OnHealthChanged?.Invoke(CurrentHealth);
        }
    }
}
