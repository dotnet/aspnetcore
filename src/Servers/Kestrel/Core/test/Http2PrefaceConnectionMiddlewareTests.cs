// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Moq;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class Http2PrefaceConnectionMiddlewareTests
{
    [Theory]
    [InlineData(HttpProtocols.Http1)]
    [InlineData(HttpProtocols.Http2)]
    public void ProtocolOverrideBypassesSniffingSynchronously(HttpProtocols protocols)
    {
        var nextTask = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task;
        var serviceContext = new TestServiceContext();
        var middleware = new Http2PrefaceConnectionMiddleware(_ => nextTask, serviceContext, HttpProtocols.Http1AndHttp2);
        var connection = CreateConnection();
        connection.Features.Set(new HttpProtocolsFeature(protocols));

        var result = middleware.OnConnectionAsync(connection);

        Assert.Same(nextTask, result);
    }

    [Fact]
    public void TlsConnectionBypassesSniffingSynchronously()
    {
        var nextTask = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously).Task;
        var serviceContext = new TestServiceContext();
        var middleware = new Http2PrefaceConnectionMiddleware(_ => nextTask, serviceContext, HttpProtocols.Http1AndHttp2);
        var connection = CreateConnection();
        connection.Features.Set(Mock.Of<ITlsConnectionFeature>());

        var result = middleware.OnConnectionAsync(connection);

        Assert.Same(nextTask, result);
    }

    [Fact]
    public async Task TimeoutCancelsReadTokenAndRecordsTimeout()
    {
        var serviceContext = new TestServiceContext();
        serviceContext.ServerOptions.Limits.KeepAliveTimeout = TimeSpan.FromMilliseconds(50);
        var input = new ControllablePipeReader();
        var connection = CreateConnection(input);
        var tags = AddMetricsTagsFeature(connection);
        var middleware = new Http2PrefaceConnectionMiddleware(_ => Task.CompletedTask, serviceContext, HttpProtocols.Http1AndHttp2);

        await middleware.OnConnectionAsync(connection).WaitAsync(TimeSpan.FromSeconds(2));

        Assert.True(input.ReadCancellationRequested.Task.IsCompletedSuccessfully);
        Assert.Contains(tags, tag => tag.Key == "error.type" && (string)tag.Value == "keep_alive_timeout");
    }

    [Fact]
    public async Task ShutdownCancelsReadTokenWithoutRecordingTimeout()
    {
        using var shutdown = new CancellationTokenSource();
        var serviceContext = new TestServiceContext();
        serviceContext.ServerOptions.Limits.KeepAliveTimeout = Timeout.InfiniteTimeSpan;
        var input = new ControllablePipeReader();
        var connection = CreateConnection(input);
        var lifetimeFeature = new Mock<IConnectionLifetimeNotificationFeature>();
        lifetimeFeature.SetupGet(feature => feature.ConnectionClosedRequested).Returns(shutdown.Token);
        connection.Features.Set(lifetimeFeature.Object);
        var tags = AddMetricsTagsFeature(connection);
        var nextCalled = false;
        var middleware = new Http2PrefaceConnectionMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, serviceContext, HttpProtocols.Http1AndHttp2);

        var middlewareTask = middleware.OnConnectionAsync(connection);
        await input.ReadStarted.Task.DefaultTimeout();
        shutdown.Cancel();
        await middlewareTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.True(input.ReadCancellationRequested.Task.IsCompletedSuccessfully);
        Assert.False(nextCalled);
        Assert.Empty(tags);
    }

    [Fact]
    public async Task PreCanceledShutdownDoesNotInvokeNext()
    {
        var nextCalled = false;
        var middleware = new Http2PrefaceConnectionMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, new TestServiceContext(), HttpProtocols.Http1AndHttp2);
        var connection = CreateConnection();
        var lifetimeFeature = new Mock<IConnectionLifetimeNotificationFeature>();
        lifetimeFeature.SetupGet(feature => feature.ConnectionClosedRequested).Returns(new CancellationToken(canceled: true));
        connection.Features.Set(lifetimeFeature.Object);

        await middleware.OnConnectionAsync(connection);

        Assert.False(nextCalled);
    }

    [Fact]
    public async Task PartialPrefaceEofSelectsHttp1AndReplaysInput()
    {
        var input = "PRI *"u8.ToArray();
        var connection = CreateConnection(out var application);
        await application.Output.WriteAsync(input);
        await application.Output.CompleteAsync();
        var nextCalled = false;
        var middleware = new Http2PrefaceConnectionMiddleware(context =>
        {
            nextCalled = true;
            Assert.Equal(HttpProtocols.Http1, context.Features.Get<HttpProtocolsFeature>()?.HttpProtocols);
            return Task.CompletedTask;
        }, new TestServiceContext(), HttpProtocols.Http1AndHttp2);

        await middleware.OnConnectionAsync(connection);

        Assert.True(nextCalled);
        var result = await connection.Transport.Input.ReadAsync();
        Assert.Equal(input, result.Buffer.ToArray());
        connection.Transport.Input.AdvanceTo(result.Buffer.End);
    }

    [Fact]
    public async Task CompletePrefaceWithTrailingInputSelectsHttp2AndReplaysInput()
    {
        var input = new byte[Http2Connection.ClientPreface.Length + 2];
        Http2Connection.ClientPreface.CopyTo(input);
        input[^2] = 0x01;
        input[^1] = 0x02;
        var connection = CreateConnection(out var application);
        await application.Output.WriteAsync(input);
        var middleware = new Http2PrefaceConnectionMiddleware(context =>
        {
            Assert.Equal(HttpProtocols.Http2, context.Features.Get<HttpProtocolsFeature>()?.HttpProtocols);
            return Task.CompletedTask;
        }, new TestServiceContext(), HttpProtocols.Http1AndHttp2);

        await middleware.OnConnectionAsync(connection);

        var result = await connection.Transport.Input.ReadAsync();
        Assert.Equal(input, result.Buffer.ToArray());
        connection.Transport.Input.AdvanceTo(result.Buffer.End);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReadFailureStopsSelection(bool connectionReset)
    {
        var input = new ControllablePipeReader();
        var connection = CreateConnection(input);
        var tags = AddMetricsTagsFeature(connection);
        var nextCalled = false;
        var middleware = new Http2PrefaceConnectionMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, new TestServiceContext(), HttpProtocols.Http1AndHttp2);

        var middlewareTask = middleware.OnConnectionAsync(connection);
        await input.ReadStarted.Task.DefaultTimeout();
        input.FailRead(connectionReset ? new ConnectionResetException("reset") : new IOException("read failed"));
        await middlewareTask.DefaultTimeout();

        Assert.False(nextCalled);
        if (connectionReset)
        {
            Assert.Empty(tags);
        }
        else
        {
            Assert.Contains(tags, tag => tag.Key == "error.type" && (string)tag.Value == "io_error");
        }
    }

    [Fact]
    public async Task UnexpectedOperationCanceledExceptionIsSurfaced()
    {
        var expected = new OperationCanceledException("unexpected");
        var input = new ControllablePipeReader();
        var connection = CreateConnection(input);
        var middleware = new Http2PrefaceConnectionMiddleware(_ => Task.CompletedTask, new TestServiceContext(), HttpProtocols.Http1AndHttp2);

        var middlewareTask = middleware.OnConnectionAsync(connection);
        await input.ReadStarted.Task.DefaultTimeout();
        input.FailRead(expected);

        var actual = await Assert.ThrowsAsync<OperationCanceledException>(() => middlewareTask);
        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task AdvanceFailureIsSurfaced()
    {
        var expected = new InvalidOperationException("Advance failed.");
        var input = new ControllablePipeReader
        {
            AdvanceToCallback = () => throw expected
        };
        var connection = CreateConnection(input);
        var middleware = new Http2PrefaceConnectionMiddleware(_ => Task.CompletedTask, new TestServiceContext(), HttpProtocols.Http1AndHttp2);

        var middlewareTask = middleware.OnConnectionAsync(connection);
        await input.ReadStarted.Task.DefaultTimeout();
        input.CompleteRead(new ReadResult(new ReadOnlySequence<byte>("P"u8.ToArray()), isCanceled: false, isCompleted: false));

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => middlewareTask);
        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task InfiniteKeepAliveDoesNotCancelReadUntilShutdown()
    {
        using var shutdown = new CancellationTokenSource();
        var serviceContext = new TestServiceContext();
        serviceContext.ServerOptions.Limits.KeepAliveTimeout = Timeout.InfiniteTimeSpan;
        Assert.Equal(TimeSpan.MaxValue, serviceContext.ServerOptions.Limits.KeepAliveTimeout);
        var input = new ControllablePipeReader();
        var connection = CreateConnection(input);
        var lifetimeFeature = new Mock<IConnectionLifetimeNotificationFeature>();
        lifetimeFeature.SetupGet(feature => feature.ConnectionClosedRequested).Returns(shutdown.Token);
        connection.Features.Set(lifetimeFeature.Object);
        var middleware = new Http2PrefaceConnectionMiddleware(_ => Task.CompletedTask, serviceContext, HttpProtocols.Http1AndHttp2);
        var middlewareTask = middleware.OnConnectionAsync(connection);
        await input.ReadStarted.Task.DefaultTimeout();

        Assert.False(input.ReadCancellationRequested.Task.IsCompleted);
        shutdown.Cancel();
        await middlewareTask.DefaultTimeout();
        Assert.True(input.ReadCancellationRequested.Task.IsCompletedSuccessfully);
    }

    private static List<KeyValuePair<string, object>> AddMetricsTagsFeature(DefaultConnectionContext connection)
    {
        var tags = new List<KeyValuePair<string, object>>();
        var metricsTagsFeature = new Mock<IConnectionMetricsTagsFeature>();
        metricsTagsFeature.SetupGet(feature => feature.Tags).Returns(tags);
        connection.Features.Set(metricsTagsFeature.Object);
        return tags;
    }

    private static DefaultConnectionContext CreateConnection()
        => CreateConnection(out _);

    private static DefaultConnectionContext CreateConnection(out IDuplexPipe application)
    {
        var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
        application = pair.Application;
        return new DefaultConnectionContext("test", pair.Transport, pair.Application);
    }

    private static DefaultConnectionContext CreateConnection(PipeReader input)
    {
        var pipe = new Pipe();
        var transport = new DuplexPipe(input, pipe.Writer);
        return new DefaultConnectionContext("test", transport, transport);
    }

    private sealed class ControllablePipeReader : PipeReader
    {
        private readonly TaskCompletionSource<ReadResult> _readResult = new();
        private CancellationTokenRegistration _readCancellationRegistration;

        public TaskCompletionSource ReadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReadCancellationRequested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Action AdvanceToCallback { get; init; }

        public override void AdvanceTo(SequencePosition consumed)
        {
            AdvanceToCallback?.Invoke();
        }

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
        {
            AdvanceToCallback?.Invoke();
        }

        public override void CancelPendingRead()
        {
        }

        public override void Complete(Exception exception = null)
        {
            _readCancellationRegistration.Dispose();
        }

        public void CompleteRead(ReadResult result)
        {
            if (_readResult.TrySetResult(result))
            {
                _readCancellationRegistration.Dispose();
            }
        }

        public void FailRead(Exception exception)
        {
            if (_readResult.TrySetException(exception))
            {
                _readCancellationRegistration.Dispose();
            }
        }

        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            ReadStarted.TrySetResult();
            _readCancellationRegistration = cancellationToken.UnsafeRegister(
                static state =>
                {
                    var reader = (ControllablePipeReader)state!;
                    reader.ReadCancellationRequested.TrySetResult();
                    reader._readResult.TrySetCanceled();
                },
                this);
            return new ValueTask<ReadResult>(_readResult.Task);
        }

        public override bool TryRead(out ReadResult result)
        {
            result = default;
            return false;
        }
    }
}
