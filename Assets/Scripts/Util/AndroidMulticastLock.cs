
using UnityEngine;

public class AndroidMulticastLock : MonoBehaviour
{
    private AndroidJavaObject _mcastLock;

    private void OnEnable()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try {
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var wifi = activity.Call<AndroidJavaObject>("getSystemService", "wifi");
            var lockObj = wifi.Call<AndroidJavaObject>("createMulticastLock", "quest_player_presence");
            lockObj.Call("setReferenceCounted", true);
            lockObj.Call("acquire");
            _mcastLock = lockObj;
            Debug.Log("[AndroidMulticastLock] MulticastLock acquired");
        } catch (System.Exception e) {
            Debug.LogWarning("[AndroidMulticastLock] Failed to acquire: " + e.Message);
        }
#endif
    }

    private void OnDisable()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try { _mcastLock?.Call("release"); } catch {}
        _mcastLock = null;
        Debug.Log("[AndroidMulticastLock] MulticastLock released");
#endif
    }
}
