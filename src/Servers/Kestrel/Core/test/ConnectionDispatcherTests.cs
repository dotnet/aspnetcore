// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.AspNetCore.Testing;
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

            var connection = new Mock<TransportConnection> { CallBase = true }.Object;
            connection.ConnectionClosed = new CancellationToken(canceled: true);

            dispatcher.OnConnection(connection);

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
        public async Task OnConnectionCompletesTransportPipesAfterReturning()
        {
            var serviceContext = new TestServiceContext();
            var dispatcher = new ConnectionDispatcher(serviceContext, _ => Task.CompletedTask);

            var mockConnection = new Mock<TransportConnection> { CallBase = true };
            mockConnection.Object.ConnectionClosed = new CancellationToken(canceled: true);
            var mockPipeReader = new Mock<PipeReader>();
            var mockPipeWriter = new Mock<PipeWriter>();
            var mockPipe = new Mock<IDuplexPipe>();
            mockPipe.Setup(m => m.Input).Returns(mockPipeReader.Object);
            mockPipe.Setup(m => m.Output).Returns(mockPipeWriter.Object);
            mockConnection.Setup(m => m.Transport).Returns(mockPipe.Object);
            var connection = mockConnection.Object;

            await dispatcher.OnConnection(connection);

            mockPipeWriter.Verify(m => m.Complete(It.IsAny<Exception>()), Times.Once());
            mockPipeReader.Verify(m => m.Complete(It.IsAny<Exception>()), Times.Once());
        }
    }
}
