using UnityEngine;
using Mentality.UI.Repositories;
using Mentality.UI.Services;
using Mentality.UI.ViewModels;
using VContainer;
using VContainer.Unity;

namespace Mentality.UI
{
    public class UILifetimeScope : LifetimeScope
    {
        [SerializeField] private UIDoc uiDoc;

        [Tooltip("Activa el controlador de pausa (requiere IPlayerInputService en un LifetimeScope padre).")]
        [SerializeField] private bool enableGameplayPause;

        protected override void Configure(IContainerBuilder builder)
        {
            if (uiDoc != null)
                builder.RegisterComponent(uiDoc);

            builder.Register<UIRepository>(Lifetime.Singleton);
            builder.Register<UIService>(Lifetime.Singleton);

            // Repositories & Services
            builder.Register<ButtonRepository>(Lifetime.Singleton);
            builder.Register<ButtonService>(Lifetime.Singleton);
            builder.Register<MenuRepository>(Lifetime.Singleton);
            builder.Register<MenuService>(Lifetime.Singleton);
            builder.Register<MenuSettingsRepository>(Lifetime.Singleton);
            builder.Register<MenuSettingsService>(Lifetime.Singleton);

            // ViewModels
            builder.Register<UnityBaseViewModel>(Lifetime.Transient);
            builder.Register<MenuViewModel>(Lifetime.Transient);
            builder.Register<MenuSettingsViewModel>(Lifetime.Singleton);

            // Pause solo en SampleScene (UILifetimeScope hijo de PlayerLifetimeScope)
            if (enableGameplayPause)
            {
                builder.Register<PauseRepository>(Lifetime.Singleton);
                builder.Register<PauseService>(Lifetime.Singleton);
                builder.Register<PauseViewModel>(Lifetime.Transient);
                builder.RegisterEntryPoint<GameplayPauseService>()
                    .As<IGameplayPauseService>();
            }
        }
    }
}
