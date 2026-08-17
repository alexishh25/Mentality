using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using VContainer.Unity;

public class ApiClient : IApiClient, IInitializable
{
    private readonly string _baseUrl = "http://127.0.0.1:8000";

    public void Initialize()
    {
        Debug.Log("[ApiClient] Inicializado correctamente.");
    }

    public async UniTask<T> Get<T>(string endpoint)
    {
        using (UnityWebRequest request = UnityWebRequest.Get($"{_baseUrl}{endpoint}"))
        {
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ApiClient] GET Error ({endpoint}): {request.error} | {request.downloadHandler.text}");
                return default;
            }

            return JsonConvert.DeserializeObject<T>(request.downloadHandler.text);
        }
    }

    public async UniTask<TResponse> Post<TRequest, TResponse>(string endpoint, TRequest data)
    {
        string json = JsonConvert.SerializeObject(data);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest($"{_baseUrl}{endpoint}", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ApiClient] POST Error ({endpoint}): {request.error} | {request.downloadHandler.text}");
                return default;
            }

            return JsonConvert.DeserializeObject<TResponse>(request.downloadHandler.text);
        }
    }
}