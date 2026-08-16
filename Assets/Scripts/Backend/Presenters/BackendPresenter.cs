using Backend.Models;
using System.Threading.Tasks;
using VContainer.Unity;

public class BackendPresenter : IInitializable
{

    private readonly IApiClient _apiClient;
    private readonly IBackendLauncher _backendLauncher;



    public BackendPresenter(IApiClient apiClient,IBackendLauncher backendLauncher)
    {
        _apiClient = apiClient;
        _backendLauncher = backendLauncher;
    }



    public async void Initialize()
    {
        await InitializeBackend();
    }



    private async Task InitializeBackend()
    {

        await _backendLauncher.StartBackend();

        await _backendLauncher.WaitForBackend();

        await CheckBackend();

    }



    private async Task CheckBackend()
    {

        var response = await _apiClient.Get<ApiStatusResponse>("/health");
        UnityEngine.Debug.Log($"Backend: {response.status}"
        );

    }

}