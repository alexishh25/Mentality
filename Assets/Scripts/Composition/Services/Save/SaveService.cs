using System;
using System.IO;
using System.Text;
using Composition.Interfaces;
using UnityEngine;

namespace Composition.Services
{
    public class SaveService : ISaveService
    {
        private readonly SaveConfigSO _config;

        public SaveConfigSO Config => _config;

        public SaveService(SaveConfigSO config = null)
        {
            _config = config;
        }

        public string GetSaveFilePath(string customFileName = null)
        {
            string fileName = !string.IsNullOrEmpty(customFileName)
                ? customFileName
                : (_config != null && !string.IsNullOrEmpty(_config.DefaultSaveFileName) ? _config.DefaultSaveFileName : "savegame.json");

            string folderName = (_config != null && !string.IsNullOrEmpty(_config.SaveFolderName))
                ? _config.SaveFolderName
                : "Saves";

            string directoryPath = Path.Combine(UnityEngine.Application.persistentDataPath, folderName);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            return Path.Combine(directoryPath, fileName);
        }

        public bool Save<T>(T data, string customFileName = null)
        {
            if (data == null)
            {
                Debug.LogWarning("[SaveService] Intentando guardar datos nulos.");
                return false;
            }

            try
            {
                string filePath = GetSaveFilePath(customFileName);
                bool prettyPrint = _config != null && _config.PrettyPrint;
                string json = JsonUtility.ToJson(data, prettyPrint);

                if (_config != null && _config.UseEncryption && !string.IsNullOrEmpty(_config.EncryptionKey))
                {
                    json = EncryptDecrypt(json, _config.EncryptionKey);
                }

                File.WriteAllText(filePath, json, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Error al guardar datos: {ex.Message}");
                return false;
            }
        }

        public T Load<T>(string customFileName = null)
        {
            try
            {
                string filePath = GetSaveFilePath(customFileName);
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[SaveService] Archivo de guardado no encontrado en: {filePath}");
                    return default;
                }

                string json = File.ReadAllText(filePath, Encoding.UTF8);

                if (_config != null && _config.UseEncryption && !string.IsNullOrEmpty(_config.EncryptionKey))
                {
                    json = EncryptDecrypt(json, _config.EncryptionKey);
                }

                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Error al cargar datos: {ex.Message}");
                return default;
            }
        }

        public bool HasSave(string customFileName = null)
        {
            string filePath = GetSaveFilePath(customFileName);
            return File.Exists(filePath);
        }

        public bool DeleteSave(string customFileName = null)
        {
            try
            {
                string filePath = GetSaveFilePath(customFileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveService] Error al eliminar archivo de guardado: {ex.Message}");
                return false;
            }
        }

        private string EncryptDecrypt(string text, string key)
        {
            var result = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                result.Append((char)(text[i] ^ key[i % key.Length]));
            }
            return result.ToString();
        }
    }
}