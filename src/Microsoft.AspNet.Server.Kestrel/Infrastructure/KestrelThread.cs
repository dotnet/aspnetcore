using Microsoft.AspNet.Server.Kestrel.Networking;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Server.Kestrel
{
    /// <summary>
    /// Summary description for KestrelThread
    /// </summary>
    public class KestrelThread
    {
        KestrelEngine _engine;
        Thread _thread;
        UvLoopHandle _loop;
        UvAsyncHandle _post;
        Queue<Work> _workAdding = new Queue<Work>();
        Queue<Work> _workRunning = new Queue<Work>();
        object _workSync = new Object();
        bool _stopImmediate = false;

        public KestrelThread(KestrelEngine engine)
        {
            _engine = engine;
            _loop = new UvLoopHandle();
            _post = new UvAsyncHandle();
            _thread = new Thread(ThreadStart);
        }

        public UvLoopHandle Loop { get { return _loop; } }

        public Task StartAsync()
        {
            var tcs = new TaskCompletionSource<int>();
            _thread.Start(tcs);
            return tcs.Task;
        }

        public void Stop(TimeSpan timeout)
        {
            Post(OnStop, null);
            if (!_thread.Join(timeout))
            {
                Post(OnStopImmediate, null);
                if (!_thread.Join(timeout))
                {
                    _thread.Abort();
                }
            }
        }

        private void OnStop(object obj)
        {
            _post.Unreference();
        }

        private void OnStopImmediate(object obj)
        {
            _stopImmediate = true;
            _loop.Stop();
        }

        public void Post(Action<object> callback, object state)
        {
            lock (_workSync)
            {
                _workAdding.Enqueue(new Work { Callback = callback, State = state });
            }
            _post.Send();
        }

        private void ThreadStart(object parameter)
        {
            var tcs = (TaskCompletionSource<int>)parameter;
            try
            {
                _loop.Init(_engine.Libuv);
                _post.Init(_loop, OnPost);
                tcs.SetResult(0);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            var ran1 = _loop.Run();
            if (_stopImmediate)
            {
                // thread-abort form of exit, resources will be leaked
                return;
            }

            // run the loop one more time to delete the _post handle
            _post.Reference();
            _post.Close();
            var ran2 = _loop.Run();

            // delete the last of the unmanaged memory
            _loop.Close();
        }

        private void OnPost()
        {
            var queue = _workAdding;
            lock (_workSync)
            {
                _workAdding = _workRunning;
            }
            _workRunning = queue;
            while (queue.Count != 0)
            {
                var work = queue.Dequeue();
                try
                {
                    work.Callback(work.State);
                }
                catch (Exception ex)
                {
                    //TODO: unhandled exceptions
                }
            }
        }

        private struct Work
        {
            public Action<object> Callback;
            public object State;
        }
    }
}
