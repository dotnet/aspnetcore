// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Abstractions;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    internal sealed class Http2Stream<TContext> : Http2Stream, IHostContextContainer<TContext> where TContext : notnull
    {
        private readonly Task _executingTask;

        private readonly Channel<ReadResult> _requestQueue = Channel.CreateUnbounded<ReadResult>(new UnboundedChannelOptions
        {
            // We want to run continuations inline
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = true
        });

        public Http2Stream(IHttpApplication<TContext> application, Http2StreamContext context)
        {
            Initialize(context);

            _executingTask = ProcessRequestsAsync(application);
        }

        public override void Execute()
        {
            KestrelEventSource.Log.RequestQueuedStop(this, AspNetCore.Http.HttpProtocol.Http2);

            _requestQueue.Writer.TryWrite(new ReadResult(default, isCanceled: false, isCompleted: false));
        }

        // Pooled Host context
        TContext? IHostContextContainer<TContext>.HostContext { get; set; }

        protected override bool BeginRead(out ValueTask<ReadResult> awaitable)
        {
            awaitable = _requestQueue.Reader.ReadAsync();
            return true;
        }

        protected override bool TryParseRequest(ReadResult result, out bool endConnection)
        {
            if (result.IsCompleted)
            {
                endConnection = true;
                return true;
            }

            return base.TryParseRequest(result, out endConnection);
        }

        protected override bool EndRequestProcessing()
        {
            base.EndRequestProcessing();
            return _requestQueue.Reader.Completion.IsCompleted;
        }

        public override void Dispose()
        {
            _requestQueue.Writer.TryWrite(new ReadResult(default, isCanceled: false, isCompleted: true));
            _requestQueue.Writer.TryComplete();
            base.Dispose();
        }
    }
}
