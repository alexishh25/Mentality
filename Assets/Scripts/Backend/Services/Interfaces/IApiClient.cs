using System.Threading.Tasks;

public interface IApiClient
{
    Task<T> Get<T>(string endpoint);

    Task<TResponse> Post<TRequest, TResponse>(
        string endpoint,
        TRequest data
    );
}