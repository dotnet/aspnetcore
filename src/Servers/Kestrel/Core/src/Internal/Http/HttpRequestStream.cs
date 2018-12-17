// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal class HttpRequestStream : ReadOnlyStream
    {
        private readonly IHttpBodyControlFeature _bodyControl;
        private MessageBody _body;
        private HttpStreamState _state;
        private Exception _error;

        public HttpRequestStream(IHttpBodyControlFeature bodyControl)
        {
            _bodyControl = bodyControl;
            _state = HttpStreamState.Closed;
        }

        public override bool CanSeek => false;

        public override long Length
            => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!_bodyControl.AllowSynchronousIO)
            {
                throw new InvalidOperationException(CoreStrings.SynchronousReadsDisallowed);
            }

            return ReadAsync(buffer, offset, count).GetAwaiter().GetResult();
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            var task = ReadAsync(buffer, offset, count, default(CancellationToken), state);
            if (callback != null)
            {
                task.ContinueWith(t => callback.Invoke(t));
            }
            return task;
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return ((Task<int>)asyncResult).GetAwaiter().GetResult();
        }

        private Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken, object state)
        {
            var tcs = new TaskCompletionSource<int>(state);
            var task = ReadAsync(buffer, offset, count, cancellationToken);
            task.ContinueWith((task2, state2) =>
            {
                var tcs2 = (TaskCompletionSource<int>)state2;
                if (task2.IsCanceled)
                {
                    tcs2.SetCanceled();
                }
                else if (task2.IsFaulted)
                {
                    tcs2.SetException(task2.Exception);
                }
                else
                {
                    tcs2.SetResult(task2.Result);
                }
            }, tcs, cancellationToken);
            return tcs.Task;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateState(cancellationToken);

            return ReadAsyncInternal(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            ValidateState(cancellationToken);

            return ReadAsyncInternal(destination, cancellationToken);
        }

        private async ValueTask<int> ReadAsyncInternal(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            try
            {
                return await _body.ReadAsync(buffer, cancellationToken);
            }
            catch (ConnectionAbortedException ex)
            {
                throw new TaskCanceledException("The request was aborted", ex);
            }
        }

        public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }
            if (bufferSize <= 0)
            {
                throw new ArgumentException(CoreStrings.PositiveNumberRequired, nameof(bufferSize));
            }

            ValidateState(cancellationToken);

            return CopyToAsyncInternal(destination, cancellationToken);
        }

        private async Task CopyToAsyncInternal(Stream destination, CancellationToken cancellationToken)
        {
            try
            {
                await _body.CopyToAsync(destination, cancellationToken);
            }
            catch (ConnectionAbortedException ex)
            {
                throw new TaskCanceledException("The request was aborted", ex);
            }
        }

        public void StartAcceptingReads(MessageBody body)
        {
            // Only start if not aborted
            if (_state == HttpStreamState.Closed)
            {
                _state = HttpStreamState.Open;
                _body = body;
            }
        }

        public void StopAcceptingReads()
        {
            // Can't use dispose (or close) as can be disposed too early by user code
            // As exampled in EngineTests.ZeroContentLengthNotSetAutomaticallyForCertainStatusCodes
            _state = HttpStreamState.Closed;
            _body = null;
        }

        public void Abort(Exception error = null)
        {
            // We don't want to throw an ODE until the app func actually completes.
            // If the request is aborted, we throw a TaskCanceledException instead,
            // unless error is not null, in which case we throw it.
            if (_state != HttpStreamState.Closed)
            {
                _state = HttpStreamState.Aborted;
                _error = error;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateState(CancellationToken cancellationToken)
        {
            var state = _state;
            if (state == HttpStreamState.Open)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            else if (state == HttpStreamState.Closed)
            {
                ThrowObjectDisposedException();
            }
            else
            {
                if (_error != null)
                {
                    ExceptionDispatchInfo.Capture(_error).Throw();
                }
                else
                {
                    ThrowTaskCanceledException();
                }
            }

            void ThrowObjectDisposedException() => throw new ObjectDisposedException(nameof(HttpRequestStream));
            void ThrowTaskCanceledException() => throw new TaskCanceledException();
        }
    }
}
