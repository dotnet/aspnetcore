// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.SignalR.Internal;

/// <summary>
/// Records the built-in <see cref="IHubActivator{THub}"/> registration so <see cref="DefaultHubDispatcher{THub}"/>
/// can tell whether the effective registration for a hub is still SignalR's default one.
/// </summary>
internal sealed class DefaultHubActivatorRegistration
{
    private readonly IServiceCollection _services;
    private readonly ServiceDescriptor _descriptor;

    public DefaultHubActivatorRegistration(IServiceCollection services, ServiceDescriptor descriptor)
    {
        _services = services;
        _descriptor = descriptor;
    }

    public bool IsDefaultActivatorRegistered(Type closedHubActivatorType)
    {
        ServiceDescriptor? lastOpenGeneric = null;
        foreach (var descriptor in _services)
        {
            if (descriptor.IsKeyedService)
            {
                continue;
            }

            if (descriptor.ServiceType == closedHubActivatorType)
            {
                // A closed registration for this hub takes precedence over the open generic one.
                return false;
            }

            if (descriptor.ServiceType == typeof(IHubActivator<>))
            {
                lastOpenGeneric = descriptor;
            }
        }

        return ReferenceEquals(lastOpenGeneric, _descriptor);
    }
}
