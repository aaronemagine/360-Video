using System;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
public class FMETPSendRelay : MonoBehaviour
{
    [Header("Auto-detect")]
    [Tooltip("Scan the scene at Start and bind to the first FMETP component with SendString(string) or SendBytes(byte[]).")]
    public bool autoFindOnStart = true;

    [Header("FMETP target (optional if auto-find is on)")]
    [Tooltip("Drag your FMETP component here (e.g., FMNetworkManager/FMWebSocket*). Leave empty to auto-find.")]
    public Component fmetpTarget;

    [Header("Preferences")]
    [Tooltip("Exact method name if you know it (e.g., SendString). Leave blank to auto-detect.")]
    public string preferredMethodName = "SendString";
    public bool allowByteFallback = true;
    public bool logOutbound = true;
    public bool logScanResults = true;

    [Header("Offline")]
    [Tooltip("When ON, nothing is actually sent; messages are only logged. Safe for testing without a tablet/network.")]
    public bool offlineNoSend = true;

    // cached reflection
    MethodInfo _sendStringMI;
    MethodInfo _sendBytesMI;
    Type _targetType;

    // common names we probe
    static readonly string[] StringNames = {
        "SendString","SendToAll","BroadcastString","WriteString","PushString","SendMessageString",
        "Send","Broadcast","Emit","PostString"
    };
    static readonly string[] BytesNames = {
        "SendBytes","BroadcastBytes","WriteBytes","PushBytes","SendMessageBytes",
        "SendRaw","SendBinary","PostBytes"
    };

    void Start()
    {
        if (autoFindOnStart && fmetpTarget == null) AutoFindTarget();
        TryCacheMethods();
    }

    // ------- Public API your bridge/heartbeat will call -------
    public void SendString(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        if (offlineNoSend)
        {
            if (logOutbound) Debug.Log($"[FMETP Relay] (offline) → {json}");
            return;
        }

        EnsureBound();

        if (_sendStringMI != null)
        {
            if (logOutbound) Debug.Log($"[FMETP Relay] → {json}");
            _sendStringMI.Invoke(fmetpTarget, new object[] { json });
            return;
        }

        if (allowByteFallback && _sendBytesMI != null)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            if (logOutbound) Debug.Log($"[FMETP Relay] → (bytes) {json}");
            _sendBytesMI.Invoke(fmetpTarget, new object[] { bytes });
            return;
        }

        Debug.LogError($"[FMETP Relay] No suitable send method on {_targetType?.Name}.");
    }

    public void SendBytes(byte[] payload)
    {
        if (payload == null || payload.Length == 0) return;

        if (offlineNoSend)
        {
            if (logOutbound) Debug.Log($"[FMETP Relay] (offline) → {payload.Length} bytes");
            return;
        }

        EnsureBound();

        if (_sendBytesMI != null)
        {
            if (logOutbound) Debug.Log($"[FMETP Relay] → {payload.Length} bytes");
            _sendBytesMI.Invoke(fmetpTarget, new object[] { payload });
            return;
        }

        if (_sendStringMI != null)
        {
            var s = Encoding.UTF8.GetString(payload);
            if (logOutbound) Debug.Log($"[FMETP Relay] → (string) {s}");
            _sendStringMI.Invoke(fmetpTarget, new object[] { s });
            return;
        }

        Debug.LogError($"[FMETP Relay] No suitable send method on {_targetType?.Name} for bytes/string.");
    }

    // ---------------- Utilities ----------------
    [ContextMenu("Scan & Auto-bind FMETP target")]
    public void AutoFindTarget()
    {
        Component best = null; MethodInfo bestStr = null, bestBytes = null;

        var all = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var comp in all)
        {
            var t = comp.GetType();
            var (s, b) = FindSendMethods(t);
            if (s == null && b == null) continue;

            if (logScanResults)
                Debug.Log($"[FMETP Relay] Candidate: {GetPath(comp.gameObject)} <{t.Name}>  string:{s?.Name ?? "—"} bytes:{b?.Name ?? "—"}");

            // prefer exact preferred string name, else any string, else any bytes
            if (best == null || (s != null && s.Name == preferredMethodName) || (bestStr == null && s != null))
            { best = comp; bestStr = s; bestBytes = b; if (s != null && s.Name == preferredMethodName) break; }
        }

        if (best != null)
        {
            fmetpTarget = best;
            _sendStringMI = bestStr;
            _sendBytesMI  = bestBytes;
            _targetType   = best.GetType();
            Debug.Log($"[FMETP Relay] Bound to {GetPath(best.gameObject)} <{_targetType.Name}> string:{_sendStringMI?.Name ?? "—"} bytes:{_sendBytesMI?.Name ?? "—"}");
        }
        else
        {
            Debug.LogError("[FMETP Relay] No FMETP-like sender found in scene.");
        }
    }

    void EnsureBound()
    {
        if (fmetpTarget == null) AutoFindTarget();
        if (_sendStringMI == null && _sendBytesMI == null) TryCacheMethods();
    }

    void TryCacheMethods()
    {
        _sendStringMI = null; _sendBytesMI = null; _targetType = fmetpTarget ? fmetpTarget.GetType() : null;
        if (_targetType == null) return;

        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        if (!string.IsNullOrWhiteSpace(preferredMethodName))
            _sendStringMI = FindMethod(_targetType, preferredMethodName, typeof(string), flags);

        if (_sendStringMI == null)
            foreach (var name in StringNames)
                if ((_sendStringMI = FindMethod(_targetType, name, typeof(string), flags)) != null) break;

        if (allowByteFallback)
        {
            if (!string.IsNullOrWhiteSpace(preferredMethodName) && _sendBytesMI == null)
                _sendBytesMI = FindMethod(_targetType, preferredMethodName, typeof(byte[]), flags);

            if (_sendBytesMI == null)
                foreach (var name in BytesNames)
                    if ((_sendBytesMI = FindMethod(_targetType, name, typeof(byte[]), flags)) != null) break;
        }

        Debug.Log($"[FMETP Relay] Target: {_targetType.Name} | string={_sendStringMI?.Name ?? "none"} | bytes={_sendBytesMI?.Name ?? "none"}");
    }

    (MethodInfo str, MethodInfo bytes) FindSendMethods(Type t)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        MethodInfo si = null, bi = null;

        if (!string.IsNullOrWhiteSpace(preferredMethodName))
        {
            si = FindMethod(t, preferredMethodName, typeof(string), flags);
            bi = FindMethod(t, preferredMethodName, typeof(byte[]), flags);
        }
        if (si == null)
            foreach (var name in StringNames)
                if ((si = FindMethod(t, name, typeof(string), flags)) != null) break;

        if (bi == null)
            foreach (var name in BytesNames)
                if ((bi = FindMethod(t, name, typeof(byte[]), flags)) != null) break;

        return (si, bi);
    }

    MethodInfo FindMethod(Type t, string methodName, Type singleParamType, BindingFlags flags)
    {
        try
        {
            return t.GetMethods(flags)
                    .FirstOrDefault(m =>
                    {
                        if (!string.Equals(m.Name, methodName, StringComparison.Ordinal)) return false;
                        var pars = m.GetParameters();
                        return pars.Length == 1 && pars[0].ParameterType == singleParamType;
                    });
        }
        catch { return null; }
    }

    string GetPath(GameObject go)
    {
        if (go == null) return "(null)";
        var path = go.name; var p = go.transform.parent;
        while (p != null) { path = p.name + "/" + path; p = p.parent; }
        return path;
    }

    // Handy tests
    [ContextMenu("Test / SendString ACK")]
    void _TestSendString() => SendString("{\"type\":\"Ack\",\"id\":\"relay-test\",\"ok\":true,\"cmd\":\"Test\",\"note\":\"hello\"}");

    [ContextMenu("Test / SendBytes ACK")]
    void _TestSendBytes() => SendBytes(Encoding.UTF8.GetBytes("{\"type\":\"Ack\",\"id\":\"relay-bytes\",\"ok\":true,\"cmd\":\"Test\"}"));
}