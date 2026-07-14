namespace Mentality.Domain.Interfaces
{
    /// <summary>
    /// Interface Segregation Principle (ISP) & Liskov Substitution Principle (LSP).
    /// Permite que cualquier clase que pueda recibir daño implemente este contrato.
    /// </summary>
    public interface IDamageable
    {
        int CurrentHealth { get; }
        int MaxHealth { get; }
        void TakeDamage(int amount);
        bool IsDead { get; }
    }
}
