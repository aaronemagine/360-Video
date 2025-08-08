using UnityEngine;
#if AVPRO_VIDEO
using RenderHeads.Media.AVProVideo;
#endif

public class AVPro360PlayerController : MonoBehaviour
{
    [Header("360 Sphere")]
    public MeshRenderer sphereRenderer;
    public float sphereScale = 100f;

#if AVPRO_VIDEO
    [Header("AVPro Components")]
    public MediaPlayer mediaPlayer;
    public ApplyToMesh applyToMesh;

    [Tooltip("Stereo mode keywords: MONO, STEREO_MODE_TOP_BOTTOM, STEREO_MODE_SIDE_BY_SIDE")]
    public string stereoKeyword = "MONO"; 
#endif

    private void Awake()
    {
        if (sphereRenderer != null)
        {
            sphereRenderer.transform.localScale = Vector3.one * sphereScale;
            Debug.Log("[AVPro360] Sphere scale set to " + sphereScale);
        }
        else
        {
            Debug.LogWarning("[AVPro360] No sphereRenderer assigned.");
        }
    }

#if AVPRO_VIDEO
    public void PlayVideo(string absolutePath)
    {
        if (mediaPlayer == null || applyToMesh == null)
        {
            Debug.LogError("[AVPro360] MediaPlayer or ApplyToMesh is not assigned.");
            return;
        }

        if (string.IsNullOrEmpty(absolutePath))
        {
            Debug.LogWarning("[AVPro360] No video path provided. Loading test.mp4 from StreamingAssets if present.");
            absolutePath = System.IO.Path.Combine(Application.streamingAssetsPath, "test.mp4");
        }

        // Apply stereo keyword to the sphere material
        if (sphereRenderer != null && sphereRenderer.sharedMaterial != null)
        {
            var mat = sphereRenderer.sharedMaterial;
            mat.DisableKeyword("MONO");
            mat.DisableKeyword("STEREO_MODE_TOP_BOTTOM");
            mat.DisableKeyword("STEREO_MODE_SIDE_BY_SIDE");
            mat.EnableKeyword(stereoKeyword);
            Debug.Log("[AVPro360] Stereo keyword set to " + stereoKeyword);
        }

        Debug.Log("[AVPro360] Opening video: " + absolutePath);
        bool success = mediaPlayer.OpenMedia(MediaPathType.AbsolutePathOrURL, absolutePath, autoPlay: true);

        if (!success)
        {
            Debug.LogError("[AVPro360] Failed to open media at: " + absolutePath);
        }
    }

    public void StopVideo()
    {
        if (mediaPlayer != null)
        {
            mediaPlayer.CloseMedia();
            Debug.Log("[AVPro360] Video stopped.");
        }
    }
#else
    public void PlayVideo(string absolutePath)
    {
        Debug.LogError("[AVPro360] AVPro Video not imported or AVPRO_VIDEO define not set.");
    }

    public void StopVideo()
    {
        Debug.LogError("[AVPro360] AVPro Video not imported or AVPRO_VIDEO define not set.");
    }
#endif
}
