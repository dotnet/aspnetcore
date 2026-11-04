// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Hosting.Fakes;

public class FakeService : IFakeEveryService, IDisposable
{
    public bool Disposed { get; private set; }

    public void Dispose()
    {
        Disposed = true;
    }
}
