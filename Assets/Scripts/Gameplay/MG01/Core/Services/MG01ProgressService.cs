using System;
using System.IO;
using UnityEngine;

namespace Mentality.Gameplay.MG01
{
    // JSON en persistentDataPath. Solo guarda las banderas: el golpe y el shader B/N->color quedan fuera (CUS-019).
    public sealed class MG01ProgressService : IMG01ProgressService
    {
        private readonly string _path;

        public MG01ProgressService(MG01ConfigSO config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _path = Path.Combine(Application.persistentDataPath, config.ProgressFileName);
        }

        public MG01ProgressData Load()
        {
            if (!File.Exists(_path)) return new MG01ProgressData();

            try
            {
                return JsonUtility.FromJson<MG01ProgressData>(File.ReadAllText(_path)) ?? new MG01ProgressData();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MG01ProgressService] No se pudo leer {_path}: {e.Message}");
                return new MG01ProgressData();
            }
        }

        public void SaveMG01Reward()
        {
            MG01ProgressData data = Load();
            data.mg01Completed = true;
            data.redHandsUnlocked = true;
            data.punchUnlocked = true;

            try
            {
                File.WriteAllText(_path, JsonUtility.ToJson(data, true));
            }
            catch (Exception e)
            {
                Debug.LogError($"[MG01ProgressService] No se pudo guardar {_path}: {e.Message}");
            }
        }
    }
}
