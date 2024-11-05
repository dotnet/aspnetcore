# Event Sources, Counters and Listeners, Oh My!

## Overview

This is a quick overview of how to add `EventSource` and `EventCounter` tracing to an ASP.NET library.

### Prerequisites

You should have a basic understanding of `EventSource` and how to write events. See <https://blogs.msdn.microsoft.com/vancem/2012/07/09/introduction-tutorial-logging-etw-events-in-c-system-diagnostics-tracing-eventsource/> for some guidance.

Similarly, you should have a basic understanding of `EventCounter` and how they work. See <https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.Tracing/documentation/EventCounterTutorial.md> for guidance.

## Event Patterns

* Add `ILogger` based tracing in all the places you add `EventSource` tracing unless there's a really good reason not to. You can always use the `Trace` level ;).
* `Start`/`Stop` events should have at least one payload value in common that can be used to correlate them. For example, Request ID, Request Path, Action Name, etc.
* `Error` events should use the `EventLevel.Error` level (which should be obvious I hope ;))
* `Stop` events should include a `double durationInMilliseconds` payload value with the duration between the `Start` and `End` event in milliseconds.
* Use `ValueStopwatch` from [Microsoft.Extensions.ValueStopwatch.Sources](https://github.com/aspnet/Common/tree/dev/shared/Microsoft.Extensions.ValueStopwatch.Sources) for timing since it avoids a heap allocation (it's a simple wrapper around a long)
* For timing events, have the `Start` event method return a `ValueStopwatch`. Have that method return `default(ValueStopwatch)` when the event is disabled, and `ValueStopwatch.StartNew()` when it is enabled. Have the `End` event take in the `ValueStopwatch` and use `.IsActive` to check if it was actually started, and then if it was started use `GetElapsedTime` to get the duration. This ensures that if the event is disabled, we don't have the perf hit of calling `ValueStopwatch.StartNew()`.
* Payload values can only be primitives, so rich objects have to be expanded in advance or via a `NonEvent` wrapper method.
* Exceptions should expand to three `string` payload parameters: `exceptionType` (`.GetType().AssemblyQualifiedName` output), `exceptionMessage` (`.Message` output), `exceptionDetails` (`.ToString()` output).
* Make Payload Names descriptive as they are seen in Event viewing tools like PerfView. For example `durationInMilliseconds` instead of `duration`.
* Naming Patterns:
  * Use `Start` as a suffix for events signalling the start of an operation. Do **not** use `Begin` or `Started`
  * Use `Stop` as a suffix for events signalling the end of an operation. Do **not** use `Stopped`, `End` or `Ended`
  * Fire `Stop` events even when a `Failure` event is also fired. Treat `Stop` as a `finally` block.
  * Use `Failure` as a suffix for events signalling an error.
  * Use present-tense verbs (`Timeout` not `TimedOut`)
  * Use `NounVerb` form (`ConnectionStart` not `StartConnection`)
* In the `EventSource` type, keep all methods marked with `[Event]` together and sorted by `eventId`
* .NET Core 2.0.3 or higher is required to view `EventCounters` (they were broken at runtime in 2.0.0 RTM). You can compile fine with 2.0 RTM though, it just won't actually trigger the counter

More patterns can be added as we find them :).

## Code Style

* Our event sources should have names that match the name of the containing assembly, but with `.` replaced with `-`. For example `Microsoft.AspNetCore.Authentication` becomes `Microsoft-AspNetCore-Authentication`.
* Event sources should always be `internal`
* Event sources should declare a `private` parameterless constructor
* Event sources should declare a `public static readonly` instance of themselves called `Log`
* Event sources should have names that end `EventSource`, for example `DependencyInjectionEventSource`

## Event Pattern

In general, events should follow a pattern like this:

```csharp
// This needs to have the NonEventAttribute because otherwise EventSource will try to auto-generate a manifest for it!
[NonEvent]
public void SomethingHappened(ObjectNeededToCalculateThePayload p, AnotherObjectNeededToCalculateThePayload p2)
{
  // Check that the source is enabled (regardless of level)
  if (IsEnabled())
  {
    // Write to an event counter if one is associated with this event
    _somethingsHappenedCounter.WriteMetric(1.0f);

    // Check that this specific event is actually enabled (by level and optionally keywords).
    if (IsEnabled(EventLevel.Informational, EventKeywords.None))
    {
      // Do any complex calculation needed to determine the payload values.
      var payloadValue = CalculateThePayload(p);

      // Fire the actual event method
      SomethingHappened(payloadValue, p2.MorePayload, p2.SomeValue - p2.SomeOtherValue);
    }
  }
}

// This has to be a separate method so that EventSource can generate an ETW manifest.
// The eventId field is required and must also be passed to WriteEvent.
[Event(eventId: 42, Level = EventLevel.Informational)]
private void SomethingHappened(string payloadValue, int anotherPayloadValue, double morePayload) => WriteEvent(42, payloadValue, anotherPayloadValue, morePayload);
```

The above example is one that covers basically all the scenarios. However, many of the individual elements of that pattern can be elided when not needed. For example, when there isn't any complex payload calculation, everything can be done in a single method with the `Event` attribute. We generally write EventCounters regardless of the level that the provider is activated at (see the section on counters), hence why we check `IsEnabled()` (no parameters) in the above example. However, if there is no counter associated with the event, the `IsEnabled(EventLevel, EventKeyword)` overload can be used directly. So, below is an example of an event with a simple payload and no event counter:

```csharp
[Event(eventId: 42, Level = EventLevel.Informational)]
public void SomethingHappened(string payloadValue)
{
  if (IsEnabled(EventLevel.Informational, EventKeywords.None))
  {
    WriteEvent(42, payloadValue);
  }
}
```

## Keywords

When we have places where we want to enable events but only when explicitly requested by the user, we can use Keywords to control those. Keywords are a simple flags value that are provided when a listener enables an event source, and can be tested when calling `IsEnabled`. See <https://msdn.microsoft.com/library/dn774985(v=pandp.20).aspx#_Using_keywords> for some guidance about keywords

## Event Counters

Since Event Counters are only actually enabled when the `EventCounterIntervalSec` parameter is provided to the event source when a listener attaches, we don't really need a level or keyword to control them. As a result, we always write to them, when the EventSource itself is enabled.

There are a number of different "kinds" of event counters in our system. They are characterized by what kind of value is written in the `EventCounter.WriteMetric` call. Counters provide multiple aggregations (Count, Mean, StdDev, Min, Max), and certain aggregations are appropriate for different kinds of counters.

* Counters track the number of times an event occurs. They are written by calling `.WriteMetric(1.0f)` to the counter. The consumer can determine the number of events that occurred over an interval by reading the "Count" aggregation. They should have names combining a plural noun and adjective, like "RequestsStarted"
* Metrics track a value that changes over time or per "unit" (i.e. per request, per connection, etc.). They are written by calling `.WriteMetric` with the current value of the metric. The consumer can use the aggregates to get data about the metric over time. They should have names combining a singular noun describing the metric, like "RequestBodySize"
  * Durations are a Metric that tracks a time duration in milliseconds. They have names ending with "Duration", like "RequestDuration"

## Full Example

Here is an example of a straw-man Event Source for Authentication:

```csharp
using System;
using System.Diagnostics.Tracing;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication.Internal
{
    [EventSource(Name = "Microsoft-AspNetCore-Authentication")]
    public class AuthenticationEventSource : EventSource
    {
        public static readonly AuthenticationEventSource Log = new AuthenticationEventSource();
        private readonly EventCounter _authenticationMiddlewareDuration;

        private AuthenticationEventSource()
        {
            _authenticationMiddlewareDuration = new EventCounter("AuthenticationMiddlewareDuration", this);
        }

        [NonEvent]
        internal void AuthenticationMiddlewareStart(HttpContext context)
        {
            if (IsEnabled(EventLevel.Informational, EventKeywords.None))
            {
                AuthenticationMiddlewareStart(context.TraceIdentifier, context.Request.Path.Value);
            }
        }

        [NonEvent]
        internal void AuthenticationMiddlewareEnd(HttpContext context, TimeSpan duration)
        {
            if (IsEnabled())
            {
                _authenticationMiddlewareDuration.WriteMetric((float)duration.TotalMilliseconds);

                if (IsEnabled(EventLevel.Informational, EventKeywords.None))
                {
                    AuthenticationMiddlewareEnd(context.TraceIdentifier, context.Request.Path.Value, duration.TotalMilliseconds);
                }
            }
        }

        [NonEvent]
        internal void AuthenticationMiddlewareFailure(HttpContext context, Exception ex)
        {
            if(IsEnabled(EventLevel.Error, EventKeywords.None))
            {
                AuthenticationMiddlewareFailure(context.TraceIdentifier, context.Request.Path.Value, ex.GetType().FullName, ex.Message, ex.ToString());
            }
        }

        [Event(eventId: 1, Level = EventLevel.Informational)]
        private void AuthenticationMiddlewareStart(string traceIdentifier, string path) => WriteEvent(1, traceIdentifier, path);

        [Event(eventId: 2, Level = EventLevel.Informational)]
        private void AuthenticationMiddlewareEnd(string traceIdentifier, string path, double durationMilliseconds) => WriteEvent(2, traceIdentifier, path, durationMilliseconds);

        [Event(eventId: 3, Level = EventLevel.Error)]
        private void AuthenticationMiddlewareFailure(string traceIdentifier, string value, string exceptionTypeName, string message, string fullException) => WriteEvent(3, traceIdentifier, value, exceptionTypeName, message, fullException);
    }
}
```

## Automated Testing of EventSources

EventSources can be tested using the `EventSourceTestBase` base class in `Microsoft.AspNetCore.InternalTesting`. An example test is below:

```csharp
// The base class MUST be used for EventSource testing because EventSources are global and parallel tests can cause issues.
// The base class adds some code to handle that.
public class SomeTest : EventSourceTestBase
{
    [Fact]
    public void TestName()
    {
        // Arrange: Explicitly register the event sources to listen to.
        CollectFrom("Microsoft-AspNetCore-SomeEventSourceName");

        // Act: Do things that causes the events to be fired.
        DoStuff();

        // Assert: Get the collected events and assert that they match the expectations
        var events = GetEvents();

        // EventAssert is a helper for testing events. It's a little odd in that EventAssert.Event returns a "builder"
        // that creates an Action<EventWrittenEventArgs> that will assert the things you configured when called.
        // This pattern makes for clearer test code.
        EventAssert.Collection(events,
            EventAssert.Event(1, "Test", EventLevel.Informational),
            EventAssert.Event(2, "TestWithPayload", EventLevel.Verbose)
                .Payload("payload1", 42)
                .Payload("payload2", 4.2));
    }
}
```

**NOTE:** The test listener does not currently support collecting EventCounters. If you need that, file an issue in Testing and ping @anurse.
