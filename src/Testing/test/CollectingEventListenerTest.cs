// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting.Tracing;
using Xunit;

namespace Microsoft.AspNetCore.InternalTesting.Tests;

// We are verifying here that when event listener tests are spread among multiple classes, they still
// work, even when run in parallel. To do that we have a bunch of tests in different classes (since
// that affects parallelism) and do some Task.Yielding in them.
public class CollectingEventListenerTests
{
    public abstract class CollectingTestBase : EventSourceTestBase
    {
        [Fact]
        public async Task CollectingEventListenerTest()
        {
            CollectFrom("Microsoft-AspNetCore-Testing-Test");

            await Task.Yield();
            TestEventSource.Log.Test();
            await Task.Yield();
            TestEventSource.Log.TestWithPayload(42, 4.2);
            await Task.Yield();

            var events = GetEvents();
            EventAssert.Collection(events,
                EventAssert.Event(1, "Test", EventLevel.Informational),
                EventAssert.Event(2, "TestWithPayload", EventLevel.Verbose)
                    .Payload("payload1", 42)
                    .Payload("payload2", 4.2));
        }
    }

    // These tests are designed to interfere with the collecting ones by running in parallel and writing events
    public abstract class NonCollectingTestBase
    {
        [Fact]
        public async Task CollectingEventListenerTest()
        {
            await Task.Yield();
            TestEventSource.Log.Test();
            await Task.Yield();
            TestEventSource.Log.TestWithPayload(42, 4.2);
            await Task.Yield();
        }
    }

    public class CollectingTests
    {
        public class A : CollectingTestBase { }
        public class B : CollectingTestBase { }
        public class C : CollectingTestBase { }
        public class D : CollectingTestBase { }
        public class E : CollectingTestBase { }
        public class F : CollectingTestBase { }
        public class G : CollectingTestBase { }
    }

    public class NonCollectingTests
    {
        public class A : NonCollectingTestBase { }
        public class B : NonCollectingTestBase { }
        public class C : NonCollectingTestBase { }
        public class D : NonCollectingTestBase { }
        public class E : NonCollectingTestBase { }
        public class F : NonCollectingTestBase { }
        public class G : NonCollectingTestBase { }
    }
}

[EventSource(Name = "Microsoft-AspNetCore-Testing-Test")]
public class TestEventSource : EventSource
{
    public static readonly TestEventSource Log = new TestEventSource();

    private TestEventSource()
    {
    }

    [Event(eventId: 1, Level = EventLevel.Informational, Message = "Test")]
    public void Test() => WriteEvent(1);

    [Event(eventId: 2, Level = EventLevel.Verbose, Message = "Test")]
    public void TestWithPayload(int payload1, double payload2) => WriteEvent(2, payload1, payload2);
}
