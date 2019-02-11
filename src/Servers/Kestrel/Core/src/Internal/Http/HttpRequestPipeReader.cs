// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal class HttpRequestPipeReader : PipeReader
    {
        private MessageBody _body;
        private HttpStreamState _state;
        private Exception _error;

        // All of these will just call into MessageBody
        public HttpRequestPipeReader()
        {
            _state = HttpStreamState.Closed;
        }

        private HttpProtocol httpProtocol;

        public HttpRequestPipeReader(HttpProtocol httpProtocol)
        {
            this.httpProtocol = httpProtocol;
        }

        public override void AdvanceTo(SequencePosition consumed)
        {
            _body.AdvanceTo(consumed);
        }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            _body.AdvanceTo(consumed, examined);
        }

        public override void CancelPendingRead()
        {
            throw new NotImplementedException();
        }

        public override void Complete(Exception exception = null)
        {
            throw new NotImplementedException();
        }

        public override void OnWriterCompleted(Action<Exception, object> callback, object state)
        {
            throw new NotImplementedException();
        }

        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override bool TryRead(out ReadResult result)
        {
            throw new NotImplementedException();
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
