// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Server.Kestrel.Networking;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Kestrel.Http;

namespace Microsoft.AspNet.Server.Kestrel
{
    public class KestrelEngine
    {

        public KestrelEngine()
        {
            Threads = new List<KestrelThread>();
            Listeners = new List<Listener>();
            Memory = new MemoryPool();
            Libuv = new Libuv();
            Libuv.Load("libuv.dll");
        }

        public Libuv Libuv { get; private set; }
        public IMemoryPool Memory { get; set; }
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

        public IDisposable CreateServer(Func<object, Task> app)
        {
            var listeners = new List<Listener>();
            foreach (var thread in Threads)
            {
                var listener = new Listener(Memory);
                listener.StartAsync(thread, app).Wait();
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
