
using System;
using UnityEngine;

[Serializable] public class HelloMsg { public bool hello; public string id; public string app; public string ver; }
[Serializable] public class HeartbeatMsg { public string type = "Heartbeat"; public string id; public string app; public string ver; public float bat; public float ts; public string state; public string movie; }
[Serializable] public class AckMsg { public string type = "Ack"; public string id; }

[Serializable] public class PlayCmd { public string type; public string id; public string movie; public string language; public bool loop; public float volume = 1f; }
[Serializable] public class StopCmd { public string type; public string id; }
[Serializable] public class PingCmd { public string type; public string id; }

[Serializable] public class TypeProbe { public string type; }

public static class MsgUtil
{
    public static string ToJson<T>(T obj) { return JsonUtility.ToJson(obj); }
    public static T FromJson<T>(string json) { return JsonUtility.FromJson<T>(json); }

    public static string SafeGetType(string json)
    {
        try {
            var probe = JsonUtility.FromJson<TypeProbe>(json);
            if (probe == null) return string.Empty;
            return string.IsNullOrEmpty(probe.type) ? string.Empty : probe.type;
        } catch { return string.Empty; }
    }
}
