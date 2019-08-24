// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    /// <summary>
    /// Default HttpRequest PipeReader implementation to be used by Kestrel.
    /// </summary>
    internal sealed class HttpRequestPipeReader : PipeReader
    {
        private MessageBody _body;
        private HttpStreamState _state;
        private ExceptionDispatchInfo _error;

        public HttpRequestPipeReader()
        {
            _state = HttpStreamState.Closed;
        }

        public override void AdvanceTo(SequencePosition consumed)
        {
            ValidateState();

            _body.AdvanceTo(consumed);
        }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            ValidateState();

            _body.AdvanceTo(consumed, examined);
        }

        public override void CancelPendingRead()
        {
            ValidateState();

            _body.CancelPendingRead();
        }

        public override void Complete(Exception exception = null)
        {
            ValidateState();

            _body.Complete(exception);
        }

        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            ValidateState(cancellationToken);

            return _body.ReadAsync(cancellationToken);
        }

        public override bool TryRead(out ReadResult result)
        {
            ValidateState();

            return _body.TryRead(out result);
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
                if (error != null)
                {
                    _error = ExceptionDispatchInfo.Capture(error);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateState(CancellationToken cancellationToken = default)
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
                    _error.Throw();
                }
                else
                {
                    ThrowTaskCanceledException();
                }
            }

            static void ThrowObjectDisposedException() => throw new ObjectDisposedException(nameof(HttpRequestStream));
            static void ThrowTaskCanceledException() => throw new TaskCanceledException();
        }
    }
}
