// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class ConnectionDispatcherTests
    {
        [Fact]
        public void OnConnectionCreatesLogScopeWithConnectionId()
        {
            var serviceContext = new TestServiceContext();
            // This needs to run inline
            var tcs = new TaskCompletionSource<object>();
            var dispatcher = new ConnectionDispatcher(serviceContext, _ => tcs.Task);

            var connection = new Mock<DefaultConnectionContext> { CallBase = true }.Object;
            connection.ConnectionClosed = new CancellationToken(canceled: true);

            _ = dispatcher.Execute(new KestrelConnection(connection, Mock.Of<ILogger>()));

            // The scope should be created
            var scopeObjects = ((TestKestrelTrace)serviceContext.Log)
                                    .Logger
                                    .Scopes
                                    .OfType<IReadOnlyList<KeyValuePair<string, object>>>()
                                    .ToList();

            Assert.Single(scopeObjects);
            var pairs = scopeObjects[0].ToDictionary(p => p.Key, p => p.Value);
            Assert.True(pairs.ContainsKey("ConnectionId"));
            Assert.Equal(connection.ConnectionId, pairs["ConnectionId"]);

            tcs.TrySetResult(null);

            // Verify the scope was disposed after request processing completed
            Assert.True(((TestKestrelTrace)serviceContext.Log).Logger.Scopes.IsEmpty);
        }

        [Fact]
        public async Task StartAcceptingConnectionsAsyncLogsIfAcceptAsyncThrows()
        {
            var serviceContext = new TestServiceContext();
            var logger = ((TestKestrelTrace)serviceContext.Log).Logger;
            logger.ThrowOnCriticalErrors = false;

            var dispatcher = new ConnectionDispatcher(serviceContext, _ => Task.CompletedTask);

            await dispatcher.StartAcceptingConnections(new ThrowingListener());

            Assert.Equal(1, logger.CriticalErrorsLogged);
            var critical = logger.Messages.SingleOrDefault(m => m.LogLevel == LogLevel.Critical);
            Assert.NotNull(critical);
            Assert.IsType<InvalidOperationException>(critical.Exception);
            Assert.Equal("Unexpected error listening", critical.Exception.Message);
        }

        [Fact]
        public async Task OnConnectionFiresOnCompleted()
        {
            var serviceContext = new TestServiceContext();
            var dispatcher = new ConnectionDispatcher(serviceContext, _ => Task.CompletedTask);

            var connection = new Mock<DefaultConnectionContext> { CallBase = true }.Object;
            connection.ConnectionClosed = new CancellationToken(canceled: true);
            var kestrelConnection = new KestrelConnection(connection, Mock.Of<ILogger>());
            var completeFeature = kestrelConnection.TransportConnection.Features.Get<IConnectionCompleteFeature>();

            Assert.NotNull(completeFeature);
            object stateObject = new object();
            object callbackState = null;
            completeFeature.OnCompleted(state => { callbackState = state; return Task.CompletedTask; }, stateObject);

            await dispatcher.Execute(kestrelConnection);

            Assert.Equal(stateObject, callbackState);
        }

        [Fact]
        public async Task OnConnectionOnCompletedExceptionCaught()
        {
            var serviceContext = new TestServiceContext();
            var dispatcher = new ConnectionDispatcher(serviceContext, _ => Task.CompletedTask);

            var connection = new Mock<DefaultConnectionContext> { CallBase = true }.Object;
            connection.ConnectionClosed = new CancellationToken(canceled: true);
            var mockLogger = new Mock<ILogger>();
            var kestrelConnection = new KestrelConnection(connection, mockLogger.Object);
            var completeFeature = kestrelConnection.TransportConnection.Features.Get<IConnectionCompleteFeature>();

            Assert.NotNull(completeFeature);
            object stateObject = new object();
            object callbackState = null;
            completeFeature.OnCompleted(state => { callbackState = state; throw new InvalidTimeZoneException(); }, stateObject);

            await dispatcher.Execute(kestrelConnection);

            Assert.Equal(stateObject, callbackState);
            var log = mockLogger.Invocations.First();
            Assert.Equal("An error occured running an IConnectionCompleteFeature.OnCompleted callback.", log.Arguments[2].ToString());
            Assert.IsType<InvalidTimeZoneException>(log.Arguments[3]);
        }

        private class ThrowingListener : IConnectionListener
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
    }
}
