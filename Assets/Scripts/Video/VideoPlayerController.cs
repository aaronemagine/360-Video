
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Events;
using UnityEngine.Networking;

public class VideoPlayerController : MonoBehaviour
{
    public float screenDistance = 2.0f;
    public float screenWidth = 3.0f;

    public UnityEvent OnPrepared;
    public UnityEvent OnStarted;
    public UnityEvent OnStopped;
    public UnityEvent<string> OnError;

    private VideoPlayer _vp;
    private AudioSource _audio;
    private RenderTexture _rt;
    private Material _mat;
    private GameObject _screen;
    private bool _isPlaying;

    private void Awake()
    {
        EnsureScreen();
        EnsureVideoPlayer();
    }

    private void OnDestroy() { CleanupRT(); }

    private void EnsureScreen()
    {
        if (_screen != null) return;
        var cam = Camera.main;
        if (cam == null) Debug.LogWarning("[Video] No Camera.main found.");
        _screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
        _screen.name = "VideoScreen";
        var t = _screen.transform;
        if (cam != null) {
            t.position = cam.transform.position + cam.transform.forward * screenDistance;
            t.rotation = Quaternion.LookRotation(t.position - cam.transform.position, Vector3.up);
        } else {
            t.position = new Vector3(0, 1.5f, screenDistance);
        }
        var mr = _screen.GetComponent<MeshRenderer>();
        _mat = new Material(Shader.Find("Unlit/Texture"));
        mr.sharedMaterial = _mat;
    }

    private void EnsureVideoPlayer()
    {
        _audio = gameObject.GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;

        _vp = gameObject.GetComponent<VideoPlayer>();
        if (_vp == null) _vp = gameObject.AddComponent<VideoPlayer>();
        _vp.playOnAwake = false;
        _vp.isLooping = false;
        _vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
        _vp.EnableAudioTrack(0, true);
        _vp.SetTargetAudioSource(0, _audio);
        _vp.renderMode = VideoRenderMode.RenderTexture;
        _vp.errorReceived += (v, msg) => { _isPlaying = false; OnError?.Invoke(msg); Debug.LogError("[Video] Error: " + msg); };
        _vp.prepareCompleted += (v) => { OnPrepared?.Invoke(); };
        _vp.loopPointReached += (v) => { _isPlaying = false; OnStopped?.Invoke(); };
        _vp.started += (v) => { _isPlaying = true; OnStarted?.Invoke(); };
    }

    private void CleanupRT()
    {
        if (_rt != null) {
            _vp.targetTexture = null;
            _mat.mainTexture = null;
            _rt.Release();
            UnityEngine.Object.Destroy(_rt);
            _rt = null;
        }
    }

    private void SetupRT(int width, int height)
    {
        CleanupRT();
        _rt = new RenderTexture(Mathf.NextPowerOfTwo(width), Mathf.NextPowerOfTwo(height), 0, RenderTextureFormat.ARGB32);
        _rt.Create();
        _vp.targetTexture = _rt;
        _mat.mainTexture = _rt;
    }

    public IEnumerator PlayMovie(string movieNameOrPath, bool loop, float volume)
    {
        bool ok = false; string absolutePath = string.Empty; bool fromStreaming = false;
        yield return StartCoroutine(ResolvePath(movieNameOrPath, (rOk, rPath, rFromSA) => { ok = rOk; absolutePath = rPath; fromStreaming = rFromSA; }));
        if (!ok) { OnError?.Invoke("File not found: " + movieNameOrPath); yield break; }

        _vp.isLooping = loop;
        _audio.volume = Mathf.Clamp01(volume);
        _vp.source = VideoSource.Url;
        _vp.url = absolutePath;
        Debug.Log("[Video] Playing: " + _vp.url);

        _vp.Prepare();
        float timeout = 10f;
        while (!_vp.isPrepared && timeout > 0f) { timeout -= Time.unscaledDeltaTime; yield return null; }
        if (!_vp.isPrepared) { OnError?.Invoke("Prepare timeout"); yield break; }

        int w = (int)_vp.width, h = (int)_vp.height;
        if (w <= 0 || h <= 0) { w = 1920; h = 1080; }
        SetupRT(w, h);
        float aspect = (float)w / h;
        _screen.transform.localScale = new Vector3(screenWidth, screenWidth / aspect, 1f);
        _vp.Play();
    }

    public void StopMovie()
    {
        if (_vp == null) return;
        try { _vp.Stop(); _isPlaying = false; OnStopped?.Invoke(); } catch {}
    }

    public bool IsPlaying { get { return _isPlaying; } }

    private IEnumerator ResolvePath(string movieNameOrPath, Action<bool,string,bool> onDone)
    {
        if (movieNameOrPath.StartsWith("/") || movieNameOrPath.StartsWith("content://") || movieNameOrPath.StartsWith("http"))
        { onDone(true, movieNameOrPath, false); yield break; }

        string fileName = movieNameOrPath;
        if (!fileName.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)) fileName += ".mp4";

        string p0 = Path.Combine(Application.persistentDataPath, "Movies");
        string p1 = Application.persistentDataPath;
        string sA = Path.Combine(Application.streamingAssetsPath, fileName);
        string c0 = Path.Combine(p0, fileName);
        string c1 = Path.Combine(p1, fileName);

        if (File.Exists(c0)) { onDone(true, c0, false); yield break; }
        if (File.Exists(c1)) { onDone(true, c1, false); yield break; }

#if UNITY_ANDROID && !UNITY_EDITOR
        try { if (!Directory.Exists(p0)) Directory.CreateDirectory(p0); } catch {}
        string dest = Path.Combine(p0, fileName);
        string srcUrl = Path.Combine(Application.streamingAssetsPath, fileName);
        using (var uwr = UnityWebRequest.Get(srcUrl))
        {
            yield return uwr.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
            if (uwr.result != UnityWebRequest.Result.Success)
#else
            if (uwr.isNetworkError || uwr.isHttpError)
#endif
            { onDone(false, string.Empty, false); yield break; }
            try { File.WriteAllBytes(dest, uwr.downloadHandler.data); onDone(true, dest, true); yield break; }
            catch { onDone(false, string.Empty, false); yield break; }
        }
#else
        if (File.Exists(sA)) { onDone(true, sA, true); yield break; }
        onDone(false, string.Empty, false); yield break;
#endif
    }
}
