using System;
using System.IO;
using UnityEngine;
using RenderHeads.Media.AVProVideo;

[DisallowMultipleComponent]
public class AVProNetworkPlayer : MonoBehaviour
{
    [Header("Links")]
    public MediaPlayer mediaPlayer;
    public MeshRenderer sphereRenderer;   // optional

    [Header("Movie Location")]
    public string moviesFolder = "";      // auto-detected on Awake if empty

    public event Action<bool> ReadyChanged;     // fired when playback becomes ready / not ready

    // state (core-safe)
    string _currentPath;
    string _currentBaseName;
    string _currentLang = "EN";
    bool _isReady;

    void Awake()
    {
        if (string.IsNullOrWhiteSpace(moviesFolder))
            moviesFolder = DefaultMoviesFolder();

        if (mediaPlayer == null)
            Debug.LogError("[AVProNet] MediaPlayer not assigned.");

        if (mediaPlayer != null)
        {
            mediaPlayer.Events.RemoveListener(OnMediaEvent);
            mediaPlayer.Events.AddListener(OnMediaEvent);
        }
    }

    string DefaultMoviesFolder()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Quest persistent path is best for large mp4s
        return Path.Combine(Application.persistentDataPath, "Movies"); // resolves to /sdcard/Android/data/<bundle>/files/Movies
#else
        return Path.Combine(Application.streamingAssetsPath, "Movies");
#endif
    }

    void OnDestroy()
    {
        if (mediaPlayer != null) mediaPlayer.Events.RemoveListener(OnMediaEvent);
    }

    void OnMediaEvent(MediaPlayer mp, MediaPlayerEvent.EventType evt, ErrorCode err)
    {
        switch (evt)
        {
            case MediaPlayerEvent.EventType.ReadyToPlay:
            case MediaPlayerEvent.EventType.FirstFrameReady:
            case MediaPlayerEvent.EventType.Started:
                SetReady(true);
                break;
            case MediaPlayerEvent.EventType.Closing:
            case MediaPlayerEvent.EventType.FinishedPlaying:
            case MediaPlayerEvent.EventType.Error:
                SetReady(false);
                break;
        }
    }

    void SetReady(bool r)
    {
        if (_isReady == r) return;
        _isReady = r;
        ReadyChanged?.Invoke(_isReady);
    }

    // ---------- Commands ----------
    public void CmdPlay(string movieName, string language = "EN", bool loop = false, float volume = 1f)
    {
        if (mediaPlayer == null) { Debug.LogError("[AVProNet] Play: MediaPlayer missing."); return; }
        if (string.IsNullOrWhiteSpace(movieName)) { Debug.LogError("[AVProNet] Play: movie name empty."); return; }

        _currentBaseName = movieName.Trim();
        _currentLang = (language ?? "EN").Trim().ToUpperInvariant();

        string langFile = Path.Combine(moviesFolder, $"{_currentBaseName}_{_currentLang}.mp4");
        string baseFile = Path.Combine(moviesFolder, $"{_currentBaseName}.mp4");
        string chosen = File.Exists(langFile) ? langFile : (File.Exists(baseFile) ? baseFile : null);

        if (chosen == null)
        {
            Debug.LogError($"[AVProNet] Not found:\n  {langFile}\n  {baseFile}\n(Or pass an absolute path.)");
            return;
        }

        OpenAndPlay(chosen, loop);
    }

    public void CmdPause(bool paused)
    {
        var ctrl = mediaPlayer?.Control;
        if (ctrl == null) { Debug.LogWarning("[AVProNet] Pause: control not ready"); return; }
        try { if (paused) ctrl.Pause(); else ctrl.Play(); } catch (Exception e) { Debug.LogWarning($"[AVProNet] Pause failed: {e.Message}"); }
    }

    public void CmdReset(string movieName = null)
    {
        var ctrl = mediaPlayer?.Control;
        if (ctrl != null)
        {
            try { ctrl.Seek(0.0); ctrl.Pause(); Debug.Log("[AVProNet] Reset 00:00"); }
            catch (Exception e) { Debug.LogWarning($"[AVProNet] Reset failed: {e.Message}"); }
        }
        if (!string.IsNullOrWhiteSpace(movieName))
            CmdPlay(movieName.Trim(), _currentLang, loop: IsLooping());
    }

    public void CmdLanguage(string language)
    {
        _currentLang = (language ?? "EN").Trim().ToUpperInvariant();

        if (string.IsNullOrEmpty(_currentBaseName))
        {
            Debug.LogWarning("[AVProNet] Language ignored: no active base movie. Call Play first.");
            return;
        }

        string dir = !string.IsNullOrEmpty(_currentPath) ? Path.GetDirectoryName(_currentPath) : moviesFolder;
        string candidate = Path.Combine(dir, $"{_currentBaseName}_{_currentLang}.mp4");
        string fallback  = Path.Combine(dir, $"{_currentBaseName}.mp4");

        string chosen = File.Exists(candidate) ? candidate : (File.Exists(fallback) ? fallback : null);
        if (chosen == null) { Debug.LogWarning($"[AVProNet] Language swap missing files:\n  {candidate}\n  {fallback}"); return; }

        OpenAndPlay(chosen, IsLooping());
    }

    void OpenAndPlay(string fullPath, bool loop)
    {
        _currentPath = fullPath;
        SetReady(false);
        Debug.Log($"[AVProNet] Open: {_currentPath}");
        bool ok = mediaPlayer.OpenMedia(MediaPathType.AbsolutePathOrURL, _currentPath, autoPlay: true);
        if (!ok) { Debug.LogError("[AVProNet] OpenMedia returned false"); return; }

        // Try both loop APIs (covers Core)
        try { mediaPlayer.Loop = loop; } catch { }
        var ctrl = mediaPlayer.Control;
        if (ctrl != null) { try { ctrl.SetLooping(loop); } catch { } }
    }

    public bool IsCurrently(string keyMoviePipeLang)
    {
        // Compare "movie|LANG" to current base/lang
        return string.Equals(keyMoviePipeLang, $"{_currentBaseName}|{_currentLang}", StringComparison.OrdinalIgnoreCase);
    }

    bool IsLooping()
    {
        try { return mediaPlayer.Loop; } catch { }
        try { var c = mediaPlayer.Control; if (c != null) return c.IsLooping(); } catch { }
        return false;
    }

    // Exposed for HUD
    public string CurrentMovie => _currentBaseName;
    public string CurrentLang  => _currentLang;
    public string CurrentPath  => _currentPath;
    public bool   Ready        => _isReady;
}