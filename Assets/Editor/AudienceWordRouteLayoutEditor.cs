using Gameplay.Rhythm.Views;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AudienceWordRouteLayout))]
[CanEditMultipleObjects]
public sealed class AudienceWordRouteLayoutEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var layout = (AudienceWordRouteLayout)target;
        EditorGUILayout.HelpBox(layout.ClearCount + " / " + layout.Count + " recorridos aptos con la configuración validada.",
            layout.ClearCount == 0 ? MessageType.Warning : MessageType.Info);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || targets.Length != 1))
            if (GUILayout.Button("Volver a comprobar recorridos")) NivelAudienceRouteSetup.Prepare();
        DrawDefaultInspector();
    }
}
