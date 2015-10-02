// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Server.Kestrel.Http;
using Microsoft.AspNet.Server.Kestrel.Infrastructure;
using Microsoft.AspNet.Server.Kestrel.Networking;
using Microsoft.Dnx.Runtime;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Server.Kestrel
{
    public class KestrelEngine : ServiceContext, IDisposable
    {
        public KestrelEngine(ILibraryManager libraryManager, ServiceContext context)
            : this(context)
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
                        : "x64";

                    libraryPath = Path.Combine(
                        libraryPath,
                        "runtimes",
                        "win7-" + architecture,
                        "native",
                        "libuv.dll");
                }
                else if (Libuv.IsDarwin)
                {
                    libraryPath = Path.Combine(
                        libraryPath,
                        "runtimes",
                        "osx",
                        "native",
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
        internal KestrelEngine(Libuv uv, ServiceContext context)
           : this(context)
        {
            Libuv = uv;
        }

        private KestrelEngine(ServiceContext context)
            : base(context)
        {
            Threads = new List<KestrelThread>();
        }

        public Libuv Libuv { get; private set; }
        public List<KestrelThread> Threads { get; private set; }

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
            foreach (var thread in Threads)
            {
                thread.Stop(TimeSpan.FromSeconds(2.5));
            }
            Threads.Clear();
        }

        public IDisposable CreateServer(ServerAddress address, Func<IFeatureCollection, Task> application)
        {
            var listeners = new List<IDisposable>();

            var usingPipes = address.IsUnixPipe;

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
                            (Listener) new PipeListener(this) : 
                            new TcpListener(this);
                        listeners.Add(listener);
                        listener.StartAsync(address, thread, application).Wait();
                    }
                    else if (first)
                    {
                        var listener = usingPipes
                            ? (ListenerPrimary) new PipeListenerPrimary(this)
                            : new TcpListenerPrimary(this);

                        listeners.Add(listener);
                        listener.StartAsync(pipeName, address, thread, application).Wait();
                    }
                    else
                    {
                        var listener = usingPipes
                            ? (ListenerSecondary) new PipeListenerSecondary(this)
                            : new TcpListenerSecondary(this);
                        listeners.Add(listener);
                        listener.StartAsync(pipeName, address, thread, application).Wait();
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
