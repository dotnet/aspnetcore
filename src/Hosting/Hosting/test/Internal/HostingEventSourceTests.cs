// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.Tracing;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.Hosting.Internal
{
    public class HostingEventSourceTests
    {
        [Fact]
        public void MatchesNameAndGuid()
        {
            // Arrange & Act
            var eventSourceType = typeof(WebHost).GetTypeInfo().Assembly.GetType(
                "Microsoft.AspNetCore.Hosting.Internal.HostingEventSource",
                throwOnError: true,
                ignoreCase: false);

            // Assert
            Assert.NotNull(eventSourceType);
            Assert.Equal("Microsoft-AspNetCore-Hosting", EventSource.GetName(eventSourceType));
            Assert.Equal(Guid.Parse("9e620d2a-55d4-5ade-deb7-c26046d245a8"), EventSource.GetGuid(eventSourceType));
            Assert.NotEmpty(EventSource.GenerateManifest(eventSourceType, "assemblyPathToIncludeInManifest"));
        }

        [Fact]
        public void HostStart()
        {
            // Arrange
            var expectedEventId = 1;
            var eventListener = new TestEventListener(expectedEventId);
            var hostingEventSource = HostingEventSource.Log;
            eventListener.EnableEvents(hostingEventSource, EventLevel.Informational);

            // Act
            hostingEventSource.HostStart();

            // Assert
            var eventData = eventListener.EventData;
            Assert.NotNull(eventData);
            Assert.Equal(expectedEventId, eventData.EventId);
            Assert.Equal("HostStart", eventData.EventName);
            Assert.Equal(EventLevel.Informational, eventData.Level);
            Assert.Same(hostingEventSource, eventData.EventSource);
            Assert.Null(eventData.Message);
            Assert.Empty(eventData.Payload);
        }

        [Fact]
        public void HostStop()
        {
            // Arrange
            var expectedEventId = 2;
            var eventListener = new TestEventListener(expectedEventId);
            var hostingEventSource = HostingEventSource.Log;
            eventListener.EnableEvents(hostingEventSource, EventLevel.Informational);

            // Act
            hostingEventSource.HostStop();

            // Assert
            var eventData = eventListener.EventData;
            Assert.NotNull(eventData);
            Assert.Equal(expectedEventId, eventData.EventId);
            Assert.Equal("HostStop", eventData.EventName);
            Assert.Equal(EventLevel.Informational, eventData.Level);
            Assert.Same(hostingEventSource, eventData.EventSource);
            Assert.Null(eventData.Message);
            Assert.Empty(eventData.Payload);
        }

        public static TheoryData<DefaultHttpContext, string[]> RequestStartData
        {
            get
            {
                var variations = new TheoryData<DefaultHttpContext, string[]>();

                var context = new DefaultHttpContext();
                context.Request.Method = "GET";
                context.Request.Path = "/Home/Index";
                variations.Add(
                    context,
                    new string[]
                    {
                        "GET",
                        "/Home/Index"
                    });

                context = new DefaultHttpContext();
                context.Request.Method = "POST";
                context.Request.Path = "/";
                variations.Add(
                    context,
                    new string[]
                    {
                        "POST",
                        "/"
                    });

                return variations;
            }
        }

        [Theory]
        [MemberData(nameof(RequestStartData))]
        public void RequestStart(DefaultHttpContext httpContext, string[] expected)
        {
            // Arrange
            var expectedEventId = 3;
            var eventListener = new TestEventListener(expectedEventId);
            var hostingEventSource = HostingEventSource.Log;
            eventListener.EnableEvents(hostingEventSource, EventLevel.Informational);

            // Act
            hostingEventSource.RequestStart(httpContext.Request.Method, httpContext.Request.Path);

            // Assert
            var eventData = eventListener.EventData;
            Assert.NotNull(eventData);
            Assert.Equal(expectedEventId, eventData.EventId);
            Assert.Equal("RequestStart", eventData.EventName);
            Assert.Equal(EventLevel.Informational, eventData.Level);
            Assert.Same(hostingEventSource, eventData.EventSource);
            Assert.Null(eventData.Message);

            var payloadList = eventData.Payload;
            Assert.Equal(expected.Length, payloadList.Count);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], payloadList[i]);
            }
        }

        [Fact]
        public void RequestStop()
        {
            // Arrange
            var expectedEventId = 4;
            var eventListener = new TestEventListener(expectedEventId);
            var hostingEventSource = HostingEventSource.Log;
            eventListener.EnableEvents(hostingEventSource, EventLevel.Informational);

            // Act
            hostingEventSource.RequestStop();

            // Assert
            var eventData = eventListener.EventData;
            Assert.Equal(expectedEventId, eventData.EventId);
            Assert.Equal("RequestStop", eventData.EventName);
            Assert.Equal(EventLevel.Informational, eventData.Level);
            Assert.Same(hostingEventSource, eventData.EventSource);
            Assert.Null(eventData.Message);
            Assert.Empty(eventData.Payload);
        }

        [Fact]
        public void UnhandledException()
        {
            // Arrange
            var expectedEventId = 5;
            var eventListener = new TestEventListener(expectedEventId);
            var hostingEventSource = HostingEventSource.Log;
            eventListener.EnableEvents(hostingEventSource, EventLevel.Informational);

            // Act
            hostingEventSource.UnhandledException();

            // Assert
            var eventData = eventListener.EventData;
            Assert.Equal(expectedEventId, eventData.EventId);
            Assert.Equal("UnhandledException", eventData.EventName);
            Assert.Equal(EventLevel.Error, eventData.Level);
            Assert.Same(hostingEventSource, eventData.EventSource);
            Assert.Null(eventData.Message);
            Assert.Empty(eventData.Payload);
        }

        private static Exception GetException()
        {
            try
            {
                throw new InvalidOperationException("An invalid operation has occurred");
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

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
                // The tests here run in parallel and since the single publisher instance (HostingEventingSource)
                // notifies all listener instances in these tests, capture the EventData that a test is explicitly
                // looking for and not give back other tests' data.
                if (eventData.EventId == _eventId)
                {
                    EventData = eventData;
                }
            }
        }
    }
}
