// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Components.Routing;

internal readonly struct RouteKey : IEquatable<RouteKey>
{
    public readonly Assembly? AppAssembly;
    public readonly HashSet<Assembly>? AdditionalAssemblies;

    public RouteKey(Assembly appAssembly, IEnumerable<Assembly> additionalAssemblies)
    {
        AppAssembly = appAssembly;
        AdditionalAssemblies = additionalAssemblies is null ? null : new HashSet<Assembly>(additionalAssemblies);
    }

    public override bool Equals(object? obj)
    {
        return obj is RouteKey other && Equals(other);
    }

    public bool Equals(RouteKey other)
    {
        if (!Equals(AppAssembly, other.AppAssembly))
        {
            return false;
        }

        if (AdditionalAssemblies is null && other.AdditionalAssemblies is null)
        {
            return true;
        }

        if (AdditionalAssemblies is null || other.AdditionalAssemblies is null)
        {
            return false;
        }

        return AdditionalAssemblies.Count == other.AdditionalAssemblies.Count &&
            AdditionalAssemblies.SetEquals(other.AdditionalAssemblies);
    }

    public override int GetHashCode()
    {
        if (AppAssembly is null)
        {
            return 0;
        }

        if (AdditionalAssemblies is null)
        {
            return AppAssembly.GetHashCode();
        }

        // Producing a hash code that includes individual assemblies requires it to have a stable order.
        // We'll avoid the cost of sorting and simply include the number of assemblies instead.
        return HashCode.Combine(AppAssembly, AdditionalAssemblies.Count);
    }
}
