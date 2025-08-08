
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CreateQuestPlayerScene
{
    [MenuItem("Tools/Quest Player/Create Basic Scene")]
    public static void CreateScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var cam = Camera.main;
        if (cam == null)
        {
            var camGO = new GameObject("Main Camera");
            cam = camGO.AddComponent<Camera>();
            cam.tag = "MainCamera";
        }

        var systems = new GameObject("PlayerSystems");
        systems.AddComponent<CrashLogger>();
        var net   = systems.AddComponent<FMETPNetworkClient>();
        var app   = systems.AddComponent<PlayerApp>();

        var videoGO = new GameObject("Video");
        var vpc = videoGO.AddComponent<VideoPlayerController>();

        app.network = net;
        app.video = vpc;

        systems.AddComponent<AndroidMulticastLock>();

        string path = EditorUtility.SaveFilePanelInProject("Save Scene", "QuestPlayerScene", "unity", "Choose a location");
        if (string.IsNullOrEmpty(path))
        {
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            path = "Assets/Scenes/QuestPlayerScene.unity";
        }
        if (!EditorSceneManager.SaveScene(scene, path))
        {
            EditorUtility.DisplayDialog("Save Failed", "Could not save scene. Please save your scene and try again.", "OK");
            return;
        }

        var list = EditorBuildSettings.scenes;
        bool already = false;
        foreach (var s in list) if (s.path == path) { already = true; break; }
        if (!already)
        {
            var newList = new System.Collections.Generic.List<EditorBuildSettingsScene>(list)
            { new EditorBuildSettingsScene(path, true) };
            EditorBuildSettings.scenes = newList.ToArray();
        }

        EditorUtility.DisplayDialog("Done", "Scene created, saved, and added to Build Settings. Wire FMETP events to FMETPNetworkClient and implement Send() in that class.", "OK");
    }
}
#endif
