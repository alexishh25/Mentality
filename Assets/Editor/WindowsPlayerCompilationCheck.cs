using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Player;

// Explicitly requested player compilation; never runs during normal imports.
[InitializeOnLoad]
public static class WindowsPlayerCompilationCheck
{
    static WindowsPlayerCompilationCheck()
    {
        if (File.Exists("Temp/WindowsPlayerCompilation.request")) EditorApplication.update += Run;
    }
    static void Run()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorApplication.update -= Run;
        File.Delete("Temp/WindowsPlayerCompilation.request");
        Directory.CreateDirectory("Logs");
        try
        {
            var result = PlayerBuildInterface.CompilePlayerScripts(new ScriptCompilationSettings
            {
                target = BuildTarget.StandaloneWindows64,
                group = BuildTargetGroup.Standalone,
                options = ScriptCompilationOptions.None
            }, "Temp/WindowsPlayerCompilation");
            bool success = result.assemblies != null && result.assemblies.Count > 0;
            File.WriteAllText("Logs/WindowsPlayerCompilation.txt", success ? "PASS: Windows x64 player scripts compiled successfully. Assemblies: " + result.assemblies.Count : "FAIL: No compiled player assemblies returned.");
        }
        catch (Exception ex) { File.WriteAllText("Logs/WindowsPlayerCompilation.txt", "FAIL: " + ex); }
    }
}


