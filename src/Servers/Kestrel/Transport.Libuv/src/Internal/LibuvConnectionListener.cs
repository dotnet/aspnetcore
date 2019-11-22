// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal
{
    internal class LibuvConnectionListener : IConnectionListener
    {
        private readonly List<ListenerContext> _listeners = new List<ListenerContext>();
        private IAsyncEnumerator<LibuvConnection> _acceptEnumerator;
        private bool _stopped;
        private bool _disposed;

        public LibuvConnectionListener(LibuvTransportContext context, EndPoint endPoint)
            : this(new LibuvFunctions(), context, endPoint)
        { }

        // For testing
        public LibuvConnectionListener(LibuvFunctions uv, LibuvTransportContext context, EndPoint endPoint)
        {
            Libuv = uv;
            TransportContext = context;

            EndPoint = endPoint;
        }

        public LibuvFunctions Libuv { get; }
        public LibuvTransportContext TransportContext { get; }
        public List<LibuvThread> Threads { get; } = new List<LibuvThread>();

        public IHostApplicationLifetime AppLifetime => TransportContext.AppLifetime;
        public ILibuvTrace Log => TransportContext.Log;
        public LibuvTransportOptions TransportOptions => TransportContext.Options;

        public EndPoint EndPoint { get; set; }

        public async ValueTask<ConnectionContext> AcceptAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (await _acceptEnumerator.MoveNextAsync())
            {
                return _acceptEnumerator.Current;
            }

            // null means we're done...
            return null;
        }

        public async ValueTask UnbindAsync(CancellationToken cancellationToken = default)
        {
            if (_stopped)
            {
                return;
            }

            _stopped = true;

            var disposeTasks = _listeners.Select(listener => ((IAsyncDisposable)listener).DisposeAsync()).ToArray();

            if (!await WaitAsync(Task.WhenAll(disposeTasks), TimeSpan.FromSeconds(5)).ConfigureAwait(false))
            {
                Log.LogError(0, null, "Disposing listeners failed");
            }
        }


        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            await UnbindAsync().ConfigureAwait(false);

            foreach (var listener in _listeners)
            {
                await listener.AbortQueuedConnectionAsync().ConfigureAwait(false);
            }

            _listeners.Clear();

            await StopThreadsAsync().ConfigureAwait(false);
        }

        internal async Task StopThreadsAsync()
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

        internal async Task BindAsync()
        {
            // TODO: Move thread management to LibuvTransportFactory
            // TODO: Split endpoint management from thread management
            for (var index = 0; index < TransportOptions.ThreadCount; index++)
            {
                Threads.Add(new LibuvThread(Libuv, TransportContext));
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
                    await listener.StartAsync(EndPoint, Threads[0]).ConfigureAwait(false);
                    EndPoint = listener.EndPoint;
                }
                else
                {
                    var pipeName = (Libuv.IsWindows ? @"\\.\pipe\kestrel_" : "/tmp/kestrel_") + Guid.NewGuid().ToString("n");
                    var pipeMessage = Guid.NewGuid().ToByteArray();

                    var listenerPrimary = new ListenerPrimary(TransportContext);
                    _listeners.Add(listenerPrimary);
                    await listenerPrimary.StartAsync(pipeName, pipeMessage, EndPoint, Threads[0]).ConfigureAwait(false);
                    EndPoint = listenerPrimary.EndPoint;

                    foreach (var thread in Threads.Skip(1))
                    {
                        var listenerSecondary = new ListenerSecondary(TransportContext);
                        _listeners.Add(listenerSecondary);
                        await listenerSecondary.StartAsync(pipeName, pipeMessage, EndPoint, thread).ConfigureAwait(false);
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
            var slots = new Task<(LibuvConnection, int)>[_listeners.Count];
            // This is the task we'll put in the slot when each listening completes. It'll prevent
            // us from having to shrink the array. We'll just loop while there are active slots.
            var incompleteTask = new TaskCompletionSource<(LibuvConnection, int)>().Task;

            var remainingSlots = slots.Length;

            // Issue parallel accepts on all listeners
            for (int i = 0; i < remainingSlots; i++)
            {
                slots[i] = AcceptAsync(_listeners[i], i);
            }

            while (remainingSlots > 0)
            {
                // Calling GetAwaiter().GetResult() is safe because we know the task is completed
                (var connection, var slot) = (await Task.WhenAny(slots)).GetAwaiter().GetResult();

                // If the connection is null then the listener was closed
                if (connection == null)
                {
                    remainingSlots--;
                    slots[slot] = incompleteTask;
                }
                else
                {
                    // Fill that slot with another accept and yield the connection
                    slots[slot] = AcceptAsync(_listeners[slot], slot);

                    yield return connection;
                }
            }

            static async Task<(LibuvConnection, int)> AcceptAsync(ListenerContext listener, int slot)
            {
                return (await listener.AcceptAsync(), slot);
            }
        }

        private static async Task<bool> WaitAsync(Task task, TimeSpan timeout)
        {
            return await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(false) == task;
        }
    }
}
