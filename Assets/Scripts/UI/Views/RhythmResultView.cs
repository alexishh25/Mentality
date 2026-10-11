using Mentality.UI.ViewModels;
using UnityEngine.UIElements;
using VContainer;

namespace Mentality.UI.Views
{
    // Game Over y Victoria comparten el boton de reinicio; el texto vive en cada UXML.
    public class RhythmResultView : BaseView
    {
        private Button _restartButton;
        private RhythmResultViewModel _viewModel;

        [Inject]
        public void Construct(RhythmResultViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        protected override void OnInitialize(VisualElement root)
        {
            _restartButton = root.Q<Button>("restartBtn");
        }

        protected override void OnBind()
        {
            if (_viewModel == null || _restartButton == null) return;
            _restartButton.clicked += _viewModel.RestartCommand.Execute;
        }

        protected override void OnUnbind()
        {
            if (_viewModel == null || _restartButton == null) return;
            _restartButton.clicked -= _viewModel.RestartCommand.Execute;
        }
    }
}
