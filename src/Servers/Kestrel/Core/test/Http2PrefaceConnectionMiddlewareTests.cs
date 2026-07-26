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
    public async Task PartialPrefaceEofDisposesTimerBeforeNextAndReplaysInput()
    {
        var input = "PRI *"u8.ToArray();
        var timeProvider = new TrackingTimeProvider();
        var serviceContext = new TestServiceContext
        {
            TimeProvider = timeProvider
        };
        var connection = CreateConnection(out var application);
        await application.Output.WriteAsync(input);
        await application.Output.CompleteAsync();
        var nextCalled = false;
        var middleware = new Http2PrefaceConnectionMiddleware(context =>
        {
            nextCalled = true;
            Assert.True(timeProvider.Timer.Disposed);
            Assert.Equal(HttpProtocols.Http1, context.Features.Get<HttpProtocolsFeature>()?.HttpProtocols);
            return Task.CompletedTask;
        }, serviceContext, HttpProtocols.Http1AndHttp2);

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

    [Fact]
    public async Task TimerCallbackFailureIsSurfacedByMiddlewareTask()
    {
        var expected = new InvalidOperationException("Timer change failed.");
        var timeProvider = new TrackingTimeProvider(expected);
        var serviceContext = new TestServiceContext
        {
            TimeProvider = timeProvider
        };
        var connection = CreateConnection();
        var middleware = new Http2PrefaceConnectionMiddleware(_ => Task.CompletedTask, serviceContext, HttpProtocols.Http1AndHttp2);

        var middlewareTask = middleware.OnConnectionAsync(connection);
        timeProvider.Timer.Fire();

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => middlewareTask);
        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task TimerCallbackBeforePublicationIsRearmed()
    {
        var timeProvider = new TrackingTimeProvider(fireDuringCreate: true);
        var serviceContext = new TestServiceContext
        {
            TimeProvider = timeProvider
        };
        var connection = CreateConnection(out var application);
        var middleware = new Http2PrefaceConnectionMiddleware(_ => Task.CompletedTask, serviceContext, HttpProtocols.Http1AndHttp2);

        var middlewareTask = middleware.OnConnectionAsync(connection);
        await application.Output.WriteAsync("G"u8.ToArray());
        await middlewareTask;

        Assert.Equal(1, timeProvider.Timer.ChangeCount);
    }

    private static DefaultConnectionContext CreateConnection()
        => CreateConnection(out _);

    private static DefaultConnectionContext CreateConnection(out IDuplexPipe application)
    {
        var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
        application = pair.Application;
        return new DefaultConnectionContext("test", pair.Transport, pair.Application);
    }

    private sealed class TrackingTimeProvider : TimeProvider
    {
        private readonly Exception _changeException;
        private readonly bool _fireDuringCreate;

        public TrackingTimeProvider(Exception changeException = null, bool fireDuringCreate = false)
        {
            _changeException = changeException;
            _fireDuringCreate = fireDuringCreate;
        }

        public TrackingTimer Timer { get; private set; }

        public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
        {
            Timer = new TrackingTimer(callback, state, _changeException);
            if (_fireDuringCreate)
            {
                Timer.Fire();
            }
            return Timer;
        }
    }

    private sealed class TrackingTimer : ITimer
    {
        private readonly TimerCallback _callback;
        private readonly object _state;
        private readonly Exception _changeException;

        public TrackingTimer(TimerCallback callback, object state, Exception changeException)
        {
            _callback = callback;
            _state = state;
            _changeException = changeException;
        }

        public bool Disposed { get; private set; }

        public int ChangeCount { get; private set; }

        public void Fire() => _callback(_state);

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            ChangeCount++;
            if (_changeException is not null)
            {
                throw _changeException;
            }

            return true;
        }

        public void Dispose() => Disposed = true;

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
