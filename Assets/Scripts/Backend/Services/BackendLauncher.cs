using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

public class BackendLauncher : IBackendLauncher, IInitializable, IDisposable
{
    private Process _process;

    public bool IsRunning => _process != null && !_process.HasExited;

    public void Initialize()
    {
        StartBackend();
    }

    public async Task StartBackend()
    {
        if (IsRunning)
            return;


        // Raíz del proyecto Unity (Mentality)
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));


        // Carpeta AIUnityClient
        string aiClientPath = Path.Combine(projectRoot,"AIUnityClient");


        // Carpeta Api
        string apiPath = Path.Combine(aiClientPath,"Api");


        // Python del entorno virtual
        string pythonPath = Path.Combine(aiClientPath,"venv","Scripts","python.exe");


        UnityEngine.Debug.Log($"API Path: {apiPath}");
        UnityEngine.Debug.Log($"Python Path: {pythonPath}");



        if (!File.Exists(pythonPath))
        {
            UnityEngine.Debug.LogError(
                $"No existe Python en: {pythonPath}"
            );
            return;
        }


        if (!File.Exists(
            Path.Combine(apiPath, "launcher.py")
        ))
        {
            UnityEngine.Debug.LogError(
                $"No existe launcher.py en: {apiPath}"
            );
            return;
        }



        ProcessStartInfo info = new ProcessStartInfo
        {
            FileName = pythonPath,

            Arguments = "launcher.py",

            WorkingDirectory = apiPath,

            UseShellExecute = false,

            CreateNoWindow = true,

            RedirectStandardOutput = true,

            RedirectStandardError = true
        };



        _process = Process.Start(info);



        if (_process == null)
        {
            UnityEngine.Debug.LogError(
                "No se pudo iniciar FastAPI"
            );
            return;
        }



        _process.OutputDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
               UnityEngine.Debug.Log(
                    "[FastAPI] " + e.Data
                );
        };


        _process.ErrorDataReceived += (s, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
                UnityEngine.Debug.LogError(
                    "[FastAPI ERROR] " + e.Data
                );
        };


        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();



        UnityEngine.Debug.Log("Iniciando FastAPI...");


        await WaitForBackend();


        UnityEngine.Debug.Log(
            "FastAPI listo"
        );
    }

    public async Task WaitForBackend()
    {
        using HttpClient client = new HttpClient();

        while (true)
        {
            try
            {
                var response = await client.GetAsync("http://127.0.0.1:8000/health");

                if (response.IsSuccessStatusCode)
                {
                    UnityEngine.Debug.Log("Backend listo.");
                    break;
                }
            }
            catch
            {
                // todavía está iniciando
            }

            await Task.Delay(300);
        }
    }

    public void StopBackend()
    {
        if (!IsRunning)
            return;

        _process.Kill();
        _process.Dispose();
        _process = null;
    }

    public void Dispose()
    {
        StopBackend();
    }
}