
using System;
using Microsoft.AspNet.Server.Kestrel.Networking;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Server.Kestrel
{
    public class KestrelEngine
    {

        public KestrelEngine()
        {
            Threads = new List<KestrelThread>();
            Listeners = new List<Listener>();
            Libuv = new Libuv();
            Libuv.Load("libuv.dll");
        }

        public Libuv Libuv { get; private set; }
        public List<KestrelThread> Threads { get; private set; }
        public List<Listener> Listeners { get; private set; }

        public void Start(int count)
        {
            for (var index = 0; index != count; ++index)
            {
                Threads.Add(new KestrelThread(this));
            }

            foreach (var thread in Threads)
            {
                thread.StartAsync().Wait();
            }
        }

        public void Stop()
        {
            foreach (var thread in Threads)
            {
                thread.Stop(TimeSpan.FromSeconds(45));
            }
            Threads.Clear();
        }

        public IDisposable CreateServer()
        {
            var listeners = new List<Listener>();
            foreach (var thread in Threads)
            {
                var listener = new Listener(thread);
                listener.StartAsync().Wait();
                listeners.Add(listener);
            }
            return new Disposable(() =>
            {
                foreach (var listener in listeners)
                {
                    listener.Dispose();
                }
            });
        }
    }
}
