using UnityEngine;
using System.IO;
using System.Text;

[DisallowMultipleComponent]
public class CrashFileLogger : MonoBehaviour
{
    [Tooltip("Create a new log file at startup. Otherwise append to latest.")]
    public bool rotateOnStart = true;

    string _logDir;
    string _logPath;
    StreamWriter _writer;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        _logDir = Path.Combine(Application.persistentDataPath, "logs");
        Directory.CreateDirectory(_logDir);

        if (rotateOnStart)
        {
            var ts = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _logPath = Path.Combine(_logDir, $"vrplayer_{ts}.log");
        }
        else
        {
            _logPath = Path.Combine(_logDir, "vrplayer.log");
        }

        _writer = new StreamWriter(_logPath, append: !rotateOnStart, Encoding.UTF8) { AutoFlush = true };
        Application.logMessageReceived += HandleLog;

        Debug.Log($"[Logger] Logging to {_logPath}");
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
        try { _writer?.Flush(); _writer?.Dispose(); } catch {}
    }

    void HandleLog(string condition, string stackTrace, LogType type)
    {
        try
        {
            _writer.WriteLine($"{System.DateTime.Now:HH:mm:ss.fff} [{type}] {condition}");
            if (type == LogType.Exception) _writer.WriteLine(stackTrace);
        }
        catch {}
    }
}