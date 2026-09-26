// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

internal sealed class TestHeartbeatHandler : IHeartbeatHandler
{
    public Action OnHeartbeatCallback { get; set; } = () => { };

    public int OnHeartbeatCount { get; private set; }

    public void OnHeartbeat()
    {
        OnHeartbeatCount++;
        OnHeartbeatCallback();
    }
}
