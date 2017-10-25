// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;
using Microsoft.AspNetCore.SignalR.Client.Internal;
using Microsoft.AspNetCore.SignalR.Internal.Protocol;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Client
{
    internal abstract class InvocationRequest : IDisposable
    {
        private readonly CancellationTokenRegistration _cancellationTokenRegistration;

        protected ILogger Logger { get; }

        public Type ResultType { get; }
        public CancellationToken CancellationToken { get; }
        public string InvocationId { get; }
        public HubConnection HubConnection { get; private set; }

        protected InvocationRequest(CancellationToken cancellationToken, Type resultType, string invocationId, ILogger logger, HubConnection hubConnection)
        {
            _cancellationTokenRegistration = cancellationToken.Register(self => ((InvocationRequest)self).Cancel(), this);

            InvocationId = invocationId;
            CancellationToken = cancellationToken;
            ResultType = resultType;
            Logger = logger;
            HubConnection = hubConnection;

            Logger.InvocationCreated(InvocationId);
        }

        public static InvocationRequest Invoke(CancellationToken cancellationToken, Type resultType, string invocationId, ILoggerFactory loggerFactory, HubConnection hubConnection, out Task<object> result)
        {
            var req = new NonStreaming(cancellationToken, resultType, invocationId, loggerFactory, hubConnection);
            result = req.Result;
            return req;
        }

        public static InvocationRequest Stream(CancellationToken cancellationToken, Type resultType, string invocationId,
            ILoggerFactory loggerFactory, HubConnection hubConnection, out ReadableChannel<object> result)
        {
            var req = new Streaming(cancellationToken, resultType, invocationId, loggerFactory, hubConnection);
            result = req.Result;
            return req;
        }

        public abstract void Fail(Exception exception);
        public abstract void Complete(CompletionMessage message);
        public abstract ValueTask<bool> StreamItem(object item);

        protected abstract void Cancel();

        public virtual void Dispose()
        {
            Logger.InvocationDisposed(InvocationId);

            // Just in case it hasn't already been completed
            Cancel();

            _cancellationTokenRegistration.Dispose();
        }

        private class Streaming : InvocationRequest
        {
            private readonly Channel<object> _channel = Channel.CreateUnbounded<object>();

            public Streaming(CancellationToken cancellationToken, Type resultType, string invocationId, ILoggerFactory loggerFactory, HubConnection hubConnection)
                : base(cancellationToken, resultType, invocationId, loggerFactory.CreateLogger<Streaming>(), hubConnection)
            {
            }

            public ReadableChannel<object> Result => _channel.In;

            public override void Complete(CompletionMessage completionMessage)
            {
                Logger.InvocationCompleted(InvocationId);
                if (completionMessage.Result != null)
                {
                    Logger.ReceivedUnexpectedComplete(InvocationId);
                    _channel.Out.TryComplete(new InvalidOperationException("Server provided a result in a completion response to a streamed invocation."));
                }

                if (!string.IsNullOrEmpty(completionMessage.Error))
                {
                    Fail(new HubException(completionMessage.Error));
                    return;
                }

                _channel.Out.TryComplete();
            }

            public override void Fail(Exception exception)
            {
                Logger.InvocationFailed(InvocationId);
                _channel.Out.TryComplete(exception);
            }

            public override async ValueTask<bool> StreamItem(object item)
            {
                try
                {
                    while (!_channel.Out.TryWrite(item))
                    {
                        if (!await _channel.Out.WaitToWriteAsync())
                        {
                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorWritingStreamItem(InvocationId, ex);
                }
                return true;
            }

            protected override void Cancel()
            {
                _channel.Out.TryComplete(new OperationCanceledException("Invocation terminated"));
            }
        }

        private class NonStreaming : InvocationRequest
        {
            private readonly TaskCompletionSource<object> _completionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            public NonStreaming(CancellationToken cancellationToken, Type resultType, string invocationId, ILoggerFactory loggerFactory, HubConnection hubConnection)
                : base(cancellationToken, resultType, invocationId, loggerFactory.CreateLogger<NonStreaming>(), hubConnection)
            {
            }

            public Task<object> Result => _completionSource.Task;

            public override void Complete(CompletionMessage completionMessage)
            {
                if (!string.IsNullOrEmpty(completionMessage.Error))
                {
                    Fail(new HubException(completionMessage.Error));
                    return;
                }

                Logger.InvocationCompleted(InvocationId);
                _completionSource.TrySetResult(completionMessage.Result);
            }

            public override void Fail(Exception exception)
            {
                Logger.InvocationFailed(InvocationId);
                _completionSource.TrySetException(exception);
            }

            public override ValueTask<bool> StreamItem(object item)
            {
                Logger.StreamItemOnNonStreamInvocation(InvocationId);
                _completionSource.TrySetException(new InvalidOperationException($"Streaming hub methods must be invoked with the '{nameof(HubConnection)}.{nameof(HubConnection.StreamAsync)}' method."));

                // We "delivered" the stream item successfully as far as the caller cares
                return new ValueTask<bool>(true);
            }

            protected override void Cancel()
            {
                _completionSource.TrySetCanceled();
            }
        }
    }
}
