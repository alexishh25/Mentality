using Cysharp.Threading.Tasks;

public interface IApiClient
{
    UniTask<T> Get<T>(string endpoint);
    UniTask<TResponse> Post<TRequest, TResponse>(string endpoint, TRequest data);
}