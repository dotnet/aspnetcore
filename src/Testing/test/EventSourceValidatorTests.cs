// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using Microsoft.AspNetCore.InternalTesting.Tracing;
using Xunit;

namespace Microsoft.AspNetCore.InternalTesting;

public class EventSourceValidatorTests
{
    [Fact]
    public void ValidateEventSourceIds_PassesForCorrectEventSource()
    {
        EventSourceValidator.ValidateEventSourceIds<CorrectEventSource>();
    }

    [Fact]
    public void ValidateEventSourceIds_FailsForMismatchedWriteEventId()
    {
        // GenerateManifest(Strict) detects the mismatch via IL inspection
        // and our validator surfaces it through Assert.Fail.
        // The exact runtime error message varies by .NET version, so we
        // only verify the validator rejects the bad source.
        Assert.ThrowsAny<Exception>(
            () => EventSourceValidator.ValidateEventSourceIds<MismatchedIdEventSource>());
    }

    [Fact]
    public void ValidateEventSourceIds_FailsForDuplicateEventIds()
    {
        // The duplicate ID message is produced by our validator code.
        var ex = Assert.ThrowsAny<Exception>(
            () => EventSourceValidator.ValidateEventSourceIds<DuplicateIdEventSource>());

        Assert.Contains("Duplicate EventId 1", ex.Message);
        Assert.Contains("EventAlpha", ex.Message);
        Assert.Contains("EventBeta", ex.Message);
    }

    [Fact]
    public void ValidateEventSourceIds_FailsForNonEventSourceType()
    {
        // The guard clause message is produced by our validator code.
        var ex = Assert.ThrowsAny<Exception>(
            () => EventSourceValidator.ValidateEventSourceIds(typeof(string)));

        Assert.Contains("does not derive from EventSource", ex.Message);
    }

    [Fact]
    public void ValidateEventSourceIds_PassesForEventSourceWithNoEvents()
    {
        EventSourceValidator.ValidateEventSourceIds<EmptyEventSource>();
    }

    [Fact]
    public void ValidateEventSourceIds_PassesForEventSourceWithMultipleParameterTypes()
    {
        EventSourceValidator.ValidateEventSourceIds<MultiParamEventSource>();
    }

    // -- Test-only EventSource implementations --

    [EventSource(Name = "Test-Correct")]
    private sealed class CorrectEventSource : EventSource
    {
        [Event(1, Level = EventLevel.Informational)]
        public void EventOne(string message) => WriteEvent(1, message);

        [Event(2, Level = EventLevel.Verbose)]
        public void EventTwo(int count) => WriteEvent(2, count);

        [Event(3, Level = EventLevel.Warning)]
        public void EventThree() => WriteEvent(3);
    }

    [EventSource(Name = "Test-MismatchedId")]
    private sealed class MismatchedIdEventSource : EventSource
    {
        [Event(1, Level = EventLevel.Informational)]
        public void EventOne(int value) => WriteEvent(99, value);
    }

    [EventSource(Name = "Test-DuplicateId")]
    private sealed class DuplicateIdEventSource : EventSource
    {
        [Event(1, Level = EventLevel.Informational)]
        public void EventAlpha(string message) => WriteEvent(1, message);

        [Event(1, Level = EventLevel.Informational)]
        public void EventBeta(int count) => WriteEvent(1, count);
    }

    [EventSource(Name = "Test-Empty")]
    private sealed class EmptyEventSource : EventSource
    {
    }

    [EventSource(Name = "Test-MultiParam")]
    private sealed class MultiParamEventSource : EventSource
    {
        [Event(1, Level = EventLevel.Informational)]
        public void EventWithString(string value) => WriteEvent(1, value);

        [Event(2, Level = EventLevel.Informational)]
        public void EventWithInt(int value) => WriteEvent(2, value);

        [Event(3, Level = EventLevel.Informational)]
        public void EventWithLong(long value) => WriteEvent(3, value);

        [Event(4, Level = EventLevel.Informational)]
        public void EventWithMultiple(string name, int count) => WriteEvent(4, name, count);

        [Event(5, Level = EventLevel.Informational)]
        public void EventWithNoArgs() => WriteEvent(5);
    }
}
