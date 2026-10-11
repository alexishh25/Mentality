using Mentality.Gameplay.MG01;
using Mentality.UI.ViewModels;
using UnityEngine.UIElements;
using VContainer;

namespace Mentality.UI.Views
{
    public class GameOverView : BaseView
    {
        private Button _retryButton;
        private Button _menuButton;
        private GameOverViewModel _viewModel;

        [Inject]
        public void Construct(GameOverViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        protected override void OnInitialize(VisualElement root)
        {
            _retryButton = root.Q<Button>("retryBtn");
            _menuButton = root.Q<Button>("menuBtn");
            if (_viewModel == null) return;

            MG01StatsBinder.SetText(root, "titleLabel", _viewModel.Title);
            MG01StatsBinder.SetText(root, "subtitleLabel", _viewModel.Subtitle);
            MG01StatsBinder.SetText(root, "retryBtn", MG01Texts.ButtonRetry);
            MG01StatsBinder.SetText(root, "menuBtn", MG01Texts.ButtonBackToMenu);
            MG01StatsBinder.Fill(root, _viewModel);
        }

        protected override void OnBind()
        {
            if (_viewModel == null) return;
            if (_retryButton != null) _retryButton.clicked += _viewModel.RetryCommand.Execute;
            if (_menuButton != null) _menuButton.clicked += _viewModel.BackToMenuCommand.Execute;
        }

        protected override void OnUnbind()
        {
            if (_viewModel == null) return;
            if (_retryButton != null) _retryButton.clicked -= _viewModel.RetryCommand.Execute;
            if (_menuButton != null) _menuButton.clicked -= _viewModel.BackToMenuCommand.Execute;
        }
    }
}
