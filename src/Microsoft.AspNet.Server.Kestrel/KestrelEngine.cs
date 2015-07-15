// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Server.Kestrel.Networking;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Kestrel.Http;
using Microsoft.Framework.Runtime;
using System.IO;

namespace Microsoft.AspNet.Server.Kestrel
{
    public class KestrelEngine : IDisposable
    {
        public KestrelEngine(ILibraryManager libraryManager, IApplicationShutdown appShutdownService)
        {
            AppShutdown = appShutdownService;
            Threads = new List<KestrelThread>();
            Listeners = new List<Listener>();
            Memory = new MemoryPool();
            Libuv = new Libuv();

            var libraryPath = default(string);

            if (libraryManager != null)
            {
                var library = libraryManager.GetLibraryInformation("Microsoft.AspNet.Server.Kestrel");
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

        public Libuv Libuv { get; private set; }
        public IMemoryPool Memory { get; set; }
        public IApplicationShutdown AppShutdown { get; private set; }
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
            var listeners = new List<Listener>();

            try
            {
                foreach (var thread in Threads)
                {
                    var listener = new Listener(Memory);

                    listeners.Add(listener);
                    listener.StartAsync(scheme, host, port, thread, application).Wait();
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
