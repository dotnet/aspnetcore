// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Hosting
{
    public class HostingEventSourceTests
    {
        [Fact]
        public void MatchesNameAndGuid()
        {
            // Arrange & Act
            var eventSource = new HostingEventSource();

            // Assert
            Assert.Equal("Microsoft.AspNetCore.Hosting", eventSource.Name);
            Assert.Equal(Guid.Parse("9ded64a4-414c-5251-dcf7-1e4e20c15e70"), eventSource.Guid);
        }

        [Fact]
        public void HostStart()
        {
            // Arrange
            var expectedEventId = 1;
            var eventListener = new TestEventListener(expectedEventId);
            var hostingEventSource = GetHostingEventSource();
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
            var hostingEventSource = GetHostingEventSource();
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
            var hostingEventSource = GetHostingEventSource();
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
            var hostingEventSource = GetHostingEventSource();
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
            var hostingEventSource = GetHostingEventSource();
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

        [Fact]
        public async Task VerifyCountersFireWithCorrectValues()
        {
            // Arrange
            var eventListener = new TestCounterListener(new[] {
                "requests-per-second",
                "total-requests",
                "current-requests",
                "failed-requests"
            });

            var hostingEventSource = GetHostingEventSource();

            using var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var rpsValues = eventListener.GetCounterValues("requests-per-second", timeoutTokenSource.Token).GetAsyncEnumerator();
            var totalRequestValues = eventListener.GetCounterValues("total-requests", timeoutTokenSource.Token).GetAsyncEnumerator();
            var currentRequestValues = eventListener.GetCounterValues("current-requests", timeoutTokenSource.Token).GetAsyncEnumerator();
            var failedRequestValues = eventListener.GetCounterValues("failed-requests", timeoutTokenSource.Token).GetAsyncEnumerator();

            eventListener.EnableEvents(hostingEventSource, EventLevel.Informational, EventKeywords.None,
                new Dictionary<string, string>
                {
                    { "EventCounterIntervalSec", "1" }
                });

            // Act & Assert
            hostingEventSource.RequestStart("GET", "/");

            Assert.Equal(1, await totalRequestValues.FirstOrDefault(v => v == 1));
            Assert.Equal(1, await rpsValues.FirstOrDefault(v => v == 1));
            Assert.Equal(1, await currentRequestValues.FirstOrDefault(v => v == 1));
            Assert.Equal(0, await failedRequestValues.FirstOrDefault(v => v == 0));

            hostingEventSource.RequestStop();

            Assert.Equal(1, await totalRequestValues.FirstOrDefault(v => v == 1));
            Assert.Equal(0, await rpsValues.FirstOrDefault(v => v == 0));
            Assert.Equal(0, await currentRequestValues.FirstOrDefault(v => v == 0));
            Assert.Equal(0, await failedRequestValues.FirstOrDefault(v => v == 0));

            hostingEventSource.RequestStart("POST", "/");

            Assert.Equal(2, await totalRequestValues.FirstOrDefault(v => v == 2));
            Assert.Equal(1, await rpsValues.FirstOrDefault(v => v == 1));
            Assert.Equal(1, await currentRequestValues.FirstOrDefault(v => v == 1));
            Assert.Equal(0, await failedRequestValues.FirstOrDefault(v => v == 0));

            hostingEventSource.RequestFailed();
            hostingEventSource.RequestStop();

            Assert.Equal(2, await totalRequestValues.FirstOrDefault(v => v == 2));
            Assert.Equal(0, await rpsValues.FirstOrDefault(v => v == 0));
            Assert.Equal(0, await currentRequestValues.FirstOrDefault(v => v == 0));
            Assert.Equal(1, await failedRequestValues.FirstOrDefault(v => v == 1));
        }

        private static HostingEventSource GetHostingEventSource()
        {
            return new HostingEventSource(Guid.NewGuid().ToString());
        }
    }
}
