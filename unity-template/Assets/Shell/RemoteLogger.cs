using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Ro3Ios.Shell
{
    /// <summary>
    /// Captures all Unity engine logs and exceptions and streams them via UDP to the host PC
    /// for full visibility without relying on screen traces or redacted syslogs.
    /// </summary>
    public static class RemoteLogger
    {
        static readonly string[] TargetHosts = new[] { "192.168.2.4", "100.96.72.18" };
        const int UdpPort = 8089;

        static UdpClient _udp;
        static IPEndPoint[] _endpoints;
        static readonly ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();
        static Thread _worker;
        static volatile bool _running;
        static string _logFile;
        static StreamWriter _fileWriter;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            if (_running) return;
            _running = true;

            try
            {
                _logFile = Path.Combine(Application.persistentDataPath, "game_live.log");
                _fileWriter = new StreamWriter(new FileStream(_logFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)) { AutoFlush = true };
            }
            catch { }

            try
            {
                _udp = new UdpClient();
                var eps = new List<IPEndPoint>();
                foreach (var h in TargetHosts)
                {
                    if (IPAddress.TryParse(h, out var ip))
                    {
                        eps.Add(new IPEndPoint(ip, UdpPort));
                    }
                }
                _endpoints = eps.ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[RemoteLogger] UDP init error: " + ex.Message);
            }

            _worker = new Thread(WorkerLoop) { IsBackground = true, Name = "ro3-remote-logger" };
            _worker.Start();

            Application.logMessageReceivedThreaded += OnLog;
            LogRaw($"=== [RO3 iOS] RemoteLogger initialized at {DateTime.Now:O} (v{Application.version}) ===");
        }

        static void OnLog(string condition, string stackTrace, LogType type)
        {
            string prefix = type == LogType.Error || type == LogType.Exception ? " [ERR] " :
                            type == LogType.Warning ? " [WARN] " : " [INFO] ";
            string line = $"{DateTime.Now:HH:mm:ss.fff}{prefix}{condition}";
            if (!string.IsNullOrEmpty(stackTrace) && (type == LogType.Error || type == LogType.Exception))
            {
                line += "\n" + stackTrace;
            }
            LogRaw(line);
        }

        public static void LogRaw(string message)
        {
            try { _fileWriter?.WriteLine(message); } catch { }
            _queue.Enqueue(message);
        }

        static void WorkerLoop()
        {
            while (_running)
            {
                bool hadData = false;
                while (_queue.TryDequeue(out var msg))
                {
                    hadData = true;
                    try
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(msg);
                        if (_udp != null && _endpoints != null)
                        {
                            foreach (var ep in _endpoints)
                            {
                                try { _udp.Send(bytes, bytes.Length, ep); } catch { }
                            }
                        }
                    }
                    catch { }
                }

                if (!hadData) Thread.Sleep(15);
            }
        }
    }
}
