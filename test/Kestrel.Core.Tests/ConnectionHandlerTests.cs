using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Protocols.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Abstractions.Internal;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests
{
    public class ConnectionHandlerTests
    {
        [Fact]
        public void OnConnectionCreatesLogScopeWithConnectionId()
        {
            var serviceContext = new TestServiceContext();
            var tcs = new TaskCompletionSource<object>();
            var handler = new ConnectionHandler(serviceContext, _ => tcs.Task);

            var connection = new TestConnection();

            handler.OnConnection(connection);

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

        private class TestConnection : FeatureCollection, IConnectionIdFeature, IConnectionTransportFeature
        {
            public TestConnection()
            {
                Set<IConnectionIdFeature>(this);
                Set<IConnectionTransportFeature>(this);
            }

            public MemoryPool<byte> MemoryPool { get; } = KestrelMemoryPool.Create();

            public IDuplexPipe Transport { get; set; }
            public IDuplexPipe Application { get; set; }

            public PipeScheduler InputWriterScheduler => PipeScheduler.ThreadPool;

            public PipeScheduler OutputReaderScheduler => PipeScheduler.ThreadPool;

            public string ConnectionId { get; set; }
        }
    }
}
