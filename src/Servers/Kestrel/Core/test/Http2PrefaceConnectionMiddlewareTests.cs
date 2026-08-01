// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
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

    [Fact]
    public async Task InFlightTimerCallbackCompletesBeforeNext()
    {
        var debugger = new BlockingDebugger();
        var timeProvider = new TrackingTimeProvider();
        var serviceContext = new TestServiceContext
        {
            TimeProvider = timeProvider
        };
        var input = new ControllablePipeReader();
        var connection = CreateConnection(input);
        var nextCalled = false;
        var middleware = new Http2PrefaceConnectionMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            serviceContext,
            HttpProtocols.Http1AndHttp2,
            debugger);

        var middlewareTask = middleware.OnConnectionAsync(connection);
        await input.ReadStarted.Task.DefaultTimeout();
        var fireTask = Task.Run(timeProvider.Timer.Fire);
        await debugger.Entered.Task.DefaultTimeout();
        input.CompleteRead(new ReadResult(new ReadOnlySequence<byte>("G"u8.ToArray()), isCanceled: false, isCompleted: false));
        await timeProvider.Timer.DisposeCalled.Task.DefaultTimeout();

        Assert.False(nextCalled);
        Assert.False(middlewareTask.IsCompleted);

        debugger.Release.TrySetResult();
        await fireTask.DefaultTimeout();
        await middlewareTask.DefaultTimeout();
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task QueuedTimerCallbackDoesNotRetainConnectionAfterSelection()
    {
        var scenario = await CreateQueuedTimerScenario();
        try
        {
            Assert.True(scenario.TimeProvider.Timer.Disposed);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Assert.False(scenario.Connection.IsAlive);
        }
        finally
        {
            scenario.Release.TrySetResult();
            await scenario.FireTask.DefaultTimeout();
            GC.KeepAlive(scenario.TimeProvider);
        }
    }

    [Fact]
    public async Task TimeoutWinningEmptyCompletedReadRecordsTimeout()
    {
        var timeProvider = new TrackingTimeProvider();
        var serviceContext = new TestServiceContext
        {
            TimeProvider = timeProvider
        };
        serviceContext.ServerOptions.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(1);
        var input = new ControllablePipeReader();
        var connection = CreateConnection(input);
        var tags = AddMetricsTagsFeature(connection);
        var middleware = new Http2PrefaceConnectionMiddleware(_ => Task.CompletedTask, serviceContext, HttpProtocols.Http1AndHttp2);

        var middlewareTask = middleware.OnConnectionAsync(connection);
        await input.ReadStarted.Task.DefaultTimeout();
        timeProvider.Advance(serviceContext.ServerOptions.Limits.KeepAliveTimeout);
        timeProvider.Timer.Fire();
        await input.CancelCalled.Task.DefaultTimeout();
        input.CompleteRead(new ReadResult(default, isCanceled: false, isCompleted: true));
        await middlewareTask.DefaultTimeout();

        Assert.Contains(tags, tag => tag.Key == "error.type" && (string)tag.Value == "keep_alive_timeout");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TimeoutWinningReadFailureRecordsTimeout(bool connectionReset)
    {
        var timeProvider = new TrackingTimeProvider();
        var serviceContext = new TestServiceContext
        {
            TimeProvider = timeProvider
        };
        serviceContext.ServerOptions.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(1);
        var input = new ControllablePipeReader();
        var connection = CreateConnection(input);
        var tags = AddMetricsTagsFeature(connection);
        var middleware = new Http2PrefaceConnectionMiddleware(_ => Task.CompletedTask, serviceContext, HttpProtocols.Http1AndHttp2);

        var middlewareTask = middleware.OnConnectionAsync(connection);
        await input.ReadStarted.Task.DefaultTimeout();
        timeProvider.Advance(serviceContext.ServerOptions.Limits.KeepAliveTimeout);
        timeProvider.Timer.Fire();
        await input.CancelCalled.Task.DefaultTimeout();
        input.FailRead(connectionReset ? new ConnectionResetException("reset") : new IOException("read failed"));
        await middlewareTask.DefaultTimeout();

        Assert.Contains(tags, tag => tag.Key == "error.type" && (string)tag.Value == "keep_alive_timeout");
        Assert.DoesNotContain(tags, tag => tag.Key == "error.type" && (string)tag.Value == "io_error");
    }

    [Fact]
    public async Task ShutdownWinningReadFailureDoesNotRecordIoError()
    {
        using var shutdown = new CancellationTokenSource();
        var input = new ControllablePipeReader();
        var connection = CreateConnection(input);
        var lifetimeFeature = new Mock<IConnectionLifetimeNotificationFeature>();
        lifetimeFeature.SetupGet(feature => feature.ConnectionClosedRequested).Returns(shutdown.Token);
        connection.Features.Set(lifetimeFeature.Object);
        var tags = AddMetricsTagsFeature(connection);
        var middleware = new Http2PrefaceConnectionMiddleware(
            _ => Task.CompletedTask,
            new TestServiceContext(),
            HttpProtocols.Http1AndHttp2);

        var middlewareTask = middleware.OnConnectionAsync(connection);
        await input.ReadStarted.Task.DefaultTimeout();
        shutdown.Cancel();
        await input.CancelCalled.Task.DefaultTimeout();
        input.FailRead(new IOException("read failed"));
        await middlewareTask.DefaultTimeout();

        Assert.Empty(tags);
    }

    [Fact]
    public async Task CancelPendingReadFailureIsPublishedBeforeTimeoutCompletes()
    {
        var expected = new InvalidOperationException("Cancel failed.");
        var cancelEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCancel = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var timeProvider = new TrackingTimeProvider();
        var serviceContext = new TestServiceContext
        {
            TimeProvider = timeProvider
        };
        serviceContext.ServerOptions.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(1);
        var input = new ControllablePipeReader
        {
            CancelPendingReadCallback = () =>
            {
                cancelEntered.TrySetResult();
                releaseCancel.Task.GetAwaiter().GetResult();
                throw expected;
            }
        };
        var connection = CreateConnection(input);
        var middleware = new Http2PrefaceConnectionMiddleware(_ => Task.CompletedTask, serviceContext, HttpProtocols.Http1AndHttp2);

        var middlewareTask = Task.Run(() => middleware.OnConnectionAsync(connection));
        await input.ReadStarted.Task.DefaultTimeout();
        timeProvider.Advance(serviceContext.ServerOptions.Limits.KeepAliveTimeout);
        var fireTask = Task.Run(timeProvider.Timer.Fire);
        await cancelEntered.Task.DefaultTimeout();
        input.CompleteRead(new ReadResult(new ReadOnlySequence<byte>("P"u8.ToArray()), isCanceled: false, isCompleted: false));

        try
        {
            Assert.False(middlewareTask.IsCompleted);
        }
        finally
        {
            releaseCancel.TrySetResult();
        }

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => middlewareTask);
        Assert.Same(expected, actual);
        await fireTask.DefaultTimeout();
    }

    [Fact]
    public async Task CancelPendingReadFailureCompletesWithoutReadWakeUp()
    {
        var expected = new InvalidOperationException("Cancel failed.");
        var timeProvider = new TrackingTimeProvider();
        var serviceContext = new TestServiceContext
        {
            TimeProvider = timeProvider
        };
        serviceContext.ServerOptions.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(1);
        var input = new ControllablePipeReader
        {
            CancelPendingReadCallback = () => throw expected
        };
        var connection = CreateConnection(input);
        var middleware = new Http2PrefaceConnectionMiddleware(_ => Task.CompletedTask, serviceContext, HttpProtocols.Http1AndHttp2);

        var middlewareTask = middleware.OnConnectionAsync(connection);
        await input.ReadStarted.Task.DefaultTimeout();
        timeProvider.Advance(serviceContext.ServerOptions.Limits.KeepAliveTimeout);
        timeProvider.Timer.Fire();

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => middlewareTask.WaitAsync(TimeSpan.FromSeconds(1)));
        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task TimeoutWinningAdvanceFailureRecordsTimeout()
    {
        var advanceException = new InvalidOperationException("Advance failed.");
        var cancelEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCancel = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var timeProvider = new TrackingTimeProvider();
        var serviceContext = new TestServiceContext
        {
            TimeProvider = timeProvider
        };
        serviceContext.ServerOptions.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(1);
        var input = new ControllablePipeReader
        {
            AdvanceToCallback = () => throw advanceException,
            CancelPendingReadCallback = () =>
            {
                cancelEntered.TrySetResult();
                releaseCancel.Task.GetAwaiter().GetResult();
            }
        };
        var connection = CreateConnection(input);
        var tags = AddMetricsTagsFeature(connection);
        var middleware = new Http2PrefaceConnectionMiddleware(_ => Task.CompletedTask, serviceContext, HttpProtocols.Http1AndHttp2);

        var middlewareTask = Task.Run(() => middleware.OnConnectionAsync(connection));
        await input.ReadStarted.Task.DefaultTimeout();
        timeProvider.Advance(serviceContext.ServerOptions.Limits.KeepAliveTimeout);
        var fireTask = Task.Run(timeProvider.Timer.Fire);
        await cancelEntered.Task.DefaultTimeout();
        input.CompleteRead(new ReadResult(new ReadOnlySequence<byte>("P"u8.ToArray()), isCanceled: false, isCompleted: false));

        try
        {
            Assert.False(middlewareTask.IsCompleted);
        }
        finally
        {
            releaseCancel.TrySetResult();
        }

        await middlewareTask.DefaultTimeout();
        await fireTask.DefaultTimeout();
        Assert.Contains(tags, tag => tag.Key == "error.type" && (string)tag.Value == "keep_alive_timeout");
    }

    [Fact]
    public async Task AdvanceFailureWithoutStopIsSurfaced()
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
    public async Task DebuggerAttachedDefersExpiredTimeout()
    {
        var debugger = new TestDebugger { IsAttached = true };
        var timeProvider = new TrackingTimeProvider();
        var serviceContext = new TestServiceContext
        {
            TimeProvider = timeProvider
        };
        serviceContext.ServerOptions.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(1);
        var input = new ControllablePipeReader();
        var connection = CreateConnection(input);
        var middleware = new Http2PrefaceConnectionMiddleware(
            _ => Task.CompletedTask,
            serviceContext,
            HttpProtocols.Http1AndHttp2,
            debugger);

        var middlewareTask = middleware.OnConnectionAsync(connection);
        await input.ReadStarted.Task.DefaultTimeout();
        timeProvider.Advance(serviceContext.ServerOptions.Limits.KeepAliveTimeout);
        timeProvider.Timer.Fire();

        Assert.False(input.CancelCalled.Task.IsCompleted);
        Assert.Equal(1, timeProvider.Timer.ChangeCount);

        debugger.IsAttached = false;
        timeProvider.Timer.Fire();
        await input.CancelCalled.Task.DefaultTimeout();
        input.CompleteRead(new ReadResult(default, isCanceled: true, isCompleted: false));
        await middlewareTask.DefaultTimeout();
    }

    [Fact]
    public async Task InfiniteKeepAliveDoesNotCreateTimer()
    {
        using var shutdown = new CancellationTokenSource();
        var timeProvider = new TrackingTimeProvider();
        var serviceContext = new TestServiceContext
        {
            TimeProvider = timeProvider
        };
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

        try
        {
            Assert.Null(timeProvider.Timer);
        }
        finally
        {
            shutdown.Cancel();
            await input.CancelCalled.Task.DefaultTimeout();
            input.CompleteRead(new ReadResult(default, isCanceled: true, isCompleted: false));
            await middlewareTask.DefaultTimeout();
        }
    }

    private static List<KeyValuePair<string, object>> AddMetricsTagsFeature(DefaultConnectionContext connection)
    {
        var tags = new List<KeyValuePair<string, object>>();
        var metricsTagsFeature = new Mock<IConnectionMetricsTagsFeature>();
        metricsTagsFeature.SetupGet(feature => feature.Tags).Returns(tags);
        connection.Features.Set(metricsTagsFeature.Object);
        return tags;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<(WeakReference Connection, TrackingTimeProvider TimeProvider, Task FireTask, TaskCompletionSource Release)> CreateQueuedTimerScenario()
    {
        var timeProvider = new TrackingTimeProvider();
        var serviceContext = new TestServiceContext
        {
            TimeProvider = timeProvider
        };
        var input = new ControllablePipeReader();
        var connection = CreateConnection(input);
        var middleware = new Http2PrefaceConnectionMiddleware(_ => Task.CompletedTask, serviceContext, HttpProtocols.Http1AndHttp2);

        var middlewareTask = middleware.OnConnectionAsync(connection);
        await input.ReadStarted.Task.DefaultTimeout();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var fireTask = timeProvider.Timer.QueueFireAsync(release.Task);
        await timeProvider.Timer.CallbackQueued.Task.DefaultTimeout();
        input.CompleteRead(new ReadResult(new ReadOnlySequence<byte>("G"u8.ToArray()), isCanceled: false, isCompleted: false));
        await middlewareTask.DefaultTimeout();

        return (new WeakReference(connection), timeProvider, fireTask, release);
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

        public TaskCompletionSource CancelCalled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Action AdvanceToCallback { get; init; }

        public Action CancelPendingReadCallback { get; init; }

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
            CancelCalled.TrySetResult();
            CancelPendingReadCallback?.Invoke();
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
                static state => ((TaskCompletionSource<ReadResult>)state!).TrySetCanceled(),
                _readResult);
            return new ValueTask<ReadResult>(_readResult.Task);
        }

        public override bool TryRead(out ReadResult result)
        {
            result = default;
            return false;
        }
    }

    private sealed class BlockingDebugger : IDebugger
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool IsAttached
        {
            get
            {
                Entered.TrySetResult();
                Release.Task.GetAwaiter().GetResult();
                return false;
            }
        }
    }

    private sealed class TestDebugger : IDebugger
    {
        public bool IsAttached { get; set; }
    }

    private sealed class TrackingTimeProvider : TimeProvider
    {
        private readonly Exception _changeException;
        private readonly bool _fireDuringCreate;
        private long _timestamp;

        public TrackingTimeProvider(Exception changeException = null, bool fireDuringCreate = false)
        {
            _changeException = changeException;
            _fireDuringCreate = fireDuringCreate;
        }

        public TrackingTimer Timer { get; private set; }

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => Volatile.Read(ref _timestamp);

        public void Advance(TimeSpan duration) => Interlocked.Add(ref _timestamp, duration.Ticks);

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

        public TaskCompletionSource CallbackQueued { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource DisposeCalled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int ChangeCount { get; private set; }

        public void Fire() => _callback(_state);

        public async Task QueueFireAsync(Task release)
        {
            CallbackQueued.TrySetResult();
            await release;
            _callback(_state);
        }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            ChangeCount++;
            if (_changeException is not null)
            {
                throw _changeException;
            }

            return true;
        }

        public void Dispose()
        {
            Disposed = true;
            DisposeCalled.TrySetResult();
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
