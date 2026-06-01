// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class ConnectionDispatcherTests : LoggedTest
{
    [Fact]
    public async Task OnConnectionCreatesLogScopeWithConnectionId()
    {
        var testLogger = new TestApplicationErrorLogger();
        var loggerFactory = new LoggerFactory(new[] { new KestrelTestLoggerProvider(testLogger) });

        var serviceContext = new TestServiceContext(loggerFactory);
        // This needs to run inline
        var tcs = new TaskCompletionSource();

        var connection = new Mock<DefaultConnectionContext> { CallBase = true }.Object;
        connection.ConnectionClosed = new CancellationToken(canceled: true);
        var transportConnectionManager = new TransportConnectionManager(serviceContext.ConnectionManager);
        var kestrelConnection = CreateKestrelConnection(serviceContext, connection, transportConnectionManager, connectionDelegate: _ => tcs.Task);
        transportConnectionManager.AddConnection(0, kestrelConnection);

        var task = kestrelConnection.ExecuteAsync();

        // The scope should be created
        var scopeObjects = testLogger.Scopes.OfType<IReadOnlyList<KeyValuePair<string, object>>>().ToList();

        Assert.Single(scopeObjects);
        var pairs = scopeObjects[0].ToDictionary(p => p.Key, p => p.Value);
        Assert.True(pairs.ContainsKey("ConnectionId"));
        Assert.Equal(connection.ConnectionId, pairs["ConnectionId"]);

        tcs.TrySetResult();

        await task;

        // Verify the scope was disposed after request processing completed
        Assert.True(testLogger.Scopes.IsEmpty);
    }

    [Fact]
    public async Task StartAcceptingConnectionsAsyncLogsIfAcceptAsyncThrows()
    {
        var serviceContext = new TestServiceContext(LoggerFactory);

        var dispatcher = new ConnectionDispatcher<ConnectionContext>(serviceContext, _ => Task.CompletedTask, new TransportConnectionManager(serviceContext.ConnectionManager));

        await dispatcher.StartAcceptingConnections(new ThrowingListener());

        var critical = TestSink.Writes.SingleOrDefault(m => m.LogLevel == LogLevel.Critical);
        Assert.NotNull(critical);
        Assert.IsType<InvalidOperationException>(critical.Exception);
        Assert.Equal("Unexpected error listening", critical.Exception.Message);
    }

    [Fact]
    public async Task StartAcceptingConnectionsContinuesAfterConnectionProcessingError()
    {
        var serviceContext = new TestServiceContext(LoggerFactory);

        var dispatcher = new ConnectionDispatcher<ConnectionContext>(serviceContext, _ => Task.CompletedTask, new TransportConnectionManager(serviceContext.ConnectionManager));

        await dispatcher.StartAcceptingConnections(new SingleConnectionListener(new ThrowingConnectionContext()));

        var error = TestSink.Writes.SingleOrDefault(m => m.LogLevel == LogLevel.Error);
        Assert.NotNull(error);
        Assert.StartsWith("The connection listener failed to process an accepted connection.", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnConnectionFiresOnCompleted()
    {
        var serviceContext = new TestServiceContext();

        var connection = new Mock<DefaultConnectionContext> { CallBase = true }.Object;
        connection.ConnectionClosed = new CancellationToken(canceled: true);
        var transportConnectionManager = new TransportConnectionManager(serviceContext.ConnectionManager);
        var kestrelConnection = CreateKestrelConnection(serviceContext, connection, transportConnectionManager);
        transportConnectionManager.AddConnection(0, kestrelConnection);
        var completeFeature = kestrelConnection.TransportConnection.Features.Get<IConnectionCompleteFeature>();

        Assert.NotNull(completeFeature);
        object stateObject = new object();
        object callbackState = null;
        completeFeature.OnCompleted(state => { callbackState = state; return Task.CompletedTask; }, stateObject);

        await kestrelConnection.ExecuteAsync();

        Assert.Equal(stateObject, callbackState);
    }

    [Fact]
    public async Task OnConnectionOnCompletedExceptionCaught()
    {
        var serviceContext = new TestServiceContext(LoggerFactory);
        var connection = new Mock<DefaultConnectionContext> { CallBase = true }.Object;
        connection.ConnectionClosed = new CancellationToken(canceled: true);
        var transportConnectionManager = new TransportConnectionManager(serviceContext.ConnectionManager);
        var kestrelConnection = CreateKestrelConnection(serviceContext, connection, transportConnectionManager);
        transportConnectionManager.AddConnection(0, kestrelConnection);
        var completeFeature = kestrelConnection.TransportConnection.Features.Get<IConnectionCompleteFeature>();

        Assert.NotNull(completeFeature);
        object stateObject = new object();
        object callbackState = null;
        completeFeature.OnCompleted(state => { callbackState = state; throw new InvalidTimeZoneException(); }, stateObject);

        await kestrelConnection.ExecuteAsync();

        Assert.Equal(stateObject, callbackState);
        var errors = TestSink.Writes.Where(e => e.LogLevel >= LogLevel.Error).ToArray();
        Assert.Single(errors);
        Assert.Equal("An error occurred running an IConnectionCompleteFeature.OnCompleted callback.", errors[0].Message);
    }

    private static KestrelConnection<ConnectionContext> CreateKestrelConnection(TestServiceContext serviceContext, DefaultConnectionContext connection, TransportConnectionManager transportConnectionManager, Func<ConnectionContext, Task> connectionDelegate = null)
    {
        connectionDelegate ??= _ => Task.CompletedTask;

        return new KestrelConnection<ConnectionContext>(
            id: 0, serviceContext, transportConnectionManager, connectionDelegate, connection, serviceContext.Log, TestContextFactory.CreateMetricsContext(connection));
    }

    private class ThrowingListener : IConnectionListener<ConnectionContext>
    {
        public EndPoint EndPoint { get; set; }

        public ValueTask<ConnectionContext> AcceptAsync(CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Unexpected error listening");
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }

        public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
        {
            return default;
        }
    }

    private sealed class SingleConnectionListener : IConnectionListener<ConnectionContext>
    {
        private readonly ConnectionContext _connection;
        private bool _returnedConnection;

        public SingleConnectionListener(ConnectionContext connection)
        {
            _connection = connection;
        }

        public EndPoint EndPoint { get; set; }

        public ValueTask<ConnectionContext> AcceptAsync(CancellationToken cancellationToken = default)
        {
            if (_returnedConnection)
            {
                return default;
            }

            _returnedConnection = true;
            return new ValueTask<ConnectionContext>(_connection);
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }

        public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
        {
            return default;
        }
    }

    private sealed class ThrowingConnectionContext : ConnectionContext
    {
        private readonly CancellationTokenSource _connectionClosedTokenSource = new CancellationTokenSource();
        private readonly ThrowingFeatureCollection _features = new ThrowingFeatureCollection();

        public ThrowingConnectionContext()
        {
            ConnectionId = Guid.NewGuid().ToString();
            Features = _features;
            Items = new ConnectionItems();
            Transport = new DummyDuplexPipe();
            ConnectionClosed = _connectionClosedTokenSource.Token;
        }

        public override string ConnectionId { get; set; }
        public override IFeatureCollection Features { get; }
        public override IDictionary<object, object> Items { get; set; }
        public override IDuplexPipe Transport { get; set; }
        public override CancellationToken ConnectionClosed { get; set; }
        public override EndPoint LocalEndPoint { get; set; }
        public override EndPoint RemoteEndPoint { get; set; }

        public override void Abort() => _connectionClosedTokenSource.Cancel();

        public override void Abort(ConnectionAbortedException abortReason) => _connectionClosedTokenSource.Cancel();

        public override ValueTask DisposeAsync()
        {
            var duplexPipe = (DummyDuplexPipe)Transport;
            duplexPipe.Input.Complete();
            duplexPipe.Output.Complete();
            _connectionClosedTokenSource.Dispose();
            return default;
        }

        private sealed class DummyDuplexPipe : IDuplexPipe
        {
            public DummyDuplexPipe()
            {
                var pipe = new Pipe();
                Input = pipe.Reader;
                Output = pipe.Writer;
            }

            public PipeReader Input { get; }
            public PipeWriter Output { get; }
        }

        private sealed class ThrowingFeatureCollection : IFeatureCollection
        {
            private readonly FeatureCollection _inner = new FeatureCollection();
            private bool _isReadOnly;

            public object this[Type key]
            {
                get => _inner[key];
                set => _inner[key] = value;
            }

            public int Revision => _inner.Revision;
            public bool IsReadOnly { get => _isReadOnly; set => _isReadOnly = value; }

            public TFeature Get<TFeature>() => _inner.Get<TFeature>();

            public void Set<TFeature>(TFeature feature)
            {
                throw new InvalidOperationException("The feature collection failed during connection processing.");
            }

            public IEnumerator<KeyValuePair<Type, object>> GetEnumerator() => _inner.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
