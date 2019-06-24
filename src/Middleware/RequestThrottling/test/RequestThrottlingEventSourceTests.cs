using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.RequestThrottling.Tests
{
    public class RequestThrottlingEventSourceTests
    {
        [Fact]
        public void MatchesNameAndGuid()
        {
            var eventSource = new RequestThrottlingEventSource();

            Assert.Equal("Microsoft.AspNetCore.RequestThrottling", eventSource.Name);
            Assert.Equal(Guid.Parse("436f1cb1-8acc-56c0-86ec-e0832bd696ed"), eventSource.Guid);
        }

        [Fact]
        public void RecordsRequestsRejected()
        {
            // Arrange
            var expectedId = 1;
            var eventListener = new TestEventListener(expectedId);
            var eventSource = new RequestThrottlingEventSource();
            eventListener.EnableEvents(eventSource, EventLevel.Informational);

            // Act
            eventSource.RequestRejected();

            // Assert
            var eventData = eventListener.EventData;
            Assert.NotNull(eventData);
            Assert.Equal(expectedId, eventData.EventId);
            Assert.Equal(EventLevel.Warning, eventData.Level);
            Assert.Same(eventSource, eventData.EventSource);
            Assert.Null(eventData.Message);
            Assert.Empty(eventData.Payload);
        }


        // TODO test:
        //  the queue length tracking
        //  the queue duration tracking

        // this means using TestCounterListener

    }
}
