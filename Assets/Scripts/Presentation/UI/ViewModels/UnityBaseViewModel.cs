using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mentality.UI.ViewModels
{
    public class UnityBaseViewModel : INotifyPropertyChanged, INotifyBindablePropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<BindablePropertyChangedEventArgs> propertyChanged;

        /// <summary>
        /// Notifica un cambio en una propiedad y actualiza la interfaz de usuario.
        /// </summary>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName)) return;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 
        /// </summary>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

}