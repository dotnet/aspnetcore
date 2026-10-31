// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Abstractions;

namespace BasicWebSite;

public class ActionDescriptorCreationCounter : IActionDescriptorProvider
{
    private long _callCount;

    public long CallCount
    {
        get
        {
            var callCount = Interlocked.Read(ref _callCount);

            return callCount;
        }
    }

    public int Order
    {
        get
        {
            return -1000 - 100;
        }
    }

    public void OnProvidersExecuting(ActionDescriptorProviderContext context)
    {
    }

    public void OnProvidersExecuted(ActionDescriptorProviderContext context)
    {
        if (context.Results.Count == 0)
        {
            throw new InvalidOperationException("No actions found!");
        }

        Interlocked.Increment(ref _callCount);
    }
}
