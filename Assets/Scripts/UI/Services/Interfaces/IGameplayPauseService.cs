namespace Mentality.UI.Services
{
    public interface IGameplayPauseService
    {
        bool IsPaused { get; }
        void Pause();
        void Resume();
        void ExitToMainMenu();
    }
}
