// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Networking;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal
{
    public class KestrelEngine : IDisposable
    {
        public KestrelEngine(ServiceContext context)
            : this(new Libuv(), context)
        { }

        // For testing
        internal KestrelEngine(Libuv uv, ServiceContext context)
        {
            Libuv = uv;
            ServiceContext = context;
            Threads = new List<KestrelThread>();
        }

        public Libuv Libuv { get; private set; }
        public ServiceContext ServiceContext { get; set; }
        public List<KestrelThread> Threads { get; private set; }

        public IApplicationLifetime AppLifetime => ServiceContext.AppLifetime;
        public IKestrelTrace Log => ServiceContext.Log;
        public IThreadPool ThreadPool => ServiceContext.ThreadPool;
        public KestrelServerOptions ServerOptions => ServiceContext.ServerOptions;

        public void Start(int count)
        {
            for (var index = 0; index < count; index++)
            {
                Threads.Add(new KestrelThread(this));
            }

            foreach (var thread in Threads)
            {
                thread.StartAsync().Wait();
            }
        }

        public void Dispose()
        {
            try
            {
                Task.WaitAll(Threads.Select(thread => thread.StopAsync(TimeSpan.FromSeconds(2.5))).ToArray());
            }
            catch (AggregateException aggEx)
            {
                // An uncaught exception was likely thrown from the libuv event loop.
                // The original error that crashed one loop may have caused secondary errors in others.
                // Make sure that the stack trace of the original error is logged.
                foreach (var ex in aggEx.InnerExceptions)
                {
                    Log.LogCritical("Failed to gracefully close Kestrel.", ex);
                }

                throw;
            }

            Threads.Clear();
#if DEBUG
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
#endif
        }

        public IDisposable CreateServer(ListenOptions listenOptions)
        {
            var listeners = new List<IAsyncDisposable>();

            try
            {
                if (Threads.Count == 1)
                {
                    var listener = new Listener(ServiceContext);
                    listeners.Add(listener);
                    listener.StartAsync(listenOptions, Threads[0]).Wait();
                }
                else
                {
                    var pipeName = (Libuv.IsWindows ? @"\\.\pipe\kestrel_" : "/tmp/kestrel_") + Guid.NewGuid().ToString("n");
                    var pipeMessage = Guid.NewGuid().ToByteArray();

                    var listenerPrimary = new ListenerPrimary(ServiceContext);
                    listeners.Add(listenerPrimary);
                    listenerPrimary.StartAsync(pipeName, pipeMessage, listenOptions, Threads[0]).Wait();

                    foreach (var thread in Threads.Skip(1))
                    {
                        var listenerSecondary = new ListenerSecondary(ServiceContext);
                        listeners.Add(listenerSecondary);
                        listenerSecondary.StartAsync(pipeName, pipeMessage, listenOptions, thread).Wait();
                    }
                }

                return new Disposable(() =>
                {
                    DisposeListeners(listeners);
                });
            }
            catch
            {
                DisposeListeners(listeners);
                throw;
            }
        }

        private void DisposeListeners(List<IAsyncDisposable> listeners)
        {
            var disposeTasks = listeners.Select(listener => listener.DisposeAsync()).ToArray();

            if (!Task.WaitAll(disposeTasks, TimeSpan.FromSeconds(2.5)))
            {
                Log.LogError(0, null, "Disposing listeners failed");
            }
        }
    }
}
