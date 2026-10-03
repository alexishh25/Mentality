using UnityEngine;
using VContainer;

namespace Gameplay
{
    public class LockerFactory
    {
        private readonly IObjectResolver resolver;

        public LockerFactory(IObjectResolver resolver)
        {
            this.resolver = resolver;
        }

        public void CreatePresenter(ILockerView view, GameObject consumablePrefab)
        {
            // Instancia un Presenter independiente por cada casillero detectado
            var presenter = new LockerPresenter(view, resolver, consumablePrefab);

            // Registra el ciclo de vida del presenter en el contenedor de VContainer
            presenter.Start();
        }
    }
}