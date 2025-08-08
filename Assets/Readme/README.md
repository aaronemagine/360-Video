
# Quest Headset Player (Unity 6 + FMETP)

1) Import this Assets/ folder into a Unity 6 project with Android + OpenXR.
2) Use Tools → Quest Player → Create Basic Scene (or set up manually).
3) Wire FMETP events to FMETPNetworkClient (Connected, Disconnected, StringReceived).
4) Implement FMETP send in FMETPNetworkClient.Send().
5) Put MP4s in persistentDataPath/Movies or StreamingAssets. Send Play JSON from the tablet.
