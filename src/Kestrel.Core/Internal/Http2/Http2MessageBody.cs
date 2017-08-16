// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public abstract class Http2MessageBody : IMessageBody
    {
        private static readonly Http2MessageBody _emptyMessageBody = new ForEmpty();

        private readonly Http2Stream _context;

        private bool _send100Continue = true;

        protected Http2MessageBody(Http2Stream context)
        {
            _context = context;
        }

        public bool IsCompleted { get; protected set; }

        public virtual async Task OnDataAsync(ArraySegment<byte> data, bool endStream)
        {
            try
            {
                if (data.Count > 0)
                {
                    var writableBuffer = _context.RequestBodyPipe.Writer.Alloc(1);
                    bool done;

                    try
                    {
                        done = Read(data, writableBuffer);
                    }
                    finally
                    {
                        writableBuffer.Commit();
                    }

                    await writableBuffer.FlushAsync();
                }

                if (endStream)
                {
                    IsCompleted = true;
                    _context.RequestBodyPipe.Writer.Complete();
                }
            }
            catch (Exception ex)
            {
                _context.RequestBodyPipe.Writer.Complete(ex);
            }
        }

        public virtual async Task<int> ReadAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
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
                        // buffer.Count is int
                        var actual = (int)Math.Min(readableBuffer.Length, buffer.Count);
                        var slice = readableBuffer.Slice(0, actual);
                        consumed = readableBuffer.Move(readableBuffer.Start, actual);
                        slice.CopyTo(buffer);
                        return actual;
                    }
                    else if (result.IsCompleted)
                    {
                        return 0;
                    }
                }
                finally
                {
                    _context.RequestBodyPipe.Reader.Advance(consumed);
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
                            var array = memory.GetArray();
                            await destination.WriteAsync(array.Array, array.Offset, array.Count, cancellationToken);
                        }
                    }
                    else if (result.IsCompleted)
                    {
                        return;
                    }
                }
                finally
                {
                    _context.RequestBodyPipe.Reader.Advance(consumed);
                }
            }
        }

        public virtual Task StopAsync()
        {
            _context.RequestBodyPipe.Reader.Complete();
            _context.RequestBodyPipe.Writer.Complete();
            return Task.CompletedTask;
        }

        protected void Copy(Span<byte> data, WritableBuffer writableBuffer)
        {
            writableBuffer.Write(data);
        }

        private void TryProduceContinue()
        {
            if (_send100Continue)
            {
                _context.HttpStreamControl.ProduceContinue();
                _send100Continue = false;
            }
        }

        private void TryInit()
        {
            if (!_context.HasStartedConsumingRequestBody)
            {
                OnReadStart();
                _context.HasStartedConsumingRequestBody = true;
            }
        }

        protected virtual bool Read(Span<byte> readableBuffer, WritableBuffer writableBuffer)
        {
            throw new NotImplementedException();
        }

        protected virtual void OnReadStart()
        {
        }

        public static Http2MessageBody For(
            FrameRequestHeaders headers,
            Http2Stream context)
        {
            if (!context.ExpectBody)
            {
                return _emptyMessageBody;
            }

            if (headers.ContentLength.HasValue)
            {
                var contentLength = headers.ContentLength.Value;

                return new ForContentLength(contentLength, context);
            }

            return new ForRemainingData(context);
        }

        private class ForEmpty : Http2MessageBody
        {
            public ForEmpty()
                : base(context: null)
            {
                IsCompleted = true;
            }

            public override Task OnDataAsync(ArraySegment<byte> data, bool endStream)
            {
                throw new NotImplementedException();
            }

            public override Task<int> ReadAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
            {
                return Task.FromResult(0);
            }

            public override Task CopyToAsync(Stream destination, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }

        private class ForRemainingData : Http2MessageBody
        {
            public ForRemainingData(Http2Stream context)
                : base(context)
            {
            }

            protected override bool Read(Span<byte> data, WritableBuffer writableBuffer)
            {
                Copy(data, writableBuffer);
                return false;
            }
        }

        private class ForContentLength : Http2MessageBody
        {
            private readonly long _contentLength;
            private long _inputLength;

            public ForContentLength(long contentLength, Http2Stream context)
                : base(context)
            {
                _contentLength = contentLength;
                _inputLength = _contentLength;
            }

            protected override bool Read(Span<byte> data, WritableBuffer writableBuffer)
            {
                if (_inputLength == 0)
                {
                    throw new InvalidOperationException("Attempted to read from completed Content-Length request body.");
                }

                if (data.Length > _inputLength)
                {
                    _context.ThrowRequestRejected(RequestRejectionReason.RequestBodyExceedsContentLength);
                }

                _inputLength -= data.Length;

                Copy(data, writableBuffer);

                return _inputLength == 0;
            }

            protected override void OnReadStart()
            {
                if (_contentLength > _context.MaxRequestBodySize)
                {
                    _context.ThrowRequestRejected(RequestRejectionReason.RequestBodyTooLarge);
                }
            }
        }
    }
}
