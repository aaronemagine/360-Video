using System;
using System.IO;
using UnityEngine;
using RenderHeads.Media.AVProVideo;

[DisallowMultipleComponent]
public class AVProNetworkPlayer : MonoBehaviour
{
    [Header("Links")]
    public MediaPlayer mediaPlayer;              // assign in Inspector
    public MeshRenderer sphereRenderer;          // optional (diagnostics only)

    [Header("Movie Location")]
    [Tooltip("Leave empty to use StreamingAssets/Movies. For Quest, you can also pass absolute paths in Play commands.")]
    public string moviesFolder = "";

    // We keep our own state instead of using MediaPlayer.Path (not available in Core)
    private string _currentPath;         // full path to current file
    private string _currentBaseName;     // logical movie name without suffix/lang
    private string _currentLang = "EN";  // last requested language

    void Awake()
    {
        if (string.IsNullOrWhiteSpace(moviesFolder))
            moviesFolder = Path.Combine(Application.streamingAssetsPath, "Movies");

        // Folder may not exist in Editor; that's OK if you pass absolute paths
        if (!Directory.Exists(moviesFolder))
            Debug.LogWarning($"[AVProNet] Movies folder not found (ok if using absolute paths): {moviesFolder}");

        if (mediaPlayer == null)
            Debug.LogError("[AVProNet] MediaPlayer not assigned.");
    }

    // ---------------- Commands (called from FMETP bridge) ----------------

    /// <summary>
    /// Play movie by logical name (no .mp4). Tries {name}_{LANG}.mp4 first, then {name}.mp4.
    /// </summary>
    public void CmdPlay(string movieName, string language = "EN", bool loop = false, float volume = 1f)
    {
        if (mediaPlayer == null) { Debug.LogError("[AVProNet] Play: MediaPlayer missing."); return; }
        if (string.IsNullOrWhiteSpace(movieName)) { Debug.LogError("[AVProNet] Play: movie name empty."); return; }

        _currentBaseName = movieName.Trim();
        _currentLang = (language ?? "EN").Trim().ToUpperInvariant();

        // 1) language-specific file in Movies folder
        string langFile = Path.Combine(moviesFolder, $"{_currentBaseName}_{_currentLang}.mp4");
        // 2) base file in Movies folder
        string baseFile = Path.Combine(moviesFolder, $"{_currentBaseName}.mp4");

        string chosen = File.Exists(langFile) ? langFile :
                        File.Exists(baseFile) ? baseFile : null;

        if (chosen == null)
        {
            Debug.LogError($"[AVProNet] Not found:\n  {langFile}\n  {baseFile}\n(Or pass an absolute path in your Play command.)");
            return;
        }

        OpenAndPlay(chosen, loop /*, volume*/);
    }

    /// <summary>
    /// Pause/resume current movie.
    /// </summary>
    public void CmdPause(bool paused)
    {
        var ctrl = mediaPlayer?.Control;
        if (ctrl == null) { Debug.LogWarning("[AVProNet] Pause: control not ready"); return; }
        try { if (paused) ctrl.Pause(); else ctrl.Play(); }
        catch (Exception e) { Debug.LogWarning($"[AVProNet] Pause failed: {e.Message}"); }
        Debug.Log($"[AVProNet] {(paused ? "Paused" : "Resumed")}");
    }

    /// <summary>
    /// Seek to 00:00 and pause. If movieName is supplied, reopen that movie at start (keeping last language).
    /// </summary>
    public void CmdReset(string movieName = null)
    {
        var ctrl = mediaPlayer?.Control;
        if (ctrl != null)
        {
            try { ctrl.Seek(0.0); ctrl.Pause(); Debug.Log("[AVProNet] Reset to 00:00"); }
            catch (Exception e) { Debug.LogWarning($"[AVProNet] Reset failed: {e.Message}"); }
        }

        if (!string.IsNullOrWhiteSpace(movieName))
        {
            // reopen requested title using last language choice
            CmdPlay(movieName.Trim(), _currentLang, loop: IsLooping(), volume: 1f);
        }
    }

    /// <summary>
    /// Change language by swapping files to *_LANG.mp4. (Core edition: no audio track switching API.)
    /// </summary>
    public void CmdLanguage(string language)
    {
        _currentLang = (language ?? "EN").Trim().ToUpperInvariant();

        if (string.IsNullOrEmpty(_currentBaseName))
        {
            Debug.LogWarning("[AVProNet] Language ignored: no active movie base name yet. Call Play first.");
            return;
        }

        // Compute candidate in same folder as last file if we have one; otherwise Movies folder
        string dir = !string.IsNullOrEmpty(_currentPath) ? Path.GetDirectoryName(_currentPath) : moviesFolder;
        string candidate = Path.Combine(dir, $"{_currentBaseName}_{_currentLang}.mp4");
        string fallback = Path.Combine(dir, $"{_currentBaseName}.mp4");

        string chosen = File.Exists(candidate) ? candidate :
                        File.Exists(fallback) ? fallback : null;

        if (chosen == null)
        {
            Debug.LogWarning($"[AVProNet] Language swap failed; missing files:\n  {candidate}\n  {fallback}");
            return;
        }

        OpenAndPlay(chosen, IsLooping() /*, volume*/);
    }

    // ---------------- Internals ----------------

    private void OpenAndPlay(string fullPath, bool loop /*, float volume*/)
    {
        _currentPath = fullPath;

        Debug.Log($"[AVProNet] Open: {_currentPath}");
        bool ok = mediaPlayer.OpenMedia(MediaPathType.AbsolutePathOrURL, _currentPath, autoPlay: true);
        if (!ok) { Debug.LogError("[AVProNet] OpenMedia returned false"); return; }

        // Loop: try property then control (covers API differences)
        try { mediaPlayer.Loop = loop; } catch { /* ignore */ }
        var ctrl = mediaPlayer.Control;
        if (ctrl != null) { try { ctrl.SetLooping(loop); } catch { /* ignore */ } }

        // Volume: Core setups vary (AudioOutput vs AudioSource). Implement if you wire an AudioSource or AudioOutput.
        // (Intentionally omitted here to stay Core-safe.)
    }

    private bool IsLooping()
    {
        try { return mediaPlayer.Loop; } catch { }
        try { var c = mediaPlayer.Control; if (c != null) return c.IsLooping(); } catch { }
        return false;
    }
}