using Gameplay;
using Gameplay.Doors.Models;
using Gameplay.Doors.Services;
using Gameplay.Doors.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class MechanicsLifetimeScope : LifetimeScope
{
    [Header("Lockers Settings")]
    [SerializeField] private LockerInitializer lockerInitializer;

    [Header("Player Settings")]
    [SerializeField] private Transform playerTransform;

    [Header("Doors Settings")]
    [SerializeField] private DoorConfig doorConfig;

    protected override void Configure(IContainerBuilder builder)
    {
        // ---------------------------------------------------------
        // 1. SERVICES & GLOBAL REFERENCES
        // ---------------------------------------------------------
        builder.RegisterComponentInHierarchy<TouchableObject>();
        builder.RegisterInstance(playerTransform);

        // ---------------------------------------------------------
        // 2. LOCKERS MECHANIC
        // ---------------------------------------------------------
        builder.Register<LockerFactory>(Lifetime.Scoped);
        builder.RegisterComponent(lockerInitializer);

        // ---------------------------------------------------------
        // 3. DOORS MECHANIC
        // ---------------------------------------------------------
        // Registra el modelo de configuración de las puertas
        builder.RegisterInstance(doorConfig);

        builder.Register<DoorManagerService>(Lifetime.Scoped);

        // Registra todas las instancias de DoorView presentes en la jerarquía
        builder.RegisterEntryPoint<DoorInitializer>(Lifetime.Scoped);
    }
}