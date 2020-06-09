// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Server.IIS.Core
{
    internal class HttpRequestStream : ReadOnlyStream
    {
        private readonly IHttpBodyControlFeature _bodyControl;
        private IISHttpContext _body;
        private HttpStreamState _state;
        private Exception _error;

        public HttpRequestStream(IHttpBodyControlFeature bodyControl)
        {
            _bodyControl = bodyControl;
            _state = HttpStreamState.Closed;
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
            return TaskToApm.Begin(ReadAsync(buffer, offset, count), callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return TaskToApm.End<int>(asyncResult);
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

        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
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

        public void StartAcceptingReads(IISHttpContext body)
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

        private void ValidateState(CancellationToken cancellationToken)
        {
            switch (_state)
            {
                case HttpStreamState.Open:
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    break;
                case HttpStreamState.Closed:
                    throw new ObjectDisposedException(nameof(HttpRequestStream));
                case HttpStreamState.Aborted:
                    if (_error != null)
                    {
                        ExceptionDispatchInfo.Capture(_error).Throw();
                    }
                    else
                    {
                        throw new TaskCanceledException();
                    }
                    break;
            }
        }
    }
}
