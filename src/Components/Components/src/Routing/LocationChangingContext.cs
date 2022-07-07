// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Routing;

public class LocationChangingContext
{
    internal LocationChangingContext(string location, bool isNavigationIntercepted, bool forceLoad, CancellationToken cancellationToken)
    {
        Location = location;
        IsNavigationIntercepted = isNavigationIntercepted;
        ForceLoad = forceLoad;
        CancellationToken = cancellationToken;
    }

    public string Location { get; }

    public bool IsNavigationIntercepted { get; }

    public bool ForceLoad { get; }

    public CancellationToken CancellationToken { get; }

    public bool IsCanceled { get; private set; }

    public void Cancel()
    {
        IsCanceled = true;
    }
}
