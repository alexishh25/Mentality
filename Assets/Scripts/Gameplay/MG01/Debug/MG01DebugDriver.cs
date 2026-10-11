#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace Mentality.Gameplay.MG01.DebugTools
{
    // Solo Editor / Development Build. Teclas elegidas para no chocar con R, P y Escape del HUD ritmico de prueba.
    //   H = impacto (-10)   J = palabra destruida   G = clic en fantasma
    //   L = terminar cancion   K = impacto y fin de cancion en el mismo frame
    public sealed class MG01DebugDriver : ITickable
    {
        private readonly ISanityService _sanity;
        private readonly IMG01ResultService _result;
        private readonly IPauseService _pause;
        private readonly DebugSongProgressSource _debugSong;

        public MG01DebugDriver(ISanityService sanity, IMG01ResultService result, IPauseService pause, ISongProgressSource song)
        {
            _sanity = sanity ?? throw new ArgumentNullException(nameof(sanity));
            _result = result ?? throw new ArgumentNullException(nameof(result));
            _pause = pause ?? throw new ArgumentNullException(nameof(pause));
            _debugSong = song as DebugSongProgressSource;
        }

        public void Tick()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || _pause.IsGameplayStopped) return;

            if (keyboard.hKey.wasPressedThisFrame)
            {
                _sanity.ApplyHit();
                Debug.Log($"[MG01 Debug] Impacto. Cordura = {_sanity.Current}/{_sanity.Max}");
            }

            if (keyboard.jKey.wasPressedThisFrame)
            {
                _result.ReportWordDestroyed();
                Debug.Log($"[MG01 Debug] Palabra destruida ({_result.Stats.wordsDestroyed})");
            }

            if (keyboard.gKey.wasPressedThisFrame)
            {
                _result.ReportGhostClicked();
                Debug.Log($"[MG01 Debug] Clic en fantasma ({_result.Stats.ghostClicks})");
            }

            if (keyboard.lKey.wasPressedThisFrame && _debugSong != null)
            {
                _debugSong.JumpToEnd();
                Debug.Log("[MG01 Debug] Cancion terminada");
            }

            if (keyboard.kKey.wasPressedThisFrame && _debugSong != null)
            {
                // Primero el fin de la cancion y despues el impacto: el resultado debe resolver el dano primero.
                _debugSong.JumpToEnd();
                _sanity.ApplyHit();
                Debug.Log($"[MG01 Debug] Fin de cancion + impacto en el mismo frame. Cordura = {_sanity.Current}");
            }
        }
    }
}
#endif
