using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Mentality.UI.Services;
using VContainer;

namespace Mentality.UI.Views
{
	public abstract class BaseView : MonoBehaviour
	{
		[Header("Cancel button name")]
		[SerializeField] private string cancelBtnName = "cancelButton";

		private List<Button> _registeredbuttons = new List<Button>();

		private Button _cancelButton;
        private InputAction _cancelAction;

		//private InputService _inputService;
        protected ButtonService _buttonService;
        public UIService _uiService;

        [Inject]
        public void Construct(ButtonService buttonService, UIService uiService)
        {
            _buttonService = buttonService;
            _uiService = uiService;
        }

        protected virtual void Awake()
        {
            
        }

		public virtual void Initialize(VisualElement root)
		{
            if (root == null) return;

            Cleanup();

            OnInitialize(root);

            _registeredbuttons = root.Query<Button>().ToList();

            if (_buttonService != null && _registeredbuttons.Count > 0)
            {
                _buttonService.AlternateRegisterHoverSFX(true, _registeredbuttons);
            }

            _cancelButton = root.Q<Button>(cancelBtnName);

            // Vincular acción del Input System
            BindCancelAction();
            // Realizar Binding con el ViewModel
            OnBind();
        }

        private void BindCancelAction()
        {
            // if (_inputService?.inputActions == null) return;
            //  var map = _inputService.inputActions.FindActionMap("UI");
            //if (map != null)
            //{
            //    _cancelAction = map.FindAction("Cancel");
            //    if (_cancelAction != null)
            //        _cancelAction.performed += OnCancelActionPerformed;
            //}
        }

        public virtual void Dispose() => Cleanup();
        public void Cleanup()
        {
            if (_cancelAction != null)
            {
                _cancelAction.performed -= OnCancelActionPerformed;
                _cancelAction = null;
            }

            if (_registeredbuttons.Count > 0 && _buttonService != null)
            {
                _buttonService.AlternateRegisterHoverSFX(false, _registeredbuttons);
            }

            _registeredbuttons.Clear();

            OnUnbind();
        }

        private void OnCancelActionPerformed(InputAction.CallbackContext ctx)
        {
            if (_cancelButton == null) return;

            if (_cancelButton.panel?.focusController?.focusedElement == _cancelButton)
            {
                using (var e = new NavigationSubmitEvent() { target = _cancelButton })
                    _cancelButton.SendEvent(e);
            }
            else
                _cancelButton.Focus();
        }

        protected abstract void OnInitialize(VisualElement root);
        protected abstract void OnBind();
        protected abstract void OnUnbind();
    }

}