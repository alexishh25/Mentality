using Mentality.UI.Commands;
using Mentality.UI.Services;

namespace Mentality.UI.ViewModels
{
    public class RhythmResultViewModel : UnityBaseViewModel
    {
        public UnityHandler RestartCommand { get; }

        public RhythmResultViewModel(RhythmUiService uiService)
        {
            RestartCommand = new UnityHandler(uiService.RequestRestart);
        }
    }
}
