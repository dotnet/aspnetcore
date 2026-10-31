// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Internal;

// Snapshot of the IHubActivator<> registrations, used to decide whether a hub's activator is guaranteed to be the
// default one so it can be created directly instead of being resolved from every scope.
internal sealed class HubActivatorRegistrations
{
    private readonly bool _defaultIsOpenGenericRegistration;
    private readonly HashSet<Type> _closedActivatorTypes = new();

    // Constructed lazily on first resolution, once the service collection can no longer change.
    public HubActivatorRegistrations(IServiceCollection services)
    {
        foreach (var descriptor in services)
        {
            if (descriptor.IsKeyedService)
            {
                continue;
            }

            var serviceType = descriptor.ServiceType;
            if (serviceType == typeof(IHubActivator<>))
            {
                // The last open generic registration wins.
                _defaultIsOpenGenericRegistration = descriptor.ImplementationType == typeof(DefaultHubActivator<>);
            }
            else if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IHubActivator<>))
            {
                // A closed registration for a specific hub takes precedence over the open generic one.
                _closedActivatorTypes.Add(serviceType);
            }
        }
    }

    public bool IsDefaultRegistration(Type hubActivatorType)
    {
        return _defaultIsOpenGenericRegistration && !_closedActivatorTypes.Contains(hubActivatorType);
    }
}
