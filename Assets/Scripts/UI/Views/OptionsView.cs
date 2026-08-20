using UnityEngine;
using UnityEngine.UIElements;
using Mentality.UI.ViewModels;
using VContainer;

namespace Mentality.UI.Views
{
    public class OptionsView : BaseView
    {
        private Button returnBtn;
        
        private MenuSettingsViewModel _settingsViewModel;

        [Inject]
        public void Construct(MenuSettingsViewModel settingsViewModel)
        {
            _settingsViewModel = settingsViewModel;
        }
        protected override void OnInitialize(VisualElement root)
        {
            returnBtn = root.Q<Button>("returnBtn");
        }

        protected override void OnBind()
        {
            if (_settingsViewModel == null) return;
            if (returnBtn != null) returnBtn.clicked += _settingsViewModel.ReturnMenuCommand.Execute;
        }

        protected override void OnUnbind()
        {
            if (_settingsViewModel == null) return;
            if (returnBtn != null) returnBtn.clicked -= _settingsViewModel.ReturnMenuCommand.Execute;
        }
    }
}
