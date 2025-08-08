
using UnityEngine;
using System;

public class FMETPNetworkClient : MonoBehaviour, INetworkClient
{
    public event Action<string> OnMessage;
    public event Action OnConnected;
    public event Action OnDisconnected;

    public MonoBehaviour fmNetworkManager;
    public bool verboseLogging = true;
    public bool IsConnected { get; private set; }

    private void Awake()
    {
        if (fmNetworkManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            fmNetworkManager = FindFirstObjectByType<MonoBehaviour>();
#else
            fmNetworkManager = FindObjectOfType<MonoBehaviour>();
#endif
        }
    }

    public void HandleConnected()    { if (verboseLogging) Debug.Log("[FMETP] Connected");    IsConnected = true;  OnConnected?.Invoke(); }
    public void HandleDisconnected() { if (verboseLogging) Debug.Log("[FMETP] Disconnected"); IsConnected = false; OnDisconnected?.Invoke(); }
    public void HandleIncomingString(string json) { if (verboseLogging) Debug.Log("[FMETP] <= " + json); OnMessage?.Invoke(json); }

    public void Send(string json)
    {
        if (verboseLogging) Debug.Log("[FMETP] => " + json);
        try {
            // TODO: hook up your FMETP string send here, e.g.:
            // FMNetworkManager.instance.SendToAllString(json);
            // or fmNetworkManager.SendMessage("SendToAllString", json);
        } catch (Exception e) {
            Debug.LogWarning("[FMETP] Send failed: " + e.Message);
        }
    }
}
