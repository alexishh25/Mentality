namespace Gameplay.Ghosts.Services
{
    public interface IGhostConsequences
    {
        void OnGhostClicked();
        void ResetForSession();
    }

    public interface IScreamerEffect
    {
        void TriggerScreamer();
        bool TryTriggerScreamer();
    }
}
