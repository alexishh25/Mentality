namespace Mentality.Domain.Interfaces
{
    /// <summary>
    /// Interface Segregation Principle (ISP).
    /// Segrega la funcionalidad de curación de la de daño. No todo lo que es dañable tiene que ser curable.
    /// </summary>
    public interface IHealable
    {
        void Heal(int amount);
    }
}
