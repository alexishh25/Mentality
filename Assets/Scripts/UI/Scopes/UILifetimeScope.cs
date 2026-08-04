using UnityEngine;
using UnityEngine.UIElements;
using UnityUI.Views;
using UnityUI.Repositories;
using UnityUI.Services;
using UnityUI.ViewModels;
using VContainer;
using VContainer.Unity;

public class UILifetimeScope : LifetimeScope
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private UIDoc uiDoc;
    [SerializeField] private BaseView baseView;

    protected override void Configure(IContainerBuilder builder)
    {
        if (uiDoc != null)
            builder.RegisterComponent(uiDoc);

        if (uiDocument != null)
            builder.RegisterComponent(uiDocument);

        if (baseView != null)
            builder.RegisterComponent(baseView);

        builder.Register<UIRepository>(Lifetime.Singleton);
        builder.Register<UIService>(Lifetime.Singleton);

        builder.Register<ButtonRepository>(Lifetime.Singleton);
        builder.Register<ButtonService>(Lifetime.Singleton);

        builder.Register<MenuRepository>(Lifetime.Singleton);
        builder.Register<MenuService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();

        builder.Register<MenuViewModel>(Lifetime.Transient);
    }
}
