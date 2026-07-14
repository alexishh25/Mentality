using System.IO;
using UnityEngine;
using Mentality.Application.Interfaces;

namespace Mentality.Infrastructure.Persistence
{
    /// <summary>
    /// Dependency Inversion Principle (DIP).
    /// Implementación de bajo nivel del guardado de datos usando JSON en el disco local.
    /// </summary>
    public class JsonSaveService : ISaveService
    {
        private readonly string _basePath;

        public JsonSaveService()
        {
            _basePath = UnityEngine.Application.persistentDataPath;
        }

        public void Save<T>(string key, T data)
        {
            string path = GetPath(key);
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
            Debug.Log($"[JsonSaveService] Datos guardados en: {path}");
        }

        public T Load<T>(string key, T defaultValue = default)
        {
            string path = GetPath(key);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[JsonSaveService] No se encontró el archivo de guardado en: {path}. Usando valor por defecto.");
                return defaultValue;
            }

            try
            {
                string json = File.ReadAllText(path);
                T data = JsonUtility.FromJson<T>(json);
                return data;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[JsonSaveService] Error al cargar los datos en {path}: {ex.Message}");
                return defaultValue;
            }
        }

        private string GetPath(string key)
        {
            return Path.Combine(_basePath, $"{key}.json");
        }
    }
}
