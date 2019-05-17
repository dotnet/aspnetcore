// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
{
    internal class LibuvConnectionListener : IConnectionListener
    {
        private readonly EndPoint _endPoint;

        private readonly List<ListenerContext> _listeners = new List<ListenerContext>();
        private IAsyncEnumerator<LibuvConnection> _acceptEnumerator;

        public LibuvConnectionListener(LibuvTransportContext context, EndPoint endPoint)
            : this(new LibuvFunctions(), context, endPoint)
        { }

        // For testing
        public LibuvConnectionListener(LibuvFunctions uv, LibuvTransportContext context, EndPoint endPoint)
        {
            Libuv = uv;
            TransportContext = context;

            _endPoint = endPoint;
        }

        public LibuvFunctions Libuv { get; }
        public LibuvTransportContext TransportContext { get; }
        public List<LibuvThread> Threads { get; } = new List<LibuvThread>();

        public IHostApplicationLifetime AppLifetime => TransportContext.AppLifetime;
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
            for (var index = 0; index < TransportOptions.ThreadCount; index++)
            {
                Threads.Add(new LibuvThread(this));
            }

            foreach (var thread in Threads)
            {
                await thread.StartAsync().ConfigureAwait(false);
            }

            try
            {
                if (TransportOptions.ThreadCount == 1)
                {
                    var listener = new Listener(TransportContext);
                    _listeners.Add(listener);
                    await listener.StartAsync(_endPoint, Threads[0]).ConfigureAwait(false);
                }
                else
                {
                    var pipeName = (Libuv.IsWindows ? @"\\.\pipe\kestrel_" : "/tmp/kestrel_") + Guid.NewGuid().ToString("n");
                    var pipeMessage = Guid.NewGuid().ToByteArray();

                    var listenerPrimary = new ListenerPrimary(TransportContext);
                    _listeners.Add(listenerPrimary);
                    await listenerPrimary.StartAsync(pipeName, pipeMessage, _endPoint, Threads[0]).ConfigureAwait(false);

                    foreach (var thread in Threads.Skip(1))
                    {
                        var listenerSecondary = new ListenerSecondary(TransportContext);
                        _listeners.Add(listenerSecondary);
                        await listenerSecondary.StartAsync(pipeName, pipeMessage, _endPoint, thread).ConfigureAwait(false);
                    }
                }
                _acceptEnumerator = AcceptConnections();
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

        private async IAsyncEnumerator<LibuvConnection> AcceptConnections()
        {
            var slots = new Task<(LibuvConnection Connection, int Slot)>[_listeners.Count];

            // Issue parallel accepts on all listeners
            for (int i = 0; i < slots.Length; i++)
            {
                var elem = slots[i];
                if (elem == null)
                {
                    slots[i] = AcceptAsync(_listeners[i], i);
                }
            }

            while (true)
            {
                (LibuvConnection connection, int slot) = await await Task.WhenAny(slots);

                // Fill that slot with another accept and yield the connection
                slots[slot] = AcceptAsync(_listeners[slot], slot);

                yield return connection;
            }

            static async Task<(LibuvConnection, int)> AcceptAsync(ListenerContext listener, int slot)
            {
                return (await listener.AcceptAsync(), slot);
            }
        }

        public async Task UnbindAsync()
        {
            var disposeTasks = _listeners.Select(listener => ((IAsyncDisposable)listener).DisposeAsync()).ToArray();

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

        public async ValueTask<ConnectionContext> AcceptAsync()
        {
            if (await _acceptEnumerator.MoveNextAsync())
            {
                return _acceptEnumerator.Current;
            }

            throw new OperationCanceledException();
        }

        public async ValueTask DisposeAsync()
        {
            await UnbindAsync().ConfigureAwait(false);

            if (_acceptEnumerator != null)
            {
                await _acceptEnumerator.DisposeAsync();
            }
        }
    }
}
