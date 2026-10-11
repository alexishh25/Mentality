using Mentality.Gameplay.MG01;
using Mentality.UI.ViewModels;
using UnityEngine.UIElements;
using VContainer;

namespace Mentality.UI.Views
{
    public class RewardView : BaseView
    {
        private Button _menuButton;
        private Button _replayButton;
        private RewardViewModel _viewModel;

        [Inject]
        public void Construct(RewardViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        protected override void OnInitialize(VisualElement root)
        {
            _menuButton = root.Q<Button>("menuBtn");
            _replayButton = root.Q<Button>("replayBtn");
            if (_viewModel == null) return;

            MG01StatsBinder.SetText(root, "titleLabel", _viewModel.Title);
            MG01StatsBinder.SetText(root, "rewardLabel", _viewModel.RewardLine);
            MG01StatsBinder.SetText(root, "closingLabel", _viewModel.ClosingMessage);
            MG01StatsBinder.SetText(root, "menuBtn", MG01Texts.ButtonBackToMenu);
            MG01StatsBinder.SetText(root, "replayBtn", MG01Texts.ButtonReplay);
            MG01StatsBinder.Fill(root, _viewModel);
        }

        protected override void OnBind()
        {
            if (_viewModel == null) return;
            if (_menuButton != null) _menuButton.clicked += _viewModel.BackToMenuCommand.Execute;
            if (_replayButton != null) _replayButton.clicked += _viewModel.ReplayCommand.Execute;
        }

        protected override void OnUnbind()
        {
            if (_viewModel == null) return;
            if (_menuButton != null) _menuButton.clicked -= _viewModel.BackToMenuCommand.Execute;
            if (_replayButton != null) _replayButton.clicked -= _viewModel.ReplayCommand.Execute;
        }
    }
}
