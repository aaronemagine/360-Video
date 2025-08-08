
using UnityEngine;
using System.IO;
using System.Text;

public class CrashLogger : MonoBehaviour
{
    private string _path;
    private StringBuilder _buffer = new StringBuilder(2048);

    private void Awake()
    {
        _path = Path.Combine(Application.persistentDataPath, "crash.log");
        Application.logMessageReceived += OnLog;
        Debug.Log("[CrashLogger] Started. Log: " + _path);
    }
    private void OnDestroy() { Application.logMessageReceived -= OnLog; }

    private void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Log || type == LogType.Assert) return;
        try {
            _buffer.Clear();
            _buffer.Append(System.DateTime.Now.ToString("o")).Append(' ')
                   .Append('[').Append(type.ToString()).Append(']').Append(' ')
                   .Append(condition).Append('\n')
                   .Append(stackTrace).Append("\n\n");
            File.AppendAllText(_path, _buffer.ToString());
        } catch {}
    }
}
