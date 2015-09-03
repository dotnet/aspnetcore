// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Kestrel.Http;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Microsoft.AspNet.Server.Kestrel.Networking;
using Microsoft.Dnx.Runtime;

namespace Microsoft.AspNet.Server.Kestrel
{
    public class KestrelEngine : IDisposable
    {
        private readonly ServiceContext _serviceContext;

        public KestrelEngine(ILibraryManager libraryManager, IApplicationShutdown appShutdownService)
            : this(appShutdownService)
        {
            Libuv = new Libuv();

            var libraryPath = default(string);

            if (libraryManager != null)
            {
                var library = libraryManager.GetLibrary("Microsoft.AspNet.Server.Kestrel");
                libraryPath = library.Path;
                if (library.Type == "Project")
                {
                    libraryPath = Path.GetDirectoryName(libraryPath);
                }
                if (Libuv.IsWindows)
                {
                    var architecture = IntPtr.Size == 4
                        ? "x86"
                        : "amd64";

                    libraryPath = Path.Combine(
                        libraryPath,
                        "native",
                        "windows",
                        architecture,
                        "libuv.dll");
                }
                else if (Libuv.IsDarwin)
                {
                    libraryPath = Path.Combine(
                        libraryPath,
                        "native",
                        "darwin",
                        "universal",
                        "libuv.dylib");
                }
                else
                {
                    libraryPath = "libuv.so.1";
                }
            }
            Libuv.Load(libraryPath);
        }

        // For testing
        internal KestrelEngine(Libuv uv, IApplicationShutdown appShutdownService)
           : this(appShutdownService)
        {
            Libuv = uv;
        }

        private KestrelEngine(IApplicationShutdown appShutdownService)
        {
            _serviceContext = new ServiceContext
            {
                AppShutdown = appShutdownService,
                Memory = new MemoryPool()
            };

            Threads = new List<KestrelThread>();
        }

        public Libuv Libuv { get; private set; }
        public List<KestrelThread> Threads { get; private set; }

        public void Start(int count)
        {
            for (var index = 0; index != count; ++index)
            {
                Threads.Add(new KestrelThread(this, _serviceContext));
            }

            foreach (var thread in Threads)
            {
                thread.StartAsync().Wait();
            }
        }

        public void Dispose()
        {
            foreach (var thread in Threads)
            {
                thread.Stop(TimeSpan.FromSeconds(2.5));
            }
            Threads.Clear();
        }

        public IDisposable CreateServer(string scheme, string host, int port, Func<Frame, Task> application)
        {
            var listeners = new List<IDisposable>();
            var usingPipes = host.StartsWith(Constants.UnixPipeHostPrefix);
            if (usingPipes)
            {
                // Subtract one because we want to include the '/' character that starts the path.
                host = host.Substring(Constants.UnixPipeHostPrefix.Length - 1);
            }

            try
            {
                var pipeName = (Libuv.IsWindows ? @"\\.\pipe\kestrel_" : "/tmp/kestrel_") + Guid.NewGuid().ToString("n");

                var single = Threads.Count == 1;
                var first = true;

                foreach (var thread in Threads)
                {
                    if (single)
                    {
                        var listener = usingPipes ? 
                            (Listener) new PipeListener(_serviceContext) : 
                            new TcpListener(_serviceContext);
                        listeners.Add(listener);
                        listener.StartAsync(scheme, host, port, thread, application).Wait();
                    }
                    else if (first)
                    {
                        var listener = usingPipes
                            ? (ListenerPrimary) new PipeListenerPrimary(_serviceContext)
                            : new TcpListenerPrimary(_serviceContext);

                        listeners.Add(listener);
                        listener.StartAsync(pipeName, scheme, host, port, thread, application).Wait();
                    }
                    else
                    {
                        var listener = usingPipes
                            ? (ListenerSecondary) new PipeListenerSecondary(_serviceContext)
                            : new TcpListenerSecondary(_serviceContext);
                        listeners.Add(listener);
                        listener.StartAsync(pipeName, thread, application).Wait();
                    }

                    first = false;
                }
                return new Disposable(() =>
                {
                    foreach (var listener in listeners)
                    {
                        listener.Dispose();
                    }
                });
            }
            catch
            {
                foreach (var listener in listeners)
                {
                    listener.Dispose();
                }

                throw;
            }
        }
    }
}
