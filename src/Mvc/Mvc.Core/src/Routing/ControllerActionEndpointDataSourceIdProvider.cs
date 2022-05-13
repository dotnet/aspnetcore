// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Routing;

internal sealed class ControllerActionEndpointDataSourceIdProvider
{
    private int _nextId = 1;

    internal int CreateId()
    {
        return Interlocked.Increment(ref _nextId);
    }
}
