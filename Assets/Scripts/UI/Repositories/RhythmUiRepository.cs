using Mentality.UI.Services;

using ScreenType = Mentality.UI.ScreenType;

namespace Mentality.UI.Repositories
{
    public class RhythmUiRepository
    {
        private readonly UIService _uiService;

        public RhythmUiRepository(UIService uiService)
        {
            _uiService = uiService;
        }

        public void Show(ScreenType type) => _uiService.ChangeScreen(type);

        public void Hide() => _uiService.HideScreen();
    }
}
