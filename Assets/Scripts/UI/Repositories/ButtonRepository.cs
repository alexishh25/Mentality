using System;
using System.Collections.Generic;
using Composition.Interfaces;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mentality.UI.Repositories
{
	public class ButtonRepository
	{
        [Header("Efectos de sonido")]
        [SerializeField] public SFXTrackSO sfx_buttonhover;
        [SerializeField] public SFXTrackSO sfx_clickbutton;
        [SerializeField] public SFXTrackSO sfx_cancelbutton;

        private readonly IAudioService _audioService;

        public ButtonRepository(IAudioService audioService = null)
        {
            _audioService = audioService;
        }

        public void PlayHoverSFX()
        {
            if (_audioService != null && sfx_buttonhover != null)
                _audioService.PlaySFX(sfx_buttonhover);
        }

        public void PlayClickSFX()
        {
            if (_audioService != null && sfx_clickbutton != null)
                _audioService.PlaySFX(sfx_clickbutton);
        }

        public void PlayCancelSFX()
        {
            if (_audioService != null && sfx_cancelbutton != null)
                _audioService.PlaySFX(sfx_cancelbutton);
        }

        // Allows subscribing or unsubscribing multiple buttons to their respective click event handlers efficiently.
        public void ManageButtonActions(bool suscribe, params (Button button, Action handler)[] buttonActions)
        {
            foreach (var (button, handler) in buttonActions)
            {
                if (button == null || handler == null) continue;

                if (suscribe)
                    button.clicked += handler;
                else
                    button.clicked -= handler;
            }
        }

        public void AlternateRegisterHoverSFX(bool register, List<Button> buttonhoverActions)
        {
            if (buttonhoverActions == null) return;

            foreach (var button in buttonhoverActions)
            {
                if (button == null) continue;

                if (register)
                {
                    button.RegisterCallback<PointerEnterEvent>(OnHoverPointer);
                    button.RegisterCallback<FocusInEvent>(OnHoverFocus);
                }
                else
                {
                    button.UnregisterCallback<PointerEnterEvent>(OnHoverPointer);
                    button.UnregisterCallback<FocusInEvent>(OnHoverFocus);
                }
            }
        }

        public void OnHoverPointer(PointerEnterEvent evt)
        {
            PlayHoverSFX();
        }

        public void OnHoverFocus(FocusInEvent evt)
        {
            PlayHoverSFX();
        }
    }
}