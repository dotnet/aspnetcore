using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Protocols.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
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

            public PipeFactory PipeFactory { get; } = new PipeFactory();

            public IPipeConnection Transport { get; set; }
            public IPipeConnection Application { get; set; }

            public IScheduler InputWriterScheduler => TaskRunScheduler.Default;

            public IScheduler OutputReaderScheduler => TaskRunScheduler.Default;

            public string ConnectionId { get; set; }
        }
    }
}
