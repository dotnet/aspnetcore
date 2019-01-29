// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO.Pipelines;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.AspNetCore.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests
{
    public class ConnectionDispatcherTests : TestApplicationErrorLoggerLoggedTest
    {
        [Fact]
        public async Task AppAbortViaConnectionContextIsLogged()
        {
            var testContext = new TestServiceContext(LoggerFactory);
            var connectionDispatcher = new AbortingConnectionDispatcher();

            using (var memoryPool = testContext.MemoryPoolFactory())
            {
                using (var transportConnection = new InMemoryTransportConnection(memoryPool, testContext.Log))
                {
                    transportConnection.Application = new DuplexPipe(reader: null, writer: Mock.Of<PipeWriter>());
                    await connectionDispatcher.OnConnection(transportConnection);
                }
            }

            Assert.Single(TestApplicationErrorLogger.Messages.Where(m => m.Message.Contains("The connection was aborted by the application via ConnectionContext.Abort().")));
        }

        private class AbortingConnectionDispatcher : IConnectionDispatcher
        {
            public Task OnConnection(TransportConnection connection)
            {
                connection.Abort();
                return Task.CompletedTask;
            }
        }
    }
}
