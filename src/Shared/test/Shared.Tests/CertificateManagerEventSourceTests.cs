// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;
using Microsoft.AspNetCore.Certificates.Generation;
using Microsoft.AspNetCore.InternalTesting.Tracing;

namespace Microsoft.AspNetCore.Internal.Tests;

[Collection(nameof(CertificateManagerEventSourceTestCollection))]
public class CertificateManagerEventSourceTests
{
    [Fact]
    public void EventIdsAreConsistent()
    {
        EventSourceValidator.ValidateEventSourceIds<CertificateManager.CertificateManagerEventSource>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExportCertificateStartWritesIncludePrivateKeyAsBoolean(bool includePrivateKey)
    {
        var eventSource = CertificateManager.Log;
        using var listener = new TestEventListener(eventId: 23);
        listener.EnableEvents(eventSource, EventLevel.Verbose);

        eventSource.ExportCertificateStart("certificate", "path", includePrivateKey);

        var eventData = Assert.IsType<EventWrittenEventArgs>(listener.EventData);
        Assert.Equal(23, eventData.EventId);
        Assert.Collection(
            eventData.Payload!,
            payload => Assert.Equal("certificate", payload),
            payload => Assert.Equal("path", payload),
            payload => Assert.Equal(includePrivateKey, Assert.IsType<bool>(payload)));
    }

    private sealed class TestEventListener(int eventId) : EventListener
    {
        public EventWrittenEventArgs? EventData { get; private set; }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventId == eventId)
            {
                EventData = eventData;
            }
        }
    }
}

// EventSource instances are process-global, so isolate these tests from other certificate tests.
[CollectionDefinition(nameof(CertificateManagerEventSourceTestCollection), DisableParallelization = true)]
public class CertificateManagerEventSourceTestCollection { }
