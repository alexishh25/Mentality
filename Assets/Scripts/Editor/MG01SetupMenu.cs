using Mentality.Gameplay.MG01;
using Mentality.UI;
using Mentality.UI.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mentality.EditorTools
{
    // Crea los assets y el cableado de MG01 desde el Editor para no editar YAML de escenas a mano.
    public static class MG01SetupMenu
    {
        private const string ConfigFolder = "Assets/Content/Scriptable Objects/Configs";
        private const string ConfigPath = ConfigFolder + "/MG01Config.asset";
        private const string BasePanelSettingsPath = "Assets/UI Toolkit/PanelSettings.asset";
        private const string HudPanelSettingsPath = "Assets/UI Toolkit/HudPanelSettings.asset";
        private const string HudUxmlPath = "Assets/Content/UI/Documents/Views/HUD/HudUXML.uxml";
        private const string GameOverUxmlPath = "Assets/Content/UI/Documents/Views/Results/GameOverUXML.uxml";
        private const string RewardUxmlPath = "Assets/Content/UI/Documents/Views/Results/RewardUXML.uxml";
        private const string PauseUxmlPath = "Assets/Content/UI/Documents/Views/Pause/PauseUXML.uxml";

        [MenuItem("Mentality/MG01/1. Crear assets (MG01Config + HudPanelSettings)")]
        public static void CreateAssets()
        {
            MG01ConfigSO config = GetOrCreateConfig();
            PanelSettings panel = GetOrCreateHudPanelSettings();
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(config);
            Debug.Log($"[MG01] Assets listos: {AssetDatabase.GetAssetPath(config)}, {AssetDatabase.GetAssetPath(panel)}");
        }

        [MenuItem("Mentality/MG01/2. Montar HUD y pantallas en la escena abierta")]
        public static void SetupOpenScene()
        {
            if (Object.FindFirstObjectByType<MG01LifetimeScope>() != null)
            {
                EditorUtility.DisplayDialog("MG01", "La escena ya tiene un MG01LifetimeScope.", "OK");
                return;
            }

            MG01ConfigSO config = GetOrCreateConfig();
            PanelSettings panel = GetOrCreateHudPanelSettings();
            var hudAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(HudUxmlPath);
            var gameOverAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(GameOverUxmlPath);
            var rewardAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(RewardUxmlPath);
            var pauseAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(PauseUxmlPath);
            if (hudAsset == null || gameOverAsset == null || rewardAsset == null || pauseAsset == null)
            {
                EditorUtility.DisplayDialog("MG01", "Faltan UXML de HUD/Game Over/Recompensa/Pausa. Revisa la consola de importacion.", "OK");
                return;
            }

            var root = new GameObject("MG01");
            Undo.RegisterCreatedObjectUndo(root, "Montar MG01");
            var scope = root.AddComponent<MG01LifetimeScope>();

            var hudObject = CreateChild("HUD", root.transform);
            var hudDocument = hudObject.AddComponent<UIDocument>();
            hudDocument.panelSettings = panel;
            hudDocument.visualTreeAsset = hudAsset;
            hudDocument.sortingOrder = 0;
            var hudView = hudObject.AddComponent<HudView>();
            SetReference(hudView, "document", hudDocument);

            var screensObject = CreateChild("Screens", root.transform);
            var screensDocument = screensObject.AddComponent<UIDocument>();
            screensDocument.panelSettings = panel;
            screensDocument.sortingOrder = 10;
            var uiDoc = screensObject.AddComponent<UIDoc>();

            var gameOverView = CreateChild("GameOverView", screensObject.transform).AddComponent<GameOverView>();
            var rewardView = CreateChild("RewardView", screensObject.transform).AddComponent<RewardView>();
            var pauseView = CreateChild("PauseView", screensObject.transform).AddComponent<PauseView>();

            var uiDocObject = new SerializedObject(uiDoc);
            uiDocObject.FindProperty("uIDocument").objectReferenceValue = screensDocument;
            uiDocObject.FindProperty("showInitialScreen").boolValue = false;
            SerializedProperty screens = uiDocObject.FindProperty("screens");
            screens.arraySize = 3;
            SetScreenEntry(screens.GetArrayElementAtIndex(0), ScreenType.GameOver, gameOverAsset, gameOverView);
            SetScreenEntry(screens.GetArrayElementAtIndex(1), ScreenType.Reward, rewardAsset, rewardView);
            SetScreenEntry(screens.GetArrayElementAtIndex(2), ScreenType.Pause, pauseAsset, pauseView);
            uiDocObject.ApplyModifiedPropertiesWithoutUndo();

            var scopeObject = new SerializedObject(scope);
            scopeObject.FindProperty("config").objectReferenceValue = config;
            scopeObject.FindProperty("hudView").objectReferenceValue = hudView;
            scopeObject.FindProperty("screensUIDoc").objectReferenceValue = uiDoc;
            scopeObject.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
            Debug.Log("[MG01] HUD y pantallas montados. Guarda la escena (Ctrl+S).");
        }

        private static GameObject CreateChild(string name, Transform parent)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void SetReference(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetScreenEntry(SerializedProperty entry, ScreenType type, VisualTreeAsset asset, BaseView view)
        {
            entry.FindPropertyRelative("type").enumValueIndex = (int)type;
            entry.FindPropertyRelative("visualAsset").objectReferenceValue = asset;
            entry.FindPropertyRelative("logicComponent").objectReferenceValue = view;
        }

        private static MG01ConfigSO GetOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<MG01ConfigSO>(ConfigPath);
            if (config != null) return config;

            EnsureFolder(ConfigFolder);
            config = ScriptableObject.CreateInstance<MG01ConfigSO>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static PanelSettings GetOrCreateHudPanelSettings()
        {
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(HudPanelSettingsPath);
            if (panel != null) return panel;

            var basePanel = AssetDatabase.LoadAssetAtPath<PanelSettings>(BasePanelSettingsPath);
            panel = basePanel != null ? Object.Instantiate(basePanel) : ScriptableObject.CreateInstance<PanelSettings>();
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0.5f;
            panel.sortingOrder = 10;
            AssetDatabase.CreateAsset(panel, HudPanelSettingsPath);
            return panel;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
