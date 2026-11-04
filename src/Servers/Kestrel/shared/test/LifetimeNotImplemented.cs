// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.InternalTesting;

public class LifetimeNotImplemented : IHostApplicationLifetime
{
    public CancellationToken ApplicationStarted
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public CancellationToken ApplicationStopped
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public CancellationToken ApplicationStopping
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public void StopApplication()
    {
        throw new NotImplementedException();
    }
}
