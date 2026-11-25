using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Ro3Ios.Updater
{
    /// <summary>
    /// The first screen of the iOS app: connects to https://ro3.lagnarok.online/ (or configured patch-repo),
    /// compares local game files, downloads missing chunks, assembles them into StreamingAssets,
    /// and then calls OnReady to start the game.
    /// </summary>
    public sealed class UpdateScreen : MonoBehaviour
    {
        public Action OnReady;

        enum View { Checking, Confirm, Working, UpdateRequired, Error, Done }

        volatile View _view = View.Checking;
        volatile string _message = "Buscando actualizaciones…", _error = "";

        // On-screen diagnostics: iOS redacts Debug.Log in syslog, so the updater narrates itself here.
        static readonly object _traceLock = new object();
        static readonly List<string> _traceLines = new List<string>(16);
        static System.Diagnostics.Stopwatch _traceClock;
        public static void Trace(string message)
        {
            lock (_traceLock)
            {
                _traceClock = _traceClock ?? System.Diagnostics.Stopwatch.StartNew();
                _traceLines.Add(_traceClock.Elapsed.TotalSeconds.ToString("0.0") + "s " + message);
                while (_traceLines.Count > 14) _traceLines.RemoveAt(0);
            }
        }
        public static string TraceText()
        {
            lock (_traceLock) { return string.Join("\n", _traceLines); }
        }
        string _repoUrl, _channelName, _publicKey, _root, _appVersion;
        bool _offlinePlayable, _wifi;
        Installer _installer;
        Repository _repo;
        Plan _plan;
        Channel _channel;
        TaskCompletionSource<bool> _confirm;
        readonly CancellationTokenSource _cts = new CancellationTokenSource();
        long _free;
        bool _verifyAll;

        volatile float _speed;
        volatile bool _quit;
        Thread _notifier;

        Texture2D _bg, _barBack, _barFill, _button;
        GUIStyle _title, _text, _small, _buttonStyle;

        void Start()
        {
            _root = Application.persistentDataPath;
            _appVersion = Application.version;
            var config = Resources.Load<TextAsset>("Ro3Patch");
            if (config != null)
            {
                foreach (var line in config.text.Split('\n'))
                {
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = line.Substring(0, eq).Trim(), value = line.Substring(eq + 1).Trim();
                    if (key == "repo") _repoUrl = value;
                    else if (key == "channel") _channelName = value;
                    else if (key == "key") _publicKey = value;
                }
            }
            _repoUrl = Override("patch-repo") ?? _repoUrl ?? "https://ro3.lagnarok.online/";
            _channelName = Override("patch-channel") ?? _channelName ?? "live";

            Debug.Log($"[Ro3Ios] updater: repo={_repoUrl} channel={_channelName}, app={_appVersion}");
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            _repo = new Repository(new Uri(_repoUrl));
            _installer = new Installer(_root, _repo) { Log = m => Debug.Log("[Ro3Ios] updater: " + m) };
            _verifyAll = File.Exists(Path.Combine(_root, "patch-verify"));
            if (_verifyAll) File.Delete(Path.Combine(_root, "patch-verify"));

            _notifier = new Thread(NotifyLoop) { IsBackground = true, Name = "ro3-update-notify" };
            _notifier.Start();
            Begin();
        }

        string Override(string name)
        {
            try
            {
                string path = Path.Combine(_root, name);
                return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
            }
            catch (IOException) { return null; }
        }

        void Begin()
        {
            _view = View.Checking;
            _message = "Buscando actualizaciones…";
            Trace("begin: repo=" + _repoUrl + " channel=" + _channelName);
            _offlinePlayable = _installer.IsComplete() && _installer.QuickVerify().Count == 0;
            _wifi = Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork;
            Trace("local state: complete=" + _installer.IsComplete() + " wifi=" + _wifi);
            _free = MobilePlatform.FreeSpace(_root);
            _installer.FreeSpace = _ => _free;
            bool verifyAll = _verifyAll;
            _verifyAll = false;
            Task.Run(() => RunAsync(verifyAll));
        }

        async Task RunAsync(bool verifyAll)
        {
            Trace("run: requesting channel");
            try
            {
                try
                {
                    _channel = await _repo.GetChannelAsync(_channelName, _cts.Token);
                    Trace("channel ok: " + _channel.Version);
                }
                catch (Exception e) when (!(e is UpdateException))
                {
                    Debug.LogWarning("[Ro3Ios] updater: channel: " + e);
                    Trace("channel FAILED: " + e.GetType().Name + ": " + e.Message);
                    Fail("No se pudo conectar con el servidor de actualizaciones. Revisa tu conexión a Internet.");
                    return;
                }

                if (!string.IsNullOrEmpty(_publicKey) && !_channel.VerifySignature(_publicKey))
                {
                    _offlinePlayable = false;
                    Fail("La actualización recibida no tiene una firma digital válida.");
                    return;
                }

                if (verifyAll || !_installer.IsComplete(_channel))
                {
                    Trace("iscomplete=false, going to plan");
                }
                else
                {
                    Trace("iscomplete=true, quick-verifying");
                    var quick = await Task.Run(() => _installer.QuickVerify());
                    Trace("quickverify done: " + quick.Count + " changed");
                    if (quick.Count == 0)
                    {
                        _view = View.Done;
                        return;
                    }
                }

                _message = "Comprobando archivos del juego…";
                Trace("preparing plan (manifest " + _channel.Manifest + ")");
                _plan = await _installer.PrepareAsync(_channel, verifyAll, _cts.Token);
                Trace("plan ready: " + _plan.Install.Count + " files, " + _plan.DownloadBytes + " bytes");

                if (_plan.DownloadBytes > 0 && (!_wifi || _plan.DownloadBytes > (500L << 20)))
                {
                    _confirm = new TaskCompletionSource<bool>();
                    _view = View.Confirm;
                    if (!await _confirm.Task) return;
                }

                _view = View.Working;
                Trace("applying plan");
                await _installer.ApplyAsync(_plan, _cts.Token);
                Trace("apply done");
                _view = View.Done;
            }
            catch (OperationCanceledException) { }
            catch (UpdateException e) { Fail(e.Message); }
            catch (Exception e)
            {
                Debug.LogError("[Ro3Ios] updater error: " + e);
                Trace("run FAILED: " + e.GetType().Name + ": " + e.Message);
                Fail("La actualización falló: " + e.Message);
            }
        }

        void Fail(string message)
        {
            _error = message;
            _view = View.Error;
        }

        void Update()
        {
            if (_installer == null) return;
            if (_view == View.Done) Finish();
        }

        void NotifyLoop()
        {
            try
            {
                var clock = System.Diagnostics.Stopwatch.StartNew();
                double last = 0;
                long lastDone = 0;
                while (!_quit)
                {
                    Thread.Sleep(500);
                    var status = _installer.Status;
                    double now = clock.Elapsed.TotalSeconds;
                    if (now - last >= 1)
                    {
                        long done = Interlocked.Read(ref status.Done);
                        _speed = (float)Math.Max(0, (done - lastDone) / (now - last));
                        lastDone = done;
                        last = now;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Ro3Ios] notifier loop: " + e.Message);
            }
        }

        int Percent()
        {
            var s = _installer?.Status;
            if (s == null) return -1;
            long total = Interlocked.Read(ref s.Total), done = Interlocked.Read(ref s.Done);
            if (total <= 0) return -1;
            return (int)Mathf.Clamp((float)done * 100f / total, 0, 100);
        }

        string Headline()
        {
            var s = _installer.Status;
            long done = Interlocked.Read(ref s.Done), total = Interlocked.Read(ref s.Total);
            string baseInfo = total > 0 ? $"{Installer.Format(done)} / {Installer.Format(total)}" : "";
            float sp = _speed;
            if (sp > 1024)
            {
                string speedText = $" · {Installer.Format((long)sp)}/s";
                if (total > done) speedText += $" (ETA: {Eta(total - done, sp)})";
                return baseInfo + speedText;
            }
            return baseInfo;
        }

        static string Eta(long remaining, float speed)
        {
            if (speed <= 0) return "calculando…";
            int seconds = (int)(remaining / speed);
            if (seconds < 60) return $"{seconds} s";
            if (seconds < 3600) return $"{seconds / 60} min";
            return $"{seconds / 3600} h {seconds % 3600 / 60} min";
        }

        void Finish()
        {
            if (!enabled) return;
            enabled = false;
            StopNotifier();
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
            _repo?.Dispose();
            Debug.Log("[Ro3Ios] updater: data ready, starting the game");
            var ready = OnReady;
            Destroy(gameObject);
            ready?.Invoke();
        }

        void StopNotifier()
        {
            _quit = true;
            _notifier?.Join(2000);
            _notifier = null;
        }

        void OnApplicationQuit()
        {
            _cts.Cancel();
            StopNotifier();
        }

        void OnDestroy() => StopNotifier();

        static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        void Styles(float scale)
        {
            if (_bg != null) return;
            _bg = Solid(new Color32(16, 20, 30, 255));
            _barBack = Solid(new Color32(44, 52, 70, 255));
            _barFill = Solid(new Color32(236, 170, 60, 255));
            _button = Solid(new Color32(236, 170, 60, 255));
            _title = new GUIStyle(GUI.skin.label) { fontSize = (int)(42 * scale), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _title.normal.textColor = new Color32(246, 214, 150, 255);
            _text = new GUIStyle(GUI.skin.label) { fontSize = (int)(22 * scale), alignment = TextAnchor.MiddleCenter, wordWrap = true };
            _text.normal.textColor = Color.white;
            _small = new GUIStyle(_text) { fontSize = (int)(16 * scale) };
            _small.normal.textColor = new Color32(160, 170, 190, 255);
            _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = (int)(22 * scale), fontStyle = FontStyle.Bold };
            _buttonStyle.normal.background = _button;
            _buttonStyle.active.background = _barFill;
            _buttonStyle.hover.background = _button;
            _buttonStyle.normal.textColor = _buttonStyle.hover.textColor = _buttonStyle.active.textColor = new Color32(30, 24, 12, 255);
        }

        void OnGUI()
        {
            if (_installer == null) return;
            float scale = Mathf.Max(0.6f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
            Styles(scale);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _bg);
            float w = Mathf.Min(Screen.width - 64 * scale, 900 * scale), x = (Screen.width - w) / 2, y = Screen.height * 0.22f;
            GUI.Label(new Rect(x, y, w, 60 * scale), "Lagnarok RO3", _title);
            y += 90 * scale;

            switch (_view)
            {
                case View.Checking:
                    Line(ref y, x, w, scale, _message, _text);
                    if (_installer.Status.Stage == Stage.Scanning) Progress(ref y, x, w, scale);
                    Line(ref y, x, w, scale, TraceText(), _small);
                    break;
                case View.Confirm:
                    Line(ref y, x, w, scale, $"Es necesario descargar {Installer.Format(_plan.DownloadBytes)} ({_plan.Install.Count} archivos).", _text);
                    Line(ref y, x, w, scale, $"Espacio libre: {Installer.Format(_free)}." + (_wifi ? "" : " Estás usando datos móviles: se recomienda Wi-Fi."), _small);
                    if (Button(ref y, x, w, scale, _wifi ? "Descargar actualización" : "Descargar con datos móviles")) _confirm.TrySetResult(true);
                    break;
                case View.Working:
                    Line(ref y, x, w, scale, $"Descargando archivos: {Interlocked.Read(ref _installer.Status.FilesDone)} de {Interlocked.Read(ref _installer.Status.FilesTotal)}", _text);
                    Progress(ref y, x, w, scale);
                    Line(ref y, x, w, scale, "Descargando contenido desde la red de parches…", _small);
                    break;
                case View.Error:
                    Line(ref y, x, w, scale, _error, _text);
                    if (Button(ref y, x, w, scale, "Reintentar")) Begin();
                    if (_offlinePlayable && Button(ref y, x, w, scale, "Jugar sin conexión")) _view = View.Done;
                    if (Button(ref y, x, w, scale, "Verificar todos los archivos")) { _verifyAll = true; Begin(); }
                    break;
            }

            GUI.Label(new Rect(0, Screen.height - 36 * scale, Screen.width - 16 * scale, 30 * scale), "v" + _appVersion + (_channel != null ? " · " + _channel.Version : ""), new GUIStyle(_small) { alignment = TextAnchor.MiddleRight });
        }

        void Line(ref float y, float x, float w, float scale, string text, GUIStyle style)
        {
            float h = style.CalcHeight(new GUIContent(text), w) + 12 * scale;
            GUI.Label(new Rect(x, y, w, h), text, style);
            y += h;
        }

        void Progress(ref float y, float x, float w, float scale)
        {
            int percent = Percent();
            var bar = new Rect(x, y + 8 * scale, w, 22 * scale);
            GUI.DrawTexture(bar, _barBack);
            if (percent > 0) GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * percent / 100f, bar.height), _barFill);
            y += 44 * scale;
            Line(ref y, x, w, scale, (percent >= 0 ? percent + "% · " : "") + Headline(), _small);
        }

        bool Button(ref float y, float x, float w, float scale, string label)
        {
            bool clicked = GUI.Button(new Rect(x + w / 2 - 220 * scale, y + 10 * scale, 440 * scale, 64 * scale), label, _buttonStyle);
            y += 84 * scale;
            return clicked;
        }
    }

    static class MobilePlatform
    {
        public static long FreeSpace(string path)
        {
            try
            {
                return new DriveInfo(Path.GetPathRoot(Path.GetFullPath(path))).AvailableFreeSpace;
            }
            catch
            {
                return 64L * 1024 * 1024 * 1024; // 64 GB fallback
            }
        }
    }
}
