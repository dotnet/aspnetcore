// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;

namespace Microsoft.AspNetCore.Sockets.Internal
{
    /// <summary>
    /// Creates a <see cref="IChannelConnection{Message}"/> out of a <see cref="IPipelineConnection"/> by framing data
    /// read out of the Pipeline and flattening out frames to write them to the Pipeline when received.
    /// </summary>
    public class FramingChannel : IChannelConnection<Message>, IReadableChannel<Message>, IWritableChannel<Message>
    {
        private readonly IPipelineConnection _connection;
        private readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();
        private readonly Format _format;

        Task IReadableChannel<Message>.Completion => _tcs.Task;

        public IReadableChannel<Message> Input => this;
        public IWritableChannel<Message> Output => this;

        public FramingChannel(IPipelineConnection connection, Format format)
        {
            _connection = connection;
            _format = format;
        }

        ValueTask<Message> IReadableChannel<Message>.ReadAsync(CancellationToken cancellationToken)
        {
            var awaiter = _connection.Input.ReadAsync();
            if (awaiter.IsCompleted)
            {
                return new ValueTask<Message>(ReadSync(awaiter.GetResult(), cancellationToken));
            }
            else
            {
                return new ValueTask<Message>(AwaitReadAsync(awaiter, cancellationToken));
            }
        }

        private void CancelRead()
        {
            // We need to fake cancellation support until we get a newer build of pipelines that has CancelPendingRead()

            // HACK: from hell, we attempt to cast the input to a pipeline writer and write 0 bytes so it so that we can
            // force yielding the awaiter, this is buggy because overlapping writes can be a problem.
            (_connection.Input as IPipelineWriter)?.WriteAsync(Span<byte>.Empty);
        }

        bool IReadableChannel<Message>.TryRead(out Message item)
        {
            // We need to think about how we do this. There's no way to check if there is data available in a Pipeline... though maybe there should be
            // We could ReadAsync and check IsCompleted, but then we'd also need to stash that Awaitable for later since we can't call ReadAsync a second time...
            // CancelPendingReads could help here.
            item = default(Message);
            return false;
        }

        Task<bool> IReadableChannel<Message>.WaitToReadAsync(CancellationToken cancellationToken)
        {
            // See above for TryRead. Same problems here.
            throw new NotSupportedException();
        }

        Task IWritableChannel<Message>.WriteAsync(Message item, CancellationToken cancellationToken)
        {
            // Just dump the message on to the pipeline
            var buffer = _connection.Output.Alloc();
            buffer.Append(item.Payload.Buffer);
            return buffer.FlushAsync();
        }

        Task<bool> IWritableChannel<Message>.WaitToWriteAsync(CancellationToken cancellationToken)
        {
            // We need to think about how we do this. We don't have a wait to synchronously check for back-pressure in the Pipeline.
            throw new NotSupportedException();
        }

        bool IWritableChannel<Message>.TryWrite(Message item)
        {
            // We need to think about how we do this. We don't have a wait to synchronously check for back-pressure in the Pipeline.
            return false;
        }

        bool IWritableChannel<Message>.TryComplete(Exception error)
        {
            _connection.Output.Complete(error);
            _connection.Input.Complete(error);
            return true;
        }

        private async Task<Message> AwaitReadAsync(ReadableBufferAwaitable awaiter, CancellationToken cancellationToken)
        {
            using (cancellationToken.Register(state => ((FramingChannel)state).CancelRead(), this))
            {
                // Just await and then call ReadSync
                var result = await awaiter;
                return ReadSync(result, cancellationToken);
            }
        }

        private Message ReadSync(ReadResult result, CancellationToken cancellationToken)
        {
            var buffer = result.Buffer;

            // Preserve the buffer and advance the pipeline past it
            var preserved = buffer.Preserve();
            _connection.Input.Advance(buffer.End);

            var msg = new Message(preserved, _format, endOfMessage: true);

            if (result.IsCompleted)
            {
                // Complete the task
                _tcs.TrySetResult(null);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                _tcs.TrySetCanceled();

                msg.Dispose();

                // In order to keep the behavior consistent between the transports, we throw if the token was cancelled
                throw new OperationCanceledException();
            }

            return msg;
        }

        public void Dispose()
        {
            _tcs.TrySetResult(null);
            _connection.Dispose();
        }
    }
}
