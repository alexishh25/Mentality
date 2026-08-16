using System.Threading.Tasks;

public interface IBackendLauncher
{
    bool IsRunning { get; }
    Task StartBackend();
    void StopBackend();
    Task WaitForBackend();
}