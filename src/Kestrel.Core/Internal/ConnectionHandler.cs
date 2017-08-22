// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Protocols;
using Microsoft.AspNetCore.Protocols.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    public class ConnectionHandler : IConnectionHandler
    {
        private readonly ServiceContext _serviceContext;
        private readonly ConnectionDelegate _connectionDelegate;

        public ConnectionHandler(ServiceContext serviceContext, ConnectionDelegate connectionDelegate)
        {
            _serviceContext = serviceContext;
            _connectionDelegate = connectionDelegate;
        }

        private IKestrelTrace Log => _serviceContext.Log;

        public void OnConnection(IFeatureCollection features)
        {
            var connectionContext = new DefaultConnectionContext(features);

            var transportFeature = connectionContext.Features.Get<IConnectionTransportFeature>();

            // REVIEW: Unfortunately, we still need to use the service context to create the pipes since the settings
            // for the scheduler and limits are specified here
            var inputOptions = GetInputPipeOptions(_serviceContext, transportFeature.InputWriterScheduler);
            var outputOptions = GetOutputPipeOptions(_serviceContext, transportFeature.OutputReaderScheduler);

            var pair = connectionContext.PipeFactory.CreateConnectionPair(inputOptions, outputOptions);

            // Set the transport and connection id
            connectionContext.ConnectionId = CorrelationIdGenerator.GetNextId();
            connectionContext.Transport = pair.Transport;

            // This *must* be set before returning from OnConnection
            transportFeature.Application = pair.Application;

            // REVIEW: This task should be tracked by the server for graceful shutdown
            // Today it's handled specifically for http but not for aribitrary middleware
            _ = Execute(connectionContext);
        }

        private async Task Execute(ConnectionContext connectionContext)
        {
            using (BeginConnectionScope(connectionContext))
            {
                Log.ConnectionStart(connectionContext.ConnectionId);

                try
                {
                    await _connectionDelegate(connectionContext);
                }
                catch (Exception ex)
                {
                    Log.LogCritical(0, ex, $"{nameof(ConnectionHandler)}.{nameof(Execute)}() {connectionContext.ConnectionId}");
                }

                Log.ConnectionStop(connectionContext.ConnectionId);
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

        // Internal for testing
        internal static PipeOptions GetInputPipeOptions(ServiceContext serviceContext, IScheduler writerScheduler) => new PipeOptions
        {
            ReaderScheduler = serviceContext.ThreadPool,
            WriterScheduler = writerScheduler,
            MaximumSizeHigh = serviceContext.ServerOptions.Limits.MaxRequestBufferSize ?? 0,
            MaximumSizeLow = serviceContext.ServerOptions.Limits.MaxRequestBufferSize ?? 0
        };

        internal static PipeOptions GetOutputPipeOptions(ServiceContext serviceContext, IScheduler readerScheduler) => new PipeOptions
        {
            ReaderScheduler = readerScheduler,
            WriterScheduler = serviceContext.ThreadPool,
            MaximumSizeHigh = GetOutputResponseBufferSize(serviceContext),
            MaximumSizeLow = GetOutputResponseBufferSize(serviceContext)
        };

        private static long GetOutputResponseBufferSize(ServiceContext serviceContext)
        {
            var bufferSize = serviceContext.ServerOptions.Limits.MaxResponseBufferSize;
            if (bufferSize == 0)
            {
                // 0 = no buffering so we need to configure the pipe so the the writer waits on the reader directly
                return 1;
            }

            // null means that we have no back pressure
            return bufferSize ?? 0;
        }
    }
}
