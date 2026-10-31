// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Tests;

internal class EventRaisingResourceCounter : ResourceCounter
{
    private readonly ResourceCounter _wrapped;

    public EventRaisingResourceCounter(ResourceCounter wrapped)
    {
        _wrapped = wrapped;
    }

    public event EventHandler OnRelease;
    public event EventHandler<bool> OnLock;

    public override void ReleaseOne()
    {
        _wrapped.ReleaseOne();
        OnRelease?.Invoke(this, EventArgs.Empty);
    }

    public override bool TryLockOne()
    {
        var retVal = _wrapped.TryLockOne();
        OnLock?.Invoke(this, retVal);
        return retVal;
    }
}
