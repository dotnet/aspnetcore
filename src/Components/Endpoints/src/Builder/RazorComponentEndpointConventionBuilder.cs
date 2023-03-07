// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Builder;

internal class RazorComponentEndpointConventionBuilder : IEndpointConventionBuilder
{
    private readonly object _lock;
    private readonly List<Action<EndpointBuilder>> _conventions;

    internal RazorComponentEndpointConventionBuilder(object @lock, List<Action<EndpointBuilder>> conventions)
    {
        _lock = @lock;
        _conventions = conventions;
    }

    public void Add(Action<EndpointBuilder> convention)
    {
        ArgumentNullException.ThrowIfNull(convention);

        // The lock is shared with the data source. We want to lock here
        // to avoid mutating this list while its read in the data source.
        lock (_lock)
        {
            _conventions.Add(convention);
        }
    }
}
