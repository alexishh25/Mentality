using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityUI.Repositories;

namespace UnityUI.Services
{
	public class ButtonService
	{
        private readonly ButtonRepository _buttonRepository;
        public void PlayHoverSFX() => _buttonRepository.PlayHoverSFX();
        public void PlayClickSFX() => _buttonRepository.PlayClickSFX();
        public void PlayCancelSFX() => _buttonRepository.PlayCancelSFX();
        public void ManageButtonActions(bool suscribe, params (Button button, Action handler)[] buttonActions)
        {
            _buttonRepository.ManageButtonActions(suscribe, buttonActions);
        }
        public void AlternateRegisterHoverSFX(bool register, List<Button> buttonhoverActions)
        {
            _buttonRepository.AlternateRegisterHoverSFX(register, buttonhoverActions);
        }
        public void OnHoverPointer(PointerEnterEvent evt) => _buttonRepository.OnHoverPointer(evt);
        public void OnHoverFocus(FocusInEvent evt) => _buttonRepository.OnHoverFocus(evt);
    }

}