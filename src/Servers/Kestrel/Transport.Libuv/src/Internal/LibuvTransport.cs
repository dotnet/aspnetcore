// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
{
    public class LibuvTransport : ITransport
    {
        private readonly IEndPointInformation _endPointInformation;

        private readonly List<IAsyncDisposable> _listeners = new List<IAsyncDisposable>();

        public LibuvTransport(LibuvTransportContext context, IEndPointInformation endPointInformation)
            : this(new LibuvFunctions(), context, endPointInformation)
        { }

        // For testing
        public LibuvTransport(LibuvFunctions uv, LibuvTransportContext context, IEndPointInformation endPointInformation)
        {
            Libuv = uv;
            TransportContext = context;

            _endPointInformation = endPointInformation;
        }

        public LibuvFunctions Libuv { get; }
        public LibuvTransportContext TransportContext { get; }
        public List<LibuvThread> Threads { get; } = new List<LibuvThread>();

        public IApplicationLifetime AppLifetime => TransportContext.AppLifetime;
        public ILibuvTrace Log => TransportContext.Log;
        public LibuvTransportOptions TransportOptions => TransportContext.Options;

        public async Task StopAsync()
        {
            try
            {
                await Task.WhenAll(Threads.Select(thread => thread.StopAsync(TimeSpan.FromSeconds(5))).ToArray())
                    .ConfigureAwait(false);
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
#if DEBUG && !INNER_LOOP
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
#endif
        }

        public async Task BindAsync()
        {
            // TODO: Move thread management to LibuvTransportFactory
            // TODO: Split endpoint management from thread management

            // When `FinOnError` is false (the default), we need to be able to forcibly abort connections.
            // On Windows, libuv 1.10.0 will call `shutdown`, preventing forcible abort, on any socket
            // not flagged as `UV_HANDLE_SHARED_TCP_SOCKET`.  The only way we've found to cause socket
            // to be flagged as `UV_HANDLE_SHARED_TCP_SOCKET` is to share it across a named pipe (which
            // must, itself, be flagged `ipc`), which naturally happens when a `ListenerPrimary` dispatches
            // a connection to a `ListenerSecondary`.  Therefore, in scenarios where this is required, we
            // tell the `ListenerPrimary` to dispatch *all* connections to secondary and create an
            // additional `ListenerSecondary` to replace the lost capacity.
            var dispatchAllToSecondary = Libuv.IsWindows && !TransportContext.Options.FinOnError;

            var threadCount = dispatchAllToSecondary
                ? TransportOptions.ThreadCount + 1
                : TransportOptions.ThreadCount;

            for (var index = 0; index < threadCount; index++)
            {
                Threads.Add(new LibuvThread(this));
            }

            foreach (var thread in Threads)
            {
                await thread.StartAsync().ConfigureAwait(false);
            }

            try
            {
                if (threadCount == 1)
                {
                    Debug.Assert(!dispatchAllToSecondary, "Should have taken the primary/secondary code path");

                    var listener = new Listener(TransportContext);
                    _listeners.Add(listener);
                    await listener.StartAsync(_endPointInformation, Threads[0]).ConfigureAwait(false);
                }
                else
                {
                    var pipeName = (Libuv.IsWindows ? @"\\.\pipe\kestrel_" : "/tmp/kestrel_") + Guid.NewGuid().ToString("n");
                    var pipeMessage = Guid.NewGuid().ToByteArray();

                    var listenerPrimary = new ListenerPrimary(TransportContext, dispatchAllToSecondary);
                    _listeners.Add(listenerPrimary);
                    await listenerPrimary.StartAsync(pipeName, pipeMessage, _endPointInformation, Threads[0]).ConfigureAwait(false);

                    foreach (var thread in Threads.Skip(1))
                    {
                        var listenerSecondary = new ListenerSecondary(TransportContext);
                        _listeners.Add(listenerSecondary);
                        await listenerSecondary.StartAsync(pipeName, pipeMessage, _endPointInformation, thread).ConfigureAwait(false);
                    }
                }
            }
            catch (UvException ex) when (ex.StatusCode == LibuvConstants.EADDRINUSE)
            {
                await UnbindAsync().ConfigureAwait(false);
                throw new AddressInUseException(ex.Message, ex);
            }
            catch
            {
                await UnbindAsync().ConfigureAwait(false);
                throw;
            }
        }

        public async Task UnbindAsync()
        {
            var disposeTasks = _listeners.Select(listener => listener.DisposeAsync()).ToArray();

            if (!await WaitAsync(Task.WhenAll(disposeTasks), TimeSpan.FromSeconds(5)).ConfigureAwait(false))
            {
                Log.LogError(0, null, "Disposing listeners failed");
            }

            _listeners.Clear();
        }

        private static async Task<bool> WaitAsync(Task task, TimeSpan timeout)
        {
            return await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(false) == task;
        }
    }
}
