// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

internal sealed class TestTimeoutHandler : ITimeoutHandler
{
    private readonly List<TimeoutInvocation> _invocations = new();

    public Action<TimeoutReason> OnTimeoutCallback { get; set; }

    public IReadOnlyList<TimeoutReason> TimeoutReasons => _invocations.Select(invocation => invocation.Reason).ToArray();

    public int OnTimeoutCount => _invocations.Count;

    public int Count(TimeoutReason reason) => _invocations.Count(invocation => invocation.Reason == reason);

    public void OnTimeout(TimeoutReason reason)
    {
        _invocations.Add(new TimeoutInvocation(reason));
        OnTimeoutCallback?.Invoke(reason);
    }

    public void AssertOnTimeoutCount(int expectedCount)
    {
        Assert.Equal(expectedCount, _invocations.Count);
        foreach (var invocation in _invocations)
        {
            invocation.Verified = true;
        }
    }

    public void AssertOnTimeoutCount(TimeoutReason reason, int expectedCount)
    {
        var matchingInvocations = _invocations.Where(invocation => invocation.Reason == reason).ToList();
        Assert.Equal(expectedCount, matchingInvocations.Count);
        foreach (var invocation in matchingInvocations)
        {
            invocation.Verified = true;
        }
    }

    public void AssertNoOtherCalls()
    {
        Assert.DoesNotContain(_invocations, invocation => !invocation.Verified);
    }

    private sealed class TimeoutInvocation
    {
        public TimeoutInvocation(TimeoutReason reason)
        {
            Reason = reason;
        }

        public TimeoutReason Reason { get; }

        public bool Verified { get; set; }
    }
}
