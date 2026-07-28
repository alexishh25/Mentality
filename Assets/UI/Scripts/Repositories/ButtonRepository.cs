using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityUI.Repositories
{
	public class ButtonRepository
	{
        [Header("Efectos de sonido")]

        [SerializeField] public AudioClip sfx_buttonhover;
        [SerializeField] public AudioClip sfx_clickbutton;
        [SerializeField] public AudioClip sfx_cancelbutton;

        // private ISoundService soundService;
        // private IAnimationUIService animationService;

        private void Start()
        {
            // soundService = ServiceLocator.Get<ISoundService>();
            //animationService = ServiceLocator.Get<IAnimationUIService>();
        }

        public void PlayHoverSFX()
        {
            //if (soundService != null)
             //    soundService.PlaySFX(sfx_buttonhover);
            //else
            //    Debug.LogWarning("SoundManager not found. Cannot play hover sound.");
        }

        public void PlayClickSFX()
        {
            // if (soundService != null)
            //    soundService.PlaySFX(sfx_clickbutton);
            //else
                Debug.LogWarning("SoundManager not found. Cannot play click sound.");
        }

        public void PlayCancelSFX()
        {
            // if (soundService != null)
            //    soundService.PlaySFX(sfx_cancelbutton);
            // else
            //    Debug.LogWarning("SoundManager not found. Cannot play cancel sound.");
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
            foreach (var button in buttonhoverActions)
            {
                if (button == null) continue;

                if (register)
                {
                    // button.RegisterCallback<PointerEnterEvent>(OnHoverPointer);
                    // button.RegisterCallback<FocusInEvent>(OnHoverFocus);
                }
                else
                {
                    // button.UnregisterCallback<PointerEnterEvent>(OnHoverPointer);
                    // button.UnregisterCallback<FocusInEvent>(OnHoverFocus);
                }
            }
        }

        public void OnHoverPointer(PointerEnterEvent evt)
        {
            // if (evt.target is Button button)
            // animationService.Shake(button);
            PlayHoverSFX();
        }
        public void OnHoverFocus(FocusInEvent evt)
        {
            // if (evt.target is Button button)
            //   animationService.Shake(button);
            PlayHoverSFX();
        }
    }

}