using System;
using UnityEngine;

// Tiny adapter: wire your FMETP "On String Received" (or bytes) to OnIncomingJson
[DisallowMultipleComponent]
public class FMETPCommandBridge : MonoBehaviour
{
    [Header("Target Player")]
    public AVProNetworkPlayer player;

    // Hook this to FMETP's string message event
    public void OnIncomingJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;
        try
        {
            var env = JsonUtility.FromJson<Envelope>(json);
            if (env == null || string.IsNullOrEmpty(env.type))
            {
                Debug.LogWarning($"[FMETPBridge] Unknown/empty message: {json}");
                return;
            }

            switch (env.type)
            {
                case "Play":
                    {
                        var p = JsonUtility.FromJson<CmdPlay>(json);
                        string lang = (p.language ?? "EN").Trim().ToUpperInvariant();
                        player.CmdPlay(p.movie, lang, p.loop, Mathf.Clamp01(p.volume <= 0 ? 1f : p.volume));
                        break;
                    }
                case "Pause":
                    {
                        var q = JsonUtility.FromJson<CmdPause>(json);
                        player.CmdPause(q.paused);
                        break;
                    }
                case "Reset":
                    {
                        var r = JsonUtility.FromJson<CmdReset>(json);
                        player.CmdReset(r.movie);
                        break;
                    }
                case "Language":
                    {
                        var l = JsonUtility.FromJson<CmdLanguage>(json);
                        player.CmdLanguage((l.language ?? "EN").Trim().ToUpperInvariant());
                        break;
                    }
                default:
                    Debug.LogWarning($"[FMETPBridge] Unhandled type: {env.type}");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[FMETPBridge] Parse/handle failed: {e.Message}\n{json}");
        }
    }

    // If FMETP gives you BYTES, wire it here instead:
    public void OnIncomingBytes(byte[] data)
    {
        OnIncomingJson(System.Text.Encoding.UTF8.GetString(data ?? Array.Empty<byte>()));
    }

    // -------- message shapes --------
    [Serializable] class Envelope { public string type; public string id; }

    [Serializable]
    class CmdPlay : Envelope
    {
        public string movie;        // logical name, no .mp4
        public string language;     // "EN","FR",...
        public bool loop;
        public float volume = 1f;
    }
    [Serializable] class CmdPause : Envelope { public bool paused; }
    [Serializable] class CmdReset : Envelope { public string movie; }
    [Serializable] class CmdLanguage : Envelope { public string language; }
    
    // Add this inside FMETPCommandBridge class
[ContextMenu("Test Play EN")]
void _TestPlayEN()
{
    OnIncomingJson("{\"type\":\"Play\",\"id\":\"1\",\"movie\":\"demo360\",\"language\":\"EN\",\"loop\":false,\"volume\":1.0}");
}

    [ContextMenu("Test Pause")]
    void _TestPause() => OnIncomingJson("{\"type\":\"Pause\",\"id\":\"2\",\"paused\":true}");

    [ContextMenu("Test Resume")]
    void _TestResume() => OnIncomingJson("{\"type\":\"Pause\",\"id\":\"3\",\"paused\":false}");

    [ContextMenu("Test Reset")]
    void _TestReset() => OnIncomingJson("{\"type\":\"Reset\",\"id\":\"4\",\"movie\":\"demo360\"}");

    [ContextMenu("Test Language FR")]
    void _TestLangFR() => OnIncomingJson("{\"type\":\"Language\",\"id\":\"5\",\"language\":\"FR\"}");

}