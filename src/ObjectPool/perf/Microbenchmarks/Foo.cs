// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.ObjectPool.Microbenchmarks;

#pragma warning disable S109, CPR138

internal sealed class Foo
{
    public int LastRandom;

    public void SimulateWork()
    {
        // burn some cycles to simulate work being done between get and return
        for (int i = 0; i <= 32; i++)
        {
            LastRandom = Random.Shared.Next();
        }
    }
}
