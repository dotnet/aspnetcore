// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests
{
    public class EventSourceTests : LoggedTest
    {
        private readonly TestEventListener _listener = new TestEventListener();

        public EventSourceTests()
        {
            _listener.EnableEvents(KestrelEventSource.Log, EventLevel.Verbose);
        }

        [Fact]
        public async Task EmitsConnectionStartAndStop()
        {
            string connectionId = null;
            string requestId = null;
            int port;
            using (var server = new TestServer(context =>
            {
                connectionId = context.Features.Get<IHttpConnectionFeature>().ConnectionId;
                requestId = context.TraceIdentifier;
                return Task.CompletedTask;
            }, new TestServiceContext(LoggerFactory)))
            {
                port = server.Port;
                using (var connection = server.CreateConnection())
                {
                    await connection.SendAll("GET / HTTP/1.1",
                        "Host:",
                        "",
                        "")
                        .DefaultTimeout();
                    await connection.Receive("HTTP/1.1 200");
                }
            }

            // capture list here as other tests executing in parallel may log events
            Assert.NotNull(connectionId);
            Assert.NotNull(requestId);

            var events = _listener.EventData.Where(e => e != null && GetProperty(e, "connectionId") == connectionId).ToList();

            {
                var start = Assert.Single(events, e => e.EventName == "ConnectionStart");
                Assert.All(new[] { "connectionId", "remoteEndPoint", "localEndPoint" }, p => Assert.Contains(p, start.PayloadNames));
                Assert.Equal($"127.0.0.1:{port}", GetProperty(start, "localEndPoint"));
            }
            {
                var stop = Assert.Single(events, e => e.EventName == "ConnectionStop");
                Assert.All(new[] { "connectionId" }, p => Assert.Contains(p, stop.PayloadNames));
                Assert.Same(KestrelEventSource.Log, stop.EventSource);
            }
            {
                var requestStart = Assert.Single(events, e => e.EventName == "RequestStart");
                Assert.All(new[] { "connectionId", "requestId" }, p => Assert.Contains(p, requestStart.PayloadNames));
                Assert.Equal(requestId, GetProperty(requestStart, "requestId"));
                Assert.Same(KestrelEventSource.Log, requestStart.EventSource);
            }
            {
                var requestStop = Assert.Single(events, e => e.EventName == "RequestStop");
                Assert.All(new[] { "connectionId", "requestId" }, p => Assert.Contains(p, requestStop.PayloadNames));
                Assert.Equal(requestId, GetProperty(requestStop, "requestId"));
                Assert.Same(KestrelEventSource.Log, requestStop.EventSource);
            }
        }

        private string GetProperty(EventWrittenEventArgs data, string propName)
            => data.Payload[data.PayloadNames.IndexOf(propName)] as string;

        private class TestEventListener : EventListener
        {
            private volatile bool _disposed;
            private ConcurrentQueue<EventWrittenEventArgs> _events = new ConcurrentQueue<EventWrittenEventArgs>();

            public IEnumerable<EventWrittenEventArgs> EventData => _events;

            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                if (!_disposed)
                {
                    _events.Enqueue(eventData);
                }
            }

            public override void Dispose()
            {
                _disposed = true;
                base.Dispose();
            }
        }

        public override void Dispose()
        {
            _listener.Dispose();
            base.Dispose();
        }
    }
}
