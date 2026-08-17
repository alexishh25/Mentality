using Gameplay;
using VContainer;
using VContainer.Unity;

public class MechanicsLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {

        builder.RegisterComponentInHierarchy<TouchableObject>();
    }
}
