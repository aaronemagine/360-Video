
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

public class PlayerApp : MonoBehaviour
{
    public FMETPNetworkClient network;
    public VideoPlayerController video;
    public AndroidMulticastLock multicastLock;

    public string deviceId;
    public string appName = "QuestPlayer";
    public string appVersion = "1.0.0";

    public float heartbeatInterval = 1.0f;
    public UnityEvent<string> OnStatusChanged;

    private readonly HashSet<string> _handledCmds = new HashSet<string>();
    private float _heartbeatT;
    private string _state = "Idle";
    private string _currentMovie = "";

    private void Awake()
    {
        if (string.IsNullOrEmpty(deviceId)) deviceId = SystemInfo.deviceUniqueIdentifier;
#if UNITY_2023_1_OR_NEWER
        if (network == null) network = FindFirstObjectByType<FMETPNetworkClient>();
        if (video == null) video = FindFirstObjectByType<VideoPlayerController>();
#else
        if (network == null) network = FindObjectOfType<FMETPNetworkClient>();
        if (video == null) video = FindObjectOfType<VideoPlayerController>();
#endif
        Application.targetFrameRate = 72;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
    }

    private void OnEnable()
    {
        if (network != null)
        {
            network.OnMessage += HandleMessage;
            network.OnConnected += HandleConnected;
            network.OnDisconnected += HandleDisconnected;
        }
        if (video != null)
        {
            video.OnPrepared.AddListener(() => SetState("Prepared"));
            video.OnStarted.AddListener(() => SetState("Playing"));
            video.OnStopped.AddListener(() => { SetState("Idle"); _currentMovie = ""; });
            video.OnError.AddListener((msg) => { SetState("Error: " + msg); SendState("Error"); });
        }
    }

    private void OnDisable()
    {
        if (network != null)
        {
            network.OnMessage -= HandleMessage;
            network.OnConnected -= HandleConnected;
            network.OnDisconnected -= HandleDisconnected;
        }
    }

    private void Update()
    {
        _heartbeatT += Time.unscaledDeltaTime;
        if (_heartbeatT >= heartbeatInterval)
        {
            _heartbeatT = 0f;
            SendHeartbeat();
        }
    }

    private void HandleConnected()
    {
        Debug.Log("[Player] Connected to controller");
        var hello = new HelloMsg { hello = true, id = deviceId, app = appName, ver = appVersion };
        network.Send(MsgUtil.ToJson(hello));
    }

    private void HandleDisconnected() { Debug.Log("[Player] Disconnected from controller"); }

    private void HandleMessage(string json)
    {
        try
        {
            var type = MsgUtil.SafeGetType(json);
            switch (type)
            {
                case "Play":
                    var pc = MsgUtil.FromJson<PlayCmd>(json);
                    HandlePlay(pc);
                    break;
                case "Stop":
                    var sc = MsgUtil.FromJson<StopCmd>(json);
                    HandleStop(sc);
                    break;
                case "Ping":
                    var ping = MsgUtil.FromJson<PingCmd>(json);
                    Send(new AckMsg { id = ping.id });
                    break;
                default:
                    Debug.Log("[Player] Unknown type: " + type);
                    break;
            }
        }
        catch (Exception e) { Debug.LogError("[Player] HandleMessage exception: " + e); }
    }

    private void HandlePlay(PlayCmd cmd)
    {
        if (cmd == null || string.IsNullOrEmpty(cmd.id)) { Debug.LogWarning("[Player] Invalid Play command"); return; }
        if (!_handledCmds.Add(cmd.id)) { Debug.Log("[Player] Duplicate Play " + cmd.id + " — re-ACK only"); Send(new AckMsg { id = cmd.id }); return; }

        Send(new AckMsg { id = cmd.id });
        if (!string.IsNullOrEmpty(cmd.language)) PlayerPrefs.SetString("language", cmd.language);

        _currentMovie = cmd.movie;
        SetState("Preparing");
        StartCoroutine(PlayCo(cmd));
    }

    private IEnumerator PlayCo(PlayCmd cmd)
    {
        yield return StartCoroutine(video.PlayMovie(cmd.movie, cmd.loop, Mathf.Clamp01(cmd.volume)));
        if (video.IsPlaying) SendState("Playing"); else SendState("Error");
    }

    private void HandleStop(StopCmd cmd)
    {
        if (cmd != null && !string.IsNullOrEmpty(cmd.id)) Send(new AckMsg { id = cmd.id });
        video.StopMovie();
        SendState("Idle");
    }

    private void SetState(string s) { _state = s; OnStatusChanged?.Invoke(s); Debug.Log("[Player] State = " + s); }

    private void SendHeartbeat()
    {
        try {
            var hb = new HeartbeatMsg { id = deviceId, app = appName, ver = appVersion, bat = SystemInfo.batteryLevel, ts = Time.realtimeSinceStartup, state = _state, movie = _currentMovie };
            Send(hb);
        } catch (Exception e) { Debug.LogWarning("[Player] Heartbeat send failed: " + e.Message); }
    }

    private void SendState(string state)
    {
        try {
            var hb = new HeartbeatMsg { id = deviceId, app = appName, ver = appVersion, bat = SystemInfo.batteryLevel, ts = Time.realtimeSinceStartup, state = state, movie = _currentMovie };
            Send(hb);
        } catch (Exception e) { Debug.LogWarning("[Player] State send failed: " + e.Message); }
    }

    private void Send(object msg)
    {
        if (network == null) return;
        var json = MsgUtil.ToJson(msg);
        network.Send(json);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) { if (video != null && video.IsPlaying) video.StopMovie(); }
    }
}
