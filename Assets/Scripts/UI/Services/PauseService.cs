using Mentality.UI.Repositories;

namespace Mentality.UI.Services
{
    public class PauseService
    {
        private readonly PauseRepository _pauseRepository;
        private readonly UIService _uiService;
        private readonly IGameplayPauseService _gameplayPauseService;

        public PauseService(
            PauseRepository pauseRepository,
            UIService uiService,
            IGameplayPauseService gameplayPauseService)
        {
            _pauseRepository = pauseRepository;
            _uiService = uiService;
            _gameplayPauseService = gameplayPauseService;
        }

        public void Resume() => _pauseRepository.Resume(_gameplayPauseService);
        public void OpenSettings() => _pauseRepository.OpenSettings(_uiService);
        public void ExitToMenu() => _pauseRepository.ExitToMenu(_gameplayPauseService);
    }
}
