using System;
using UnityEngine;

namespace Gameplay.Rhythm.Views
{
    [DisallowMultipleComponent]
    public sealed class AudienceWordEmitter : MonoBehaviour
    {
        [SerializeField, Tooltip("Punto de salida delante de la boca de este personaje.")]
        private Transform wordOrigin;
        [SerializeField, Tooltip("Permite a este personaje participar como emisor de palabras.")]
        private bool emissionEnabled = true;

        public Transform WordOrigin => wordOrigin;
        public bool EmissionEnabled { get => emissionEnabled; set => emissionEnabled = value; }
        public bool CanEmit => emissionEnabled && isActiveAndEnabled && wordOrigin != null
            && wordOrigin.gameObject.activeInHierarchy;

        public void Configure(Transform origin)
        {
            if (origin == null || origin == transform || !origin.IsChildOf(transform))
                throw new ArgumentException("WordOrigin must belong to this audience member.", nameof(origin));
            wordOrigin = origin;
        }
    }
}
