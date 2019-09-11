// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    public class ConnectionDispatcher : IConnectionDispatcher
    {
        private static long _lastConnectionId = long.MinValue;

        private readonly ServiceContext _serviceContext;
        private readonly ConnectionDelegate _connectionDelegate;

        public ConnectionDispatcher(ServiceContext serviceContext, ConnectionDelegate connectionDelegate)
        {
            _serviceContext = serviceContext;
            _connectionDelegate = connectionDelegate;
        }

        private IKestrelTrace Log => _serviceContext.Log;

        public Task OnConnection(TransportConnection connection)
        {
            // REVIEW: Unfortunately, we still need to use the service context to create the pipes since the settings
            // for the scheduler and limits are specified here
            var inputOptions = GetInputPipeOptions(_serviceContext, connection.MemoryPool, connection.InputWriterScheduler);
            var outputOptions = GetOutputPipeOptions(_serviceContext, connection.MemoryPool, connection.OutputReaderScheduler);

            var pair = DuplexPipe.CreateConnectionPair(inputOptions, outputOptions);

            // Set the transport and connection id
            connection.ConnectionId = CorrelationIdGenerator.GetNextId();
            connection.Transport = pair.Transport;

            // This *must* be set before returning from OnConnection
            connection.Application = pair.Application;

            return Execute(new KestrelConnection(connection));
        }

        private async Task Execute(KestrelConnection connection)
        {
            var id = Interlocked.Increment(ref _lastConnectionId);
            var connectionContext = connection.TransportConnection;

            try
            {
                _serviceContext.ConnectionManager.AddConnection(id, connection);

                Log.ConnectionStart(connectionContext.ConnectionId);
                KestrelEventSource.Log.ConnectionStart(connectionContext);

                using (BeginConnectionScope(connectionContext))
                {
                    try
                    {
                        await _connectionDelegate(connectionContext);
                    }
                    catch (Exception ex)
                    {
                        Log.LogCritical(0, ex, $"{nameof(ConnectionDispatcher)}.{nameof(Execute)}() {connectionContext.ConnectionId}");
                    }
                    finally
                    {
                        // Complete the transport PipeReader and PipeWriter after calling into application code
                        connectionContext.Transport.Input.Complete();
                        connectionContext.Transport.Output.Complete();
                    }

                    // Wait for the transport to close
                    await CancellationTokenAsTask(connectionContext.ConnectionClosed);
                }
            }
            finally
            {
                Log.ConnectionStop(connectionContext.ConnectionId);
                KestrelEventSource.Log.ConnectionStop(connectionContext);

                connection.Complete();

                _serviceContext.ConnectionManager.RemoveConnection(id);
            }
        }

        private IDisposable BeginConnectionScope(ConnectionContext connectionContext)
        {
            if (Log.IsEnabled(LogLevel.Critical))
            {
                return Log.BeginScope(new ConnectionLogScope(connectionContext.ConnectionId));
            }

            return null;
        }

        private static Task CancellationTokenAsTask(CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }

            // Transports already dispatch prior to tripping ConnectionClosed
            // since application code can register to this token.
            var tcs = new TaskCompletionSource<object>();
            token.Register(state => ((TaskCompletionSource<object>)state).SetResult(null), tcs);
            return tcs.Task;
        }

        // Internal for testing
        internal static PipeOptions GetInputPipeOptions(ServiceContext serviceContext, MemoryPool<byte> memoryPool, PipeScheduler writerScheduler) => new PipeOptions
        (
            pool: memoryPool,
            readerScheduler: serviceContext.Scheduler,
            writerScheduler: writerScheduler,
            pauseWriterThreshold: serviceContext.ServerOptions.Limits.MaxRequestBufferSize ?? 0,
            resumeWriterThreshold: serviceContext.ServerOptions.Limits.MaxRequestBufferSize ?? 0,
            useSynchronizationContext: false,
            minimumSegmentSize: KestrelMemoryPool.MinimumSegmentSize
        );

        internal static PipeOptions GetOutputPipeOptions(ServiceContext serviceContext, MemoryPool<byte> memoryPool, PipeScheduler readerScheduler) => new PipeOptions
        (
            pool: memoryPool,
            readerScheduler: readerScheduler,
            writerScheduler: serviceContext.Scheduler,
            pauseWriterThreshold: GetOutputResponseBufferSize(serviceContext),
            resumeWriterThreshold: GetOutputResponseBufferSize(serviceContext),
            useSynchronizationContext: false,
            minimumSegmentSize: KestrelMemoryPool.MinimumSegmentSize
        );

        private static long GetOutputResponseBufferSize(ServiceContext serviceContext)
        {
            var bufferSize = serviceContext.ServerOptions.Limits.MaxResponseBufferSize;
            if (bufferSize == 0)
            {
                // 0 = no buffering so we need to configure the pipe so the writer waits on the reader directly
                return 1;
            }

            // null means that we have no back pressure
            return bufferSize ?? 0;
        }
    }
}
