using Mentality.Gameplay.MG01;
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
        private PauseViewModel _viewModel;

        [Inject]
        public void Construct(PauseViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        protected override void OnInitialize(VisualElement root)
        {
            _resumeButton = root.Q<Button>("resumeBtn");
            _settingsButton = root.Q<Button>("settingsBtn");
            _exitButton = root.Q<Button>("exitBtn");

            MG01StatsBinder.SetText(root, "pauseTitle", MG01Texts.PauseTitle);
            MG01StatsBinder.SetText(root, "resumeBtn", MG01Texts.ButtonResume);
            MG01StatsBinder.SetText(root, "settingsBtn", MG01Texts.ButtonSettings);
            MG01StatsBinder.SetText(root, "exitBtn", MG01Texts.ButtonExit);

            // Options solo existe en UIScene; se oculta hasta que haya Ajustes dentro del gameplay.
            if (_settingsButton != null) _settingsButton.style.display = DisplayStyle.None;
        }

        protected override void OnBind()
        {
            if (_viewModel == null) return;
            if (_resumeButton != null) _resumeButton.clicked += _viewModel.ResumeCommand.Execute;
            if (_exitButton != null) _exitButton.clicked += _viewModel.ExitCommand.Execute;
        }

        protected override void OnUnbind()
        {
            if (_viewModel == null) return;
            if (_resumeButton != null) _resumeButton.clicked -= _viewModel.ResumeCommand.Execute;
            if (_exitButton != null) _exitButton.clicked -= _viewModel.ExitCommand.Execute;
        }
    }
}
