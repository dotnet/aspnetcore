// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

internal sealed class TestTimeoutHandler : ITimeoutHandler
{
    private readonly List<TimeoutReason> _reasons = new();

    public Action<TimeoutReason> OnTimeoutCallback { get; set; }

    public IReadOnlyList<TimeoutReason> TimeoutReasons => _reasons;

    public int OnTimeoutCount => _reasons.Count;

    public int Count(TimeoutReason reason) => _reasons.Count(r => r == reason);

    public void OnTimeout(TimeoutReason reason)
    {
        _reasons.Add(reason);
        OnTimeoutCallback?.Invoke(reason);
    }
}
