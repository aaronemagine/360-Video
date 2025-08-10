using System.Text;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class MockFMETPTransport : MonoBehaviour
{
    [Header("Inbound to Headset (simulate controller → headset)")]
    public UnityEvent<string> OnReceivedString;   // wire to FMETPCommandBridge.OnIncomingJson
    public UnityEvent<byte[]> OnReceivedBytes;    // (optional) wire to FMETPCommandBridge.OnIncomingBytes

    [Header("Logging")]
    public bool logOutbound = true;   // when the headset "sends" to controller (ACK/Presence)
    public bool logInbound  = true;   // when we simulate controller messages into the headset

    [Header("Manual JSON input (for Simulate/Send)")]
    [TextArea(3, 8)] public string testJson = "{\"type\":\"Play\",\"id\":\"1\",\"movie\":\"demo360\",\"language\":\"EN\",\"loop\":false,\"volume\":1.0}";

    // --------- Outbound (headset → controller) ---------
    public void SendString(string s)
    {
        if (logOutbound) Debug.Log($"[MockFMETP] SendString → {s}");
        // No actual network — this is just a logger placeholder.
    }

    public void SendBytes(byte[] data)
    {
        if (logOutbound) Debug.Log($"[MockFMETP] SendBytes → {data?.Length ?? 0} bytes");
        // No actual network — this is just a logger placeholder.
    }

    // --------- Inbound simulation (controller → headset) ---------
    public void SimulateIncomingString(string s)
    {
        if (logInbound) Debug.Log($"[MockFMETP] IncomingString ← {s}");
        OnReceivedString?.Invoke(s);
    }

    public void SimulateIncomingBytes(byte[] data)
    {
        if (logInbound) Debug.Log($"[MockFMETP] IncomingBytes ← {data?.Length ?? 0} bytes");
        OnReceivedBytes?.Invoke(data);
    }

    // --------- Handy context-menu actions (run from Inspector ⋮ in Play mode) ---------
    [ContextMenu("Simulate / Play EN")]
    void _SimPlayEN()  => SimulateIncomingString("{\"type\":\"Play\",\"id\":\"1\",\"movie\":\"demo360\",\"language\":\"EN\",\"loop\":false,\"volume\":1.0}");

    [ContextMenu("Simulate / Pause")]
    void _SimPause()   => SimulateIncomingString("{\"type\":\"Pause\",\"id\":\"2\",\"paused\":true}");

    [ContextMenu("Simulate / Resume")]
    void _SimResume()  => SimulateIncomingString("{\"type\":\"Pause\",\"id\":\"3\",\"paused\":false}");

    [ContextMenu("Simulate / Reset")]
    void _SimReset()   => SimulateIncomingString("{\"type\":\"Reset\",\"id\":\"4\",\"movie\":\"demo360\"}");

    [ContextMenu("Simulate / Language FR")]
    void _SimLangFR()  => SimulateIncomingString("{\"type\":\"Language\",\"id\":\"5\",\"language\":\"FR\"}");

    [ContextMenu("Simulate / Send testJson")]
    void _SimSendBox() => SimulateIncomingString(testJson);
}