using System;
using Mentality.Domain.Models;
using Mentality.Presentation.Views;

namespace Mentality.Presentation.Presenters
{
    /// <summary>
    /// Single Responsibility Principle (SRP) / Dependency Inversion Principle (DIP).
    /// El presentador desacopla completamente el modelo Health de la vista HealthBarView.
    /// Registra los eventos del modelo y actualiza la vista en consecuencia.
    /// </summary>
    public class HealthPresenter : IDisposable
    {
        private readonly Health _health;
        private readonly HealthBarView _view;

        public HealthPresenter(Health health, HealthBarView view)
        {
            _health = health ?? throw new ArgumentNullException(nameof(health));
            _view = view ?? throw new ArgumentNullException(nameof(view));

            _view.Setup(_health.MaxHealth);
            _view.UpdateHealthUI(_health.CurrentHealth, _health.MaxHealth);

            _health.OnHealthChanged += HandleHealthChanged;
        }

        private void HandleHealthChanged(int currentHealth)
        {
            _view.UpdateHealthUI(currentHealth, _health.MaxHealth);
        }

        public void Dispose()
        {
            _health.OnHealthChanged -= HandleHealthChanged;
        }
    }
}
