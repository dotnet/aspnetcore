// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Client.Internal;

internal abstract partial class InvocationRequest : IDisposable
{
    private readonly CancellationTokenRegistration _cancellationTokenRegistration;

    protected ILogger Logger { get; }

    public Type ResultType { get; }
    public CancellationToken CancellationToken { get; }
    public string InvocationId { get; }
    public HubConnection HubConnection { get; private set; }

    protected InvocationRequest(CancellationToken cancellationToken, Type resultType, string invocationId, ILogger logger, HubConnection hubConnection)
    {
        _cancellationTokenRegistration = cancellationToken.Register(self => ((InvocationRequest)self!).Cancel(), this);

        InvocationId = invocationId;
        CancellationToken = cancellationToken;
        ResultType = resultType;
        Logger = logger;
        HubConnection = hubConnection;

        Log.InvocationCreated(Logger, InvocationId);
    }

    public static InvocationRequest Invoke(CancellationToken cancellationToken, Type resultType, string invocationId, ILoggerFactory loggerFactory, HubConnection hubConnection, out Task<object?> result)
    {
        var req = new NonStreaming(cancellationToken, resultType, invocationId, loggerFactory, hubConnection);
        result = req.Result;
        return req;
    }

    public static InvocationRequest Stream(CancellationToken cancellationToken, Type resultType, string invocationId,
        ILoggerFactory loggerFactory, HubConnection hubConnection, out ChannelReader<object?> result)
    {
        var req = new Streaming(cancellationToken, resultType, invocationId, loggerFactory, hubConnection);
        result = req.Result;
        return req;
    }

    public abstract void Fail(Exception exception);
    public abstract void Complete(CompletionMessage message);
    public abstract ValueTask<bool> StreamItem(object? item);

    protected abstract void Cancel();

    public virtual void Dispose()
    {
        Log.InvocationDisposed(Logger, InvocationId);

        // Just in case it hasn't already been completed
        Cancel();

        _cancellationTokenRegistration.Dispose();
    }

    private sealed class Streaming : InvocationRequest
    {
        private readonly Channel<object?> _channel = Channel.CreateUnbounded<object?>();

        public Streaming(CancellationToken cancellationToken, Type resultType, string invocationId, ILoggerFactory loggerFactory, HubConnection hubConnection)
            : base(cancellationToken, resultType, invocationId, loggerFactory.CreateLogger(typeof(Streaming)), hubConnection)
        {
        }

        public ChannelReader<object?> Result => _channel.Reader;

        public override void Complete(CompletionMessage completionMessage)
        {
            Log.InvocationCompleted(Logger, InvocationId);
            if (completionMessage.Result != null)
            {
                Log.ReceivedUnexpectedComplete(Logger, InvocationId);
                _channel.Writer.TryComplete(new InvalidOperationException("Server provided a result in a completion response to a streamed invocation."));
            }

            if (!string.IsNullOrEmpty(completionMessage.Error))
            {
                Fail(new HubException(completionMessage.Error));
                return;
            }

            _channel.Writer.TryComplete();
        }

        public override void Fail(Exception exception)
        {
            Log.InvocationFailed(Logger, InvocationId);
            _channel.Writer.TryComplete(exception);
        }

        public override async ValueTask<bool> StreamItem(object? item)
        {
            try
            {
                while (!_channel.Writer.TryWrite(item))
                {
                    if (!await _channel.Writer.WaitToWriteAsync().ConfigureAwait(false))
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.ErrorWritingStreamItem(Logger, InvocationId, ex);
            }
            return true;
        }

        protected override void Cancel()
        {
            _channel.Writer.TryComplete(new OperationCanceledException());
        }
    }

    private sealed class NonStreaming : InvocationRequest
    {
        private readonly TaskCompletionSource<object?> _completionSource = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        public NonStreaming(CancellationToken cancellationToken, Type resultType, string invocationId, ILoggerFactory loggerFactory, HubConnection hubConnection)
            : base(cancellationToken, resultType, invocationId, loggerFactory.CreateLogger(typeof(NonStreaming)), hubConnection)
        {
        }

        public Task<object?> Result => _completionSource.Task;

        public override void Complete(CompletionMessage completionMessage)
        {
            if (!string.IsNullOrEmpty(completionMessage.Error))
            {
                Fail(new HubException(completionMessage.Error));
                return;
            }

            Log.InvocationCompleted(Logger, InvocationId);
            _completionSource.TrySetResult(completionMessage.Result);
        }

        public override void Fail(Exception exception)
        {
            Log.InvocationFailed(Logger, InvocationId);
            _completionSource.TrySetException(exception);
        }

        public override ValueTask<bool> StreamItem(object? item)
        {
            Log.StreamItemOnNonStreamInvocation(Logger, InvocationId);
            _completionSource.TrySetException(new InvalidOperationException($"Streaming hub methods must be invoked with the '{nameof(HubConnection)}.{nameof(HubConnectionExtensions.StreamAsChannelAsync)}' method."));

            // We "delivered" the stream item successfully as far as the caller cares
            return new ValueTask<bool>(true);
        }

        protected override void Cancel()
        {
            _completionSource.TrySetCanceled();
        }
    }

    private static partial class Log
    {
        // Category: Streaming and NonStreaming

        [LoggerMessage(1, LogLevel.Trace, "Invocation {InvocationId} created.", EventName = "InvocationCreated")]
        public static partial void InvocationCreated(ILogger logger, string invocationId);

        [LoggerMessage(2, LogLevel.Trace, "Invocation {InvocationId} disposed.", EventName = "InvocationDisposed")]
        public static partial void InvocationDisposed(ILogger logger, string invocationId);

        [LoggerMessage(3, LogLevel.Trace, "Invocation {InvocationId} marked as completed.", EventName = "InvocationCompleted")]
        public static partial void InvocationCompleted(ILogger logger, string invocationId);

        [LoggerMessage(4, LogLevel.Trace, "Invocation {InvocationId} marked as failed.", EventName = "InvocationFailed")]
        public static partial void InvocationFailed(ILogger logger, string invocationId);

        // Category: Streaming

        [LoggerMessage(5, LogLevel.Error, "Invocation {InvocationId} caused an error trying to write a stream item.", EventName = "ErrorWritingStreamItem")]
        public static partial void ErrorWritingStreamItem(ILogger logger, string invocationId, Exception exception);

        [LoggerMessage(6, LogLevel.Error, "Invocation {InvocationId} received a completion result, but was invoked as a streaming invocation.", EventName = "ReceivedUnexpectedComplete")]
        public static partial void ReceivedUnexpectedComplete(ILogger logger, string invocationId);

        // Category: NonStreaming

        [LoggerMessage(7, LogLevel.Error, "Invocation {InvocationId} received stream item but was invoked as a non-streamed invocation.", EventName = "StreamItemOnNonStreamInvocation")]
        public static partial void StreamItemOnNonStreamInvocation(ILogger logger, string invocationId);
    }
}
