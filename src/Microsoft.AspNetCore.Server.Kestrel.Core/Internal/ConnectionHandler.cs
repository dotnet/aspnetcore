// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Internal.System.IO.Pipelines;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    public class ConnectionHandler<TContext> : IConnectionHandler
    {
        private static long _lastFrameConnectionId = long.MinValue;

        private readonly ListenOptions _listenOptions;
        private readonly ServiceContext _serviceContext;
        private readonly IHttpApplication<TContext> _application;

        public ConnectionHandler(ListenOptions listenOptions, ServiceContext serviceContext, IHttpApplication<TContext> application)
        {
            _listenOptions = listenOptions;
            _serviceContext = serviceContext;
            _application = application;
        }

        public IConnectionContext OnConnection(IConnectionInformation connectionInfo)
        {
            var inputPipe = connectionInfo.PipeFactory.Create(GetInputPipeOptions(connectionInfo.InputWriterScheduler));
            var outputPipe = connectionInfo.PipeFactory.Create(GetOutputPipeOptions(connectionInfo.OutputReaderScheduler));

            var connectionId = CorrelationIdGenerator.GetNextId();
            var frameConnectionId = Interlocked.Increment(ref _lastFrameConnectionId);

            if (!_serviceContext.ConnectionManager.NormalConnectionCount.TryLockOne())
            {
                var goAway = new RejectionConnection(inputPipe, outputPipe, connectionId, _serviceContext);
                goAway.Reject();
                return goAway;
            }

            var connection = new FrameConnection(new FrameConnectionContext
            {
                ConnectionId = connectionId,
                FrameConnectionId = frameConnectionId,
                ServiceContext = _serviceContext,
                ConnectionInformation = connectionInfo,
                ConnectionAdapters = _listenOptions.ConnectionAdapters,
                Input = inputPipe,
                Output = outputPipe,
            });

            // Since data cannot be added to the inputPipe by the transport until OnConnection returns,
            // Frame.ProcessRequestsAsync is guaranteed to unblock the transport thread before calling
            // application code.
            connection.StartRequestProcessing<TContext>(_application);

            return connection;
        }

        // Internal for testing
        internal PipeOptions GetInputPipeOptions(IScheduler writerScheduler) => new PipeOptions
        {
            ReaderScheduler = _serviceContext.ThreadPool,
            WriterScheduler = writerScheduler,
            MaximumSizeHigh = _serviceContext.ServerOptions.Limits.MaxRequestBufferSize ?? 0,
            MaximumSizeLow = _serviceContext.ServerOptions.Limits.MaxRequestBufferSize ?? 0
        };

        internal PipeOptions GetOutputPipeOptions(IScheduler readerScheduler) => new PipeOptions
        {
            ReaderScheduler = readerScheduler,
            WriterScheduler = _serviceContext.ThreadPool,
            MaximumSizeHigh = GetOutputResponseBufferSize(),
            MaximumSizeLow = GetOutputResponseBufferSize()
        };

        private long GetOutputResponseBufferSize()
        {
            var bufferSize = _serviceContext.ServerOptions.Limits.MaxResponseBufferSize;
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
