// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal class HttpResponseStream : WriteOnlyStream
    {
        private readonly IHttpBodyControlFeature _bodyControl;
        private readonly IHttpResponseControl _httpResponseControl;
        private HttpStreamState _state;

        public HttpResponseStream(IHttpBodyControlFeature bodyControl, IHttpResponseControl httpResponseControl)
        {
            _bodyControl = bodyControl;
            _httpResponseControl = httpResponseControl;
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
            FlushAsync(default(CancellationToken)).GetAwaiter().GetResult();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            ValidateState(cancellationToken);

            return _httpResponseControl.FlushAsync(cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!_bodyControl.AllowSynchronousIO)
            {
                throw new InvalidOperationException(CoreStrings.SynchronousWritesDisallowed);
            }

            WriteAsync(buffer, offset, count, default(CancellationToken)).GetAwaiter().GetResult();
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            var task = WriteAsync(buffer, offset, count, default(CancellationToken), state);
            if (callback != null)
            {
                task.ContinueWith(t => callback.Invoke(t));
            }
            return task;
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            ((Task<object>)asyncResult).GetAwaiter().GetResult();
        }

        private Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken, object state)
        {
            var tcs = new TaskCompletionSource<object>(state);
            var task = WriteAsync(buffer, offset, count, cancellationToken);
            task.ContinueWith((task2, state2) =>
            {
                var tcs2 = (TaskCompletionSource<object>)state2;
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
                    tcs2.SetResult(null);
                }
            }, tcs, cancellationToken);
            return tcs.Task;
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateState(cancellationToken);

            return _httpResponseControl.WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        {
            ValidateState(cancellationToken);

            return new ValueTask(_httpResponseControl.WriteAsync(source, cancellationToken));
        }

        public void StartAcceptingWrites()
        {
            // Only start if not aborted
            if (_state == HttpStreamState.Closed)
            {
                _state = HttpStreamState.Open;
            }
        }

        public void StopAcceptingWrites()
        {
            // Can't use dispose (or close) as can be disposed too early by user code
            // As exampled in EngineTests.ZeroContentLengthNotSetAutomaticallyForCertainStatusCodes
            _state = HttpStreamState.Closed;
        }

        public void Abort()
        {
            // We don't want to throw an ODE until the app func actually completes.
            if (_state != HttpStreamState.Closed)
            {
                _state = HttpStreamState.Aborted;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateState(CancellationToken cancellationToken)
        {
            var state = _state;
            if (state == HttpStreamState.Open || state == HttpStreamState.Aborted)
            {
                // Aborted state only throws on write if cancellationToken requests it
                cancellationToken.ThrowIfCancellationRequested();
            }
            else
            {
                ThrowObjectDisposedException();
            }

            void ThrowObjectDisposedException()
            {
                throw new ObjectDisposedException(nameof(HttpResponseStream), CoreStrings.WritingToResponseBodyAfterResponseCompleted);
            }
        }
    }
}
