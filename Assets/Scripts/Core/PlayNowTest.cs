using UnityEngine;

public class PlayNowTest : MonoBehaviour
{
    public PlayerApp app;
    public string movieName = "demo";   // no .mp4 needed

    [ContextMenu("Send Play")]
    public void SendPlay()
    {
        string json = "{\"type\":\"Play\",\"id\":\"cmd-test\",\"movie\":\"" + movieName + "\",\"language\":\"en\",\"loop\":false,\"volume\":1}";
        var mi = typeof(PlayerApp).GetMethod("HandleMessage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        mi.Invoke(app, new object[] { json });
    }
}
