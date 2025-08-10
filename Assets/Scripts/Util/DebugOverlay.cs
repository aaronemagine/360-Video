using UnityEngine;
using System;

[DisallowMultipleComponent]
public class DebugOverlay : MonoBehaviour
{
    public AVProNetworkPlayer player;
    public KeyCode toggleKey = KeyCode.F1;
    public bool visible = true;

    Rect _rect = new Rect(16, 16, 520, 130);
    float _held;  // for gamepad/touch toggle

    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) visible = !visible;

        // Optional: long-press both sticks (rough VR toggle)
        float sum = Mathf.Abs(Input.GetAxis("Horizontal")) + Mathf.Abs(Input.GetAxis("Vertical"));
        if (sum > 0.9f) _held += Time.unscaledDeltaTime; else _held = 0f;
        if (_held > 2f) { visible = !visible; _held = 0f; }
    }

    void OnGUI()
    {
        if (!visible || player == null) return;

        string movie = player.CurrentMovie ?? "(none)";
        string lang  = player.CurrentLang ?? "EN";
        string path  = player.CurrentPath ?? "";
        string ready = player.Ready ? "READY" : "LOADING";

        double pos = 0, dur = 0;
        try
        {
            var ctrl = player.mediaPlayer != null ? player.mediaPlayer.Control : null;
            var info = player.mediaPlayer != null ? player.mediaPlayer.Info : null;

            // Use reflection so we don't break Core on method name changes
            if (ctrl != null)
            {
                var m = ctrl.GetType().GetMethod("GetCurrentTimeMs");
                if (m != null) pos = Convert.ToDouble(m.Invoke(ctrl, null)) / 1000.0;
            }
            if (info != null)
            {
                var dm = info.GetType().GetMethod("GetDurationMs");
                if (dm != null) dur = Convert.ToDouble(dm.Invoke(info, null)) / 1000.0;
            }
        }
        catch {}

        GUI.depth = 0;
        var prev = GUI.color;
        GUI.color = new Color(0,0,0,0.6f);
        GUI.Box(_rect, GUIContent.none);
        GUI.color = Color.white;

        GUILayout.BeginArea(_rect);
        GUILayout.Label($"AVPro Net Player — {ready}");
        GUILayout.Label($"Movie: {movie}  Lang: {lang}");
        if (!string.IsNullOrEmpty(path)) GUILayout.Label(TruncatePath(path, 80));
        GUILayout.Label($"Time: {Format(pos)} / {Format(dur)}");
        GUILayout.EndArea();

        GUI.color = prev;
    }

    string Format(double sec)
    {
        if (sec <= 0) return "00:00";
        var t = TimeSpan.FromSeconds(sec);
        return t.Hours > 0 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"mm\:ss");
    }
    string TruncatePath(string p, int max)
    {
        if (string.IsNullOrEmpty(p) || p.Length <= max) return p;
        return "..." + p.Substring(p.Length - max);
    }
}