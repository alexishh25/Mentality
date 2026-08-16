using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using VContainer.Unity;


public class ApiClient : IApiClient, IInitializable, IDisposable
{

    private readonly HttpClient _client;

    private readonly string _baseUrl = "http://127.0.0.1:8000";


    public ApiClient()
    {
        _client = new HttpClient();
    }


    public void Initialize()
    {
        UnityEngine.Debug.Log( "API Client inicializado");
    }

    public async Task<T> Get<T>(string endpoint)
    {

        var response = await _client.GetAsync(_baseUrl + endpoint);

        string json = await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<T>(json);
    }




    public async Task<TResponse> Post<TRequest, TResponse>(string endpoint,TRequest data)
    {
        string json = JsonConvert.SerializeObject(data);

        var content =new StringContent( json,Encoding.UTF8,"application/json");

        var response = await _client.PostAsync( _baseUrl + endpoint,content);

        string result =await response.Content.ReadAsStringAsync();

        return JsonConvert.DeserializeObject<TResponse>(result);

    }


    public void Dispose()
    {
        _client.Dispose();
    }

}