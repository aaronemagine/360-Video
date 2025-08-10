using UnityEngine;
using UnityEngine.Events;
using System;

[DisallowMultipleComponent]
public class PresenceHeartbeat : MonoBehaviour
{
    [Tooltip("Where to send presence JSON (wire to FMETP send-string).")]
    public UnityEvent<string> OnSendString;

    [Tooltip("Seconds between pings.")]
    public float intervalSeconds = 5f;

    [Tooltip("Device label to include (defaults to SystemInfo.deviceModel/unique id).")]
    public string deviceNameOverride;

    string _deviceId;
    string _appId;
    float _t;

    void Awake()
    {
        _deviceId = SystemInfo.deviceUniqueIdentifier;
        _appId = Application.identifier;
    }

    void Update()
    {
        _t += Time.unscaledDeltaTime;
        if (_t >= Mathf.Max(1f, intervalSeconds))
        {
            _t = 0f;
            var msg = new Presence
            {
                type = "Presence",
                device = string.IsNullOrEmpty(deviceNameOverride) ? _deviceId : deviceNameOverride,
                app = _appId,
                state = "ready",
                ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            OnSendString?.Invoke(JsonUtility.ToJson(msg));
        }
    }

    [Serializable] class Presence
    {
        public string type;
        public string device;
        public string app;
        public string state;
        public long ts;
    }
}