// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.InternalTesting.Tracing;

public class EventAssert
{
    private readonly int _expectedId;
    private readonly string _expectedName;
    private readonly EventLevel _expectedLevel;
    private readonly IList<(string name, Action<object> asserter)> _payloadAsserters = new List<(string, Action<object>)>();

    public EventAssert(int expectedId, string expectedName, EventLevel expectedLevel)
    {
        _expectedId = expectedId;
        _expectedName = expectedName;
        _expectedLevel = expectedLevel;
    }

    public static void Collection(IEnumerable<EventWrittenEventArgs> events, params EventAssert[] asserts)
    {
        Assert.Collection(
            events,
            asserts.Select(a => a.CreateAsserter()).ToArray());
    }

    public static EventAssert Event(int id, string name, EventLevel level)
    {
        return new EventAssert(id, name, level);
    }

    public EventAssert Payload(string name, object expectedValue) => Payload(name, actualValue => Assert.Equal(expectedValue, actualValue));

    public EventAssert Payload(string name, Action<object> asserter)
    {
        _payloadAsserters.Add((name, asserter));
        return this;
    }

    private Action<EventWrittenEventArgs> CreateAsserter() => Execute;

    private void Execute(EventWrittenEventArgs evt)
    {
        Assert.Equal(_expectedId, evt.EventId);
        Assert.Equal(_expectedName, evt.EventName);
        Assert.Equal(_expectedLevel, evt.Level);

        Action<string> CreateNameAsserter((string name, Action<object> asserter) val)
        {
            return actualValue => Assert.Equal(val.name, actualValue);
        }

        Assert.Collection(evt.PayloadNames, _payloadAsserters.Select(CreateNameAsserter).ToArray());
        Assert.Collection(evt.Payload, _payloadAsserters.Select(t => t.asserter).ToArray());
    }
}
