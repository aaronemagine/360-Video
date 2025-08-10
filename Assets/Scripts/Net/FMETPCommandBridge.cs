using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class FMETPCommandBridge : MonoBehaviour
{
    [Header("Targets")]
    public AVProNetworkPlayer player;

    [Header("Outbound (optional)")]
    [Tooltip("Wire this to your FMETP send-string method to emit ACK/Presence JSON back to the tablet.")]
    public UnityEvent<string> OnSendString;

    [Header("Safety")]
    [Tooltip("Queue commands until MediaPlayer is ready (FirstFrameReady/ReadyToPlay).")]
    public bool queueUntilReady = true;

    [Tooltip("Ignore identical Play(movie|LANG) if it is already active to avoid reopens.")]
    public bool debounceDuplicatePlay = true;

    // state
    readonly Queue<Action> _pending = new Queue<Action>();
    bool _isReady;
    string _deviceId;
    string _appId;

    void Awake()
    {
        _deviceId = SystemInfo.deviceUniqueIdentifier;
        _appId = Application.identifier;
    }

    void OnEnable()
    {
        if (player != null)
        {
            player.ReadyChanged -= OnReadyChanged;
            player.ReadyChanged += OnReadyChanged;
        }
    }

    void OnDisable()
    {
        if (player != null) player.ReadyChanged -= OnReadyChanged;
    }

    void OnReadyChanged(bool ready)
    {
        _isReady = ready;
        if (!_isReady) return;

        // drain queue
        while (_pending.Count > 0)
        {
            var a = _pending.Dequeue();
            try { a?.Invoke(); }
            catch (Exception e) { Debug.LogWarning($"[FMETPBridge] queued command failed: {e.Message}"); }
        }
    }

    // ---------- Incoming from FMETP ----------
    public void OnIncomingJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;

        Envelope env = null;
        try { env = JsonUtility.FromJson<Envelope>(json); }
        catch (Exception e) { SendError(null, "parse", $"Invalid JSON: {e.Message}"); return; }

        if (env == null || string.IsNullOrEmpty(env.type))
        { SendError(null, "parse", "Missing type"); return; }

        try
        {
            switch (env.type)
            {
                case "Play":
                {
                    var p = JsonUtility.FromJson<CmdPlay>(json);
                    string lang = (p.language ?? "EN").Trim().ToUpperInvariant();
                    string key = $"{p.movie}|{lang}";

                    Action act = () =>
                    {
                        if (debounceDuplicatePlay && player != null && player.IsCurrently(key))
                        {
                            SendAck(env.id, env.type, note: "duplicate");
                            return;
                        }
                        player?.CmdPlay(p.movie, lang, p.loop, Mathf.Clamp01(p.volume <= 0 ? 1f : p.volume));
                        SendAck(env.id, env.type, note: "opened");
                    };

                    if (queueUntilReady && !_isReady) _pending.Enqueue(act); else act();
                    break;
                }
                case "Pause":
                {
                    var q = JsonUtility.FromJson<CmdPause>(json);
                    Action act = () => { player?.CmdPause(q.paused); SendAck(env.id, env.type, note: q.paused ? "paused" : "resumed"); };
                    if (queueUntilReady && !_isReady) _pending.Enqueue(act); else act();
                    break;
                }
                case "Reset":
                {
                    var r = JsonUtility.FromJson<CmdReset>(json);
                    Action act = () => { player?.CmdReset(r.movie); SendAck(env.id, env.type, note: "reset"); };
                    if (queueUntilReady && !_isReady) _pending.Enqueue(act); else act();
                    break;
                }
                case "Language":
                {
                    var l = JsonUtility.FromJson<CmdLanguage>(json);
                    string lang = (l.language ?? "EN").Trim().ToUpperInvariant();
                    Action act = () => { player?.CmdLanguage(lang); SendAck(env.id, env.type, note: lang); };
                    if (queueUntilReady && !_isReady) _pending.Enqueue(act); else act();
                    break;
                }
                default:
                    SendError(env.id, env.type, "Unhandled type");
                    break;
            }
        }
        catch (Exception e)
        {
            SendError(env.id, env.type, e.Message);
        }
    }

    // If FMETP gives you BYTES, wire this instead:
    public void OnIncomingBytes(byte[] data)
    {
        OnIncomingJson(System.Text.Encoding.UTF8.GetString(data ?? Array.Empty<byte>()));
    }

    // ---------- Outbound helpers ----------
    void SendAck(string id, string cmd, string note = null)
    {
        var ack = new Ack { type = "Ack", id = id, ok = true, cmd = cmd, device = _deviceId, app = _appId, note = note };
        OnSendString?.Invoke(JsonUtility.ToJson(ack));
    }
    void SendError(string id, string cmd, string message)
    {
        var err = new Ack { type = "Ack", id = id, ok = false, cmd = cmd, device = _deviceId, app = _appId, error = message };
        OnSendString?.Invoke(JsonUtility.ToJson(err));
    }

    // ---------- DTOs ----------
    [Serializable] class Envelope { public string type; public string id; }
    [Serializable] class CmdPlay : Envelope { public string movie; public string language; public bool loop; public float volume = 1f; }
    [Serializable] class CmdPause : Envelope { public bool paused; }
    [Serializable] class CmdReset : Envelope { public string movie; }
    [Serializable] class CmdLanguage : Envelope { public string language; }

    [Serializable] class Ack
    {
        public string type;
        public string id;
        public bool ok;
        public string cmd;
        public string device;
        public string app;
        public string note;
        public string error;
    }

    // ---------- ContextMenu tests (run from Inspector ⋮ while in Play mode) ----------
    [ContextMenu("Test / Play EN")]
    void _TestPlayEN()
    {
        OnIncomingJson("{\"type\":\"Play\",\"id\":\"1\",\"movie\":\"demo360\",\"language\":\"EN\",\"loop\":false,\"volume\":1.0}");
    }

    [ContextMenu("Test / Pause")]
    void _TestPause()
    {
        OnIncomingJson("{\"type\":\"Pause\",\"id\":\"2\",\"paused\":true}");
    }

    [ContextMenu("Test / Resume")]
    void _TestResume()
    {
        OnIncomingJson("{\"type\":\"Pause\",\"id\":\"3\",\"paused\":false}");
    }

    [ContextMenu("Test / Reset")]
    void _TestReset()
    {
        OnIncomingJson("{\"type\":\"Reset\",\"id\":\"4\",\"movie\":\"demo360\"}");
    }

    [ContextMenu("Test / Language FR")]
    void _TestLangFR()
    {
        OnIncomingJson("{\"type\":\"Language\",\"id\":\"5\",\"language\":\"FR\"}");
    }
}