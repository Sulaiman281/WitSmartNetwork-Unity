#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using WitSmartNetwork;

public class SettingsEditorWindow : EditorWindow
{
    private Settings settings;

    private SerializedObject serializedSettings;
    private SerializedProperty modeProp;
    private SerializedProperty serverModeProp;
    private SerializedProperty serverPortProp;
    private SerializedProperty serverIpProp;
    private SerializedProperty groupIdProp;
    private SerializedProperty pingIntervalProp;
    private SerializedProperty pingTimeoutProp;

    [MenuItem("Tools/WitNetwork/Settings")]
    public static void ShowWindow()
    {
        GetWindow<SettingsEditorWindow>("WitNetwork Settings");
    }

    private void OnEnable()
    {
        // Try to find the asset in the project, not via Resources.Load
        string[] guids = AssetDatabase.FindAssets("t:Settings NetworkSettings");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            settings = AssetDatabase.LoadAssetAtPath<Settings>(path);
        }
        else
        {
            settings = null;
        }

        if (settings == null)
        {
            Debug.LogError("NetworkSettings asset not found in project!");
            return;
        }
        serializedSettings = new SerializedObject(settings);
        modeProp = serializedSettings.FindProperty("Mode");
        serverModeProp = serializedSettings.FindProperty("ServerMode");
        serverPortProp = serializedSettings.FindProperty("ServerPort");
        serverIpProp = serializedSettings.FindProperty("ServerIp");
        pingIntervalProp = serializedSettings.FindProperty("PingIntervalSeconds");
        pingTimeoutProp = serializedSettings.FindProperty("PingTimeoutIntervals");
    }

    private void OnGUI()
    {
        if (settings == null)
        {
            EditorGUILayout.HelpBox("NetworkSettings asset not found in project!", MessageType.Error);
            if (GUILayout.Button("Create NetworkSettings Asset"))
            {
                var asset = ScriptableObject.CreateInstance<Settings>();
                AssetDatabase.CreateAsset(asset, "Assets/Resources/NetworkSettings.asset");
                AssetDatabase.SaveAssets();
                settings = Settings.Load();
                OnEnable();
            }
            return;
        }

        // serializedSettings.Update();

        EditorGUILayout.PropertyField(modeProp, new GUIContent("Network Mode"));
        EditorGUILayout.PropertyField(serverModeProp, new GUIContent("Server Mode"));
        EditorGUILayout.PropertyField(serverPortProp, new GUIContent("Server Port"));
        EditorGUILayout.PropertyField(serverIpProp, new GUIContent("Server IP"));
        EditorGUILayout.PropertyField(pingIntervalProp, new GUIContent("Ping Interval (s)"));
        EditorGUILayout.PropertyField(pingTimeoutProp, new GUIContent("Ping Timeout Intervals"));

        EditorGUILayout.Space();

        if (GUILayout.Button("Save"))
        {
            serializedSettings.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Close();
        }
    }
}
#endif