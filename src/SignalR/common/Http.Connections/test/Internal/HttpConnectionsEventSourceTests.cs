// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.Tracing;
using Xunit;

namespace Microsoft.AspNetCore.Http.Connections.Internal
{
    public class HttpConnectionsEventSourceTests
    {
        [Fact]
        public void MatchesNameAndGuid()
        {
            // Arrange & Act
            var eventSource = new HttpConnectionsEventSource();

            // Assert
            Assert.Equal("Microsoft-AspNetCore-Http-Connections", eventSource.Name);
            Assert.Equal(Guid.Parse("a81dd8b5-9721-5e7a-d465-d5b9caa864dc"), eventSource.Guid);
        }

        [Fact]
        public void ConnectionStart()
        {
            // Arrange
            var expectedEventId = 1;
            var eventListener = new TestEventListener(expectedEventId);
            var httpConnectionsEventSource = GetHttpConnectionEventSource();
            eventListener.EnableEvents(httpConnectionsEventSource, EventLevel.Informational);

            // Act
            httpConnectionsEventSource.ConnectionStart("1");

            // Assert
            var eventData = eventListener.EventData;
            Assert.NotNull(eventData);
            Assert.Equal(expectedEventId, eventData.EventId);
            Assert.Equal("ConnectionStart", eventData.EventName);
            Assert.Equal(EventLevel.Informational, eventData.Level);
            Assert.Same(httpConnectionsEventSource, eventData.EventSource);
            Assert.Equal("Started connection '{0}'.", eventData.Message);
            Assert.Collection(eventData.Payload,
                arg =>
                {
                    Assert.Equal("1", arg);
                });
        }

        private static HttpConnectionsEventSource GetHttpConnectionEventSource()
        {
            return new HttpConnectionsEventSource(Guid.NewGuid().ToString());
        }

        // TODO: Shared source
        private class TestEventListener : EventListener
        {
            private readonly int _eventId;

            public TestEventListener(int eventId)
            {
                _eventId = eventId;
            }

            public EventWrittenEventArgs EventData { get; private set; }

            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                // The tests here run in parallel, capture the EventData that a test is explicitly
                // looking for and not give back other tests' data.
                if (eventData.EventId == _eventId)
                {
                    EventData = eventData;
                }
            }
        }
    }
}
