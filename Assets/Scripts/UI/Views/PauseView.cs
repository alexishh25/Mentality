using Mentality.UI.ViewModels;
using UnityEngine.UIElements;
using VContainer;

namespace Mentality.UI.Views
{
    public class PauseView : BaseView
    {
        private Button _resumeButton;
        private Button _settingsButton;
        private Button _exitButton;

        private PauseViewModel _pauseViewModel;

        [Inject]
        public void Construct(PauseViewModel pauseViewModel)
        {
            _pauseViewModel = pauseViewModel;
        }

        protected override void OnInitialize(VisualElement root)
        {
            _resumeButton = root.Q<Button>("resumeBtn");
            _settingsButton = root.Q<Button>("optionsBtn");
            _exitButton = root.Q<Button>("exitBtn");
        }

        protected override void OnBind()
        {
            if (_pauseViewModel == null) return;

            if (_resumeButton != null) _resumeButton.clicked += _pauseViewModel.ResumeCommand.Execute;
            if (_settingsButton != null) _settingsButton.clicked += _pauseViewModel.OpenSettingsCommand.Execute;
            if (_exitButton != null) _exitButton.clicked += _pauseViewModel.ExitToMenuCommand.Execute;
        }

        protected override void OnUnbind()
        {
            if (_pauseViewModel == null) return;

            if (_resumeButton != null) _resumeButton.clicked -= _pauseViewModel.ResumeCommand.Execute;
            if (_settingsButton != null) _settingsButton.clicked -= _pauseViewModel.OpenSettingsCommand.Execute;
            if (_exitButton != null) _exitButton.clicked -= _pauseViewModel.ExitToMenuCommand.Execute;
        }
    }
}
