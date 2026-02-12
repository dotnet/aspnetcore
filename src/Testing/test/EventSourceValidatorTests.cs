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
        EventSourceValidator.ValidateEventSourceIds(typeof(CorrectEventSource));
    }

    [Fact]
    public void ValidateEventSourceIds_FailsForMismatchedWriteEventId()
    {
        var ex = Assert.ThrowsAny<Exception>(
            () => EventSourceValidator.ValidateEventSourceIds(typeof(MismatchedIdEventSource)));

        Assert.Contains("was assigned event ID 1 but 99 was passed to WriteEvent", ex.Message);
    }

    [Fact]
    public void ValidateEventSourceIds_FailsForDuplicateEventIds()
    {
        var ex = Assert.ThrowsAny<Exception>(
            () => EventSourceValidator.ValidateEventSourceIds(typeof(DuplicateIdEventSource)));

        Assert.Contains("Duplicate EventId 1", ex.Message);
        Assert.Contains("EventAlpha", ex.Message);
        Assert.Contains("EventBeta", ex.Message);
    }

    [Fact]
    public void ValidateEventSourceIds_FailsForNonEventSourceType()
    {
        var ex = Assert.ThrowsAny<Exception>(
            () => EventSourceValidator.ValidateEventSourceIds(typeof(string)));

        Assert.Contains("does not derive from EventSource", ex.Message);
    }

    [Fact]
    public void ValidateEventSourceIds_PassesForEventSourceWithNoEvents()
    {
        EventSourceValidator.ValidateEventSourceIds(typeof(EmptyEventSource));
    }

    [Fact]
    public void ValidateEventSourceIds_PassesForEventSourceWithMultipleParameterTypes()
    {
        EventSourceValidator.ValidateEventSourceIds(typeof(MultiParamEventSource));
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
