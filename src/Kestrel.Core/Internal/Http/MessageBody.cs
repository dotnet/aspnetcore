// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public abstract class MessageBody
    {
        private static readonly MessageBody _zeroContentLengthClose = new ForZeroContentLength(keepAlive: false);
        private static readonly MessageBody _zeroContentLengthKeepAlive = new ForZeroContentLength(keepAlive: true);

        private readonly HttpProtocol _context;

        private bool _send100Continue = true;

        protected MessageBody(HttpProtocol context)
        {
            _context = context;
        }

        public static MessageBody ZeroContentLengthClose => _zeroContentLengthClose;

        public static MessageBody ZeroContentLengthKeepAlive => _zeroContentLengthKeepAlive;

        public bool RequestKeepAlive { get; protected set; }

        public bool RequestUpgrade { get; protected set; }

        public virtual bool IsEmpty => false;

        protected IKestrelTrace Log => _context.ServiceContext.Log;

        public virtual async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
        {
            TryInit();

            while (true)
            {
                var result = await _context.RequestBodyPipe.Reader.ReadAsync();
                var readableBuffer = result.Buffer;
                var consumed = readableBuffer.End;

                try
                {
                    if (!readableBuffer.IsEmpty)
                    {
                        //  buffer.Count is int
                        var actual = (int)Math.Min(readableBuffer.Length, buffer.Length);
                        var slice = readableBuffer.Slice(0, actual);
                        consumed = readableBuffer.GetPosition(actual);
                        slice.CopyTo(buffer.Span);
                        return actual;
                    }
                    else if (result.IsCompleted)
                    {
                        return 0;
                    }
                }
                finally
                {
                    _context.RequestBodyPipe.Reader.AdvanceTo(consumed);
                }
            }
        }

        public virtual async Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default(CancellationToken))
        {
            TryInit();

            while (true)
            {
                var result = await _context.RequestBodyPipe.Reader.ReadAsync();
                var readableBuffer = result.Buffer;
                var consumed = readableBuffer.End;

                try
                {
                    if (!readableBuffer.IsEmpty)
                    {
                        foreach (var memory in readableBuffer)
                        {
                            // REVIEW: This *could* be slower if 2 things are true
                            // - The WriteAsync(ReadOnlyMemory<byte>) isn't overridden on the destination
                            // - We change the Kestrel Memory Pool to not use pinned arrays but instead use native memory
#if NETCOREAPP2_1
                            await destination.WriteAsync(memory);
#elif NETSTANDARD2_0
                            var array = memory.GetArray();
                            await destination.WriteAsync(array.Array, array.Offset, array.Count, cancellationToken);
#else
#error TFMs need to be updated
#endif
                        }
                    }
                    else if (result.IsCompleted)
                    {
                        return;
                    }
                }
                finally
                {
                    _context.RequestBodyPipe.Reader.AdvanceTo(consumed);
                }
            }
        }

        public virtual Task ConsumeAsync()
        {
            TryInit();

            return OnConsumeAsync();
        }

        protected abstract Task OnConsumeAsync();

        public abstract Task StopAsync();

        protected void TryProduceContinue()
        {
            if (_send100Continue)
            {
                _context.HttpResponseControl.ProduceContinue();
                _send100Continue = false;
            }
        }

        private void TryInit()
        {
            if (!_context.HasStartedConsumingRequestBody)
            {
                OnReadStarting();
                _context.HasStartedConsumingRequestBody = true;
                OnReadStarted();
            }
        }

        protected virtual void OnReadStarting()
        {
        }

        protected virtual void OnReadStarted()
        {
        }

        private class ForZeroContentLength : MessageBody
        {
            public ForZeroContentLength(bool keepAlive)
                : base(null)
            {
                RequestKeepAlive = keepAlive;
            }

            public override bool IsEmpty => true;

            public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken)) => new ValueTask<int>(0);

            public override Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default(CancellationToken)) => Task.CompletedTask;

            public override Task ConsumeAsync() => Task.CompletedTask;

            public override Task StopAsync() => Task.CompletedTask;

            protected override Task OnConsumeAsync() => Task.CompletedTask;
        }
    }
}
