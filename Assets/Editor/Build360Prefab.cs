#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class Build360Prefab : EditorWindow
{
    [MenuItem("Tools/Build 360 Video Prefab (AVPro v3)")]
    static void Create360VideoPrefab()
    {
        // Ensure folders exist
        System.IO.Directory.CreateDirectory("Assets/Prefabs");
        System.IO.Directory.CreateDirectory("Assets/StreamingAssets");

        // Create sphere
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "360VideoSphere";
        sphere.transform.localScale = new Vector3(-100f, 100f, 100f); // Inverted normals

#if AVPRO_VIDEO
        // Add ApplyToMesh
        var applyMesh = sphere.AddComponent<RenderHeads.Media.AVProVideo.ApplyToMesh>();
        applyMesh.MeshRenderer = sphere.GetComponent<MeshRenderer>();

        // Add MediaPlayer
        var mediaPlayer = sphere.AddComponent<RenderHeads.Media.AVProVideo.MediaPlayer>();

        // Add your custom controller
        var controller = sphere.AddComponent<AVPro360PlayerController>();
        controller.sphereRenderer = sphere.GetComponent<MeshRenderer>();
        controller.mediaPlayer = mediaPlayer;
        controller.sphereScale = 100f;
#else
        Debug.LogWarning("AVPro Video not imported — prefab will be sphere only.");
#endif

        // Move camera to inside sphere
        if (Camera.main != null)
            Camera.main.transform.position = Vector3.zero;

        // Save as prefab
        string prefabPath = "Assets/Prefabs/360VideoSphere.prefab";
        PrefabUtility.SaveAsPrefabAsset(sphere, prefabPath);

        Debug.Log("✅ 360 Video Prefab created at " + prefabPath);
        Debug.Log("ℹ️ Place a 360 MP4 named 'test.mp4' into Assets/StreamingAssets and call PlayVideo(path) at runtime.");

        // Clean up
        GameObject.DestroyImmediate(sphere);
    }
}
#endif
