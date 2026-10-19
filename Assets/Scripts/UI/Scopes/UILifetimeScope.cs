using UnityEngine;
using Mentality.Gameplay.MG01;
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
        [Tooltip("Opcional. Sin asset se usan los valores por defecto (escena MG01 = Nivel-1_Rhythm).")]
        [SerializeField] private MG01ConfigSO mg01Config;

        protected override void Configure(IContainerBuilder builder)
        {
            if (uiDoc != null)
                builder.RegisterComponent(uiDoc);

            builder.RegisterInstance(mg01Config != null ? mg01Config : MG01ConfigSO.CreateDefault());

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
        }
    }
}
