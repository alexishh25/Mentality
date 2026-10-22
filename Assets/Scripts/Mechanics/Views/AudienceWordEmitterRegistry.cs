using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Rhythm.Views
{
    [DisallowMultipleComponent]
    public sealed class AudienceWordEmitterRegistry : MonoBehaviour
    {
        [SerializeField, Tooltip("Emisores del publico registrados en esta escena.")]
        private AudienceWordEmitter[] emitters = Array.Empty<AudienceWordEmitter>();

        public int Count => emitters.Length;
        public IReadOnlyList<AudienceWordEmitter> Emitters => Array.AsReadOnly(emitters);

        public void Configure(AudienceWordEmitter[] members)
        {
            if (members == null || members.Length == 0)
                throw new ArgumentException("At least one audience emitter is required.", nameof(members));
            var unique = new HashSet<AudienceWordEmitter>();
            var origins = new HashSet<Transform>();
            foreach (var member in members)
            {
                if (member == null || member.WordOrigin == null || !member.WordOrigin.IsChildOf(member.transform)
                    || member.WordOrigin == member.transform || member.gameObject.scene != gameObject.scene
                    || !unique.Add(member) || !origins.Add(member.WordOrigin))
                    throw new ArgumentException("Audience emitters must be valid, unique and belong to this scene.", nameof(members));
            }
            emitters = (AudienceWordEmitter[])members.Clone();
        }

        // Reuse the caller's list; no scene search or allocation is needed during selection.
        public void GetAvailableEmitters(List<AudienceWordEmitter> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            results.Clear();
            if (!isActiveAndEnabled) return;
            foreach (var emitter in emitters)
                if (emitter != null && emitter.CanEmit) results.Add(emitter);
        }
    }
}
