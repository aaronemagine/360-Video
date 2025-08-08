
using System;
public interface INetworkClient
{
    event Action<string> OnMessage;
    event Action OnConnected;
    event Action OnDisconnected;
    void Send(string json);
    bool IsConnected { get; }
}
