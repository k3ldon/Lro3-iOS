using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Ro3Ios.Updater
{
    /// <summary>
    /// Thread-safe dispatcher that runs HTTP GET requests via UnityWebRequest on Unity's main thread.
    /// This bypasses Mono/IL2CPP socket deadlocks on iOS and natively leverages iOS NSURLSession.
    /// </summary>
    public static class UnityHttpDispatcher
    {
        class Request
        {
            public Uri Uri;
            public TaskCompletionSource<(int Status, byte[] Body)> Tcs;
            public int TimeoutSeconds;
        }

        static readonly ConcurrentQueue<Request> _queue = new ConcurrentQueue<Request>();

        public static Task<(int Status, byte[] Body)> GetAsync(Uri uri, CancellationToken ct, int timeoutSeconds = 25)
        {
            var tcs = new TaskCompletionSource<(int Status, byte[] Body)>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (ct.IsCancellationRequested)
            {
                tcs.SetCanceled();
                return tcs.Task;
            }

            var req = new Request { Uri = uri, Tcs = tcs, TimeoutSeconds = timeoutSeconds };
            ct.Register(() => tcs.TrySetCanceled());
            _queue.Enqueue(req);
            return tcs.Task;
        }

        public static void ProcessQueue(MonoBehaviour host)
        {
            while (_queue.TryDequeue(out var req))
            {
                if (req.Tcs.Task.IsCanceled) continue;
                host.StartCoroutine(ExecuteRequest(req));
            }
        }

        static IEnumerator ExecuteRequest(Request req)
        {
            using (var uwr = UnityWebRequest.Get(req.Uri))
            {
                uwr.timeout = req.TimeoutSeconds;
                uwr.SetRequestHeader("User-Agent", "Ro3Ios/1.0");
                yield return uwr.SendWebRequest();

                if (req.Tcs.Task.IsCanceled) yield break;

                int status = (int)uwr.responseCode;
                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    req.Tcs.TrySetResult((status > 0 ? status : 200, uwr.downloadHandler.data));
                }
                else
                {
                    if (status > 0 && uwr.downloadHandler != null && uwr.downloadHandler.data != null)
                    {
                        req.Tcs.TrySetResult((status, uwr.downloadHandler.data));
                    }
                    else
                    {
                        req.Tcs.TrySetException(new IOException($"UnityWebRequest error: {uwr.error} ({uwr.result}) for {req.Uri}"));
                    }
                }
            }
        }
    }
}
