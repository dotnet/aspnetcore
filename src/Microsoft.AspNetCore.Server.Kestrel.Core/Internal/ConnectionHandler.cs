// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Transport;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal
{
    public class ConnectionHandler<TContext> : IConnectionHandler
    {
        // Base32 encoding - in ascii sort order for easy text based sorting
        private static readonly string _encode32Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUV";

        // Seed the _lastConnectionId for this application instance with
        // the number of 100-nanosecond intervals that have elapsed since 12:00:00 midnight, January 1, 0001
        // for a roughly increasing _requestId over restarts
        private static long _lastConnectionId = DateTime.UtcNow.Ticks;

        private readonly ServiceContext _serviceContext;
        private readonly IHttpApplication<TContext> _application;

        public ConnectionHandler(ServiceContext serviceContext, IHttpApplication<TContext> application)
        {
            _serviceContext = serviceContext;
            _application = application;
        }

        public IConnectionContext OnConnection(IConnectionInformation connectionInfo)
        {
            var inputPipe = connectionInfo.PipeFactory.Create(GetInputPipeOptions(connectionInfo.InputWriterScheduler));
            var outputPipe = connectionInfo.PipeFactory.Create(GetOutputPipeOptions(connectionInfo.OutputWriterScheduler));

            var connectionId = GenerateConnectionId(Interlocked.Increment(ref _lastConnectionId));

            var frameContext = new FrameContext
            {
                ConnectionId = connectionId,
                ConnectionInformation = connectionInfo,
                ServiceContext = _serviceContext
            };

            // TODO: Untangle this mess
            var frame = new Frame<TContext>(_application, frameContext);
            var outputProducer = new SocketOutputProducer(outputPipe.Writer, frame, connectionId, _serviceContext.Log);
            frame.LifetimeControl = new ConnectionLifetimeControl(connectionId, outputPipe.Reader, outputProducer, _serviceContext.Log);

            var connection = new FrameConnection(new FrameConnectionContext
            {
                ConnectionId = connectionId,
                ServiceContext = _serviceContext,
                PipeFactory = connectionInfo.PipeFactory,
                ConnectionAdapters = connectionInfo.ListenOptions.ConnectionAdapters,
                Frame = frame,
                Input = inputPipe,
                Output = outputPipe,
                OutputProducer = outputProducer
            });

            // Since data cannot be added to the inputPipe by the transport until OnConnection returns,
            // Frame.RequestProcessingAsync is guaranteed to unblock the transport thread before calling
            // application code.
            connection.StartRequestProcessing();

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

        private static unsafe string GenerateConnectionId(long id)
        {
            // The following routine is ~310% faster than calling long.ToString() on x64
            // and ~600% faster than calling long.ToString() on x86 in tight loops of 1 million+ iterations
            // See: https://github.com/aspnet/Hosting/pull/385

            // stackalloc to allocate array on stack rather than heap
            char* charBuffer = stackalloc char[13];

            charBuffer[0] = _encode32Chars[(int)(id >> 60) & 31];
            charBuffer[1] = _encode32Chars[(int)(id >> 55) & 31];
            charBuffer[2] = _encode32Chars[(int)(id >> 50) & 31];
            charBuffer[3] = _encode32Chars[(int)(id >> 45) & 31];
            charBuffer[4] = _encode32Chars[(int)(id >> 40) & 31];
            charBuffer[5] = _encode32Chars[(int)(id >> 35) & 31];
            charBuffer[6] = _encode32Chars[(int)(id >> 30) & 31];
            charBuffer[7] = _encode32Chars[(int)(id >> 25) & 31];
            charBuffer[8] = _encode32Chars[(int)(id >> 20) & 31];
            charBuffer[9] = _encode32Chars[(int)(id >> 15) & 31];
            charBuffer[10] = _encode32Chars[(int)(id >> 10) & 31];
            charBuffer[11] = _encode32Chars[(int)(id >> 5) & 31];
            charBuffer[12] = _encode32Chars[(int)id & 31];

            // string ctor overload that takes char*
            return new string(charBuffer, 0, 13);
        }
    }
}
