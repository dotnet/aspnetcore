// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.Tracing;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.Internal;
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
            Assert.Equal("Microsoft.AspNetCore.Http.Connections", eventSource.Name);
            Assert.Equal(Guid.Parse("c26fe4b6-8790-5d41-5900-0f2b6b74efaa"), eventSource.Guid);
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

        [Fact]
        public void ConnectionStop()
        {
            // Arrange
            var expectedEventId = 2;
            var eventListener = new TestEventListener(expectedEventId);
            var httpConnectionsEventSource = GetHttpConnectionEventSource();
            eventListener.EnableEvents(httpConnectionsEventSource, EventLevel.Informational);

            // Act
            var stopWatch = ValueStopwatch.StartNew();
            httpConnectionsEventSource.ConnectionStop("1", stopWatch);

            // Assert
            var eventData = eventListener.EventData;
            Assert.NotNull(eventData);
            Assert.Equal(expectedEventId, eventData.EventId);
            Assert.Equal("ConnectionStop", eventData.EventName);
            Assert.Equal(EventLevel.Informational, eventData.Level);
            Assert.Same(httpConnectionsEventSource, eventData.EventSource);
            Assert.Equal("Stopped connection '{0}'.", eventData.Message);
            Assert.Collection(eventData.Payload,
                arg =>
                {
                    Assert.Equal("1", arg);
                });
        }

        [Fact]
        public void ConnectionTimedOut()
        {
            // Arrange
            var expectedEventId = 3;
            var eventListener = new TestEventListener(expectedEventId);
            var httpConnectionsEventSource = GetHttpConnectionEventSource();
            eventListener.EnableEvents(httpConnectionsEventSource, EventLevel.Informational);

            // Act
            httpConnectionsEventSource.ConnectionTimedOut("1");

            // Assert
            var eventData = eventListener.EventData;
            Assert.NotNull(eventData);
            Assert.Equal(expectedEventId, eventData.EventId);
            Assert.Equal("ConnectionTimedOut", eventData.EventName);
            Assert.Equal(EventLevel.Informational, eventData.Level);
            Assert.Same(httpConnectionsEventSource, eventData.EventSource);
            Assert.Equal("Connection '{0}' timed out.", eventData.Message);
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
    }
}
