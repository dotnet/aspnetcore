// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Endpoints.FormMapping;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.AspNetCore.Components.Mcp.FormMapping;
using Microsoft.AspNetCore.Components.Mcp.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Mcp.DependencyInjection;

internal static class McpComponentServiceCollectionExtensions
{
    private static readonly object HttpDefaultServiceKey = new();

    public static void AddMcpComponentServices(this IServiceCollection services)
    {
        if (services.Any(descriptor => descriptor.ServiceType == typeof(McpServicesMarker)))
        {
            return;
        }

        services.AddRazorComponents();
        services.AddCascadingAuthenticationState();
        services.AddSingleton<McpServicesMarker>();
        services.AddScoped<McpOperationContext>();
        services.AddScoped<McpComponentHost>();
        services.AddScoped(provider => new McpComponentRequestContext(provider.GetRequiredService<McpOperationContext>()));
        services.AddCascadingValue(provider => provider.GetRequiredService<McpComponentRequestContext>());
        services.AddScoped<McpFormDataProvider>();
        services.AddScoped(provider =>
        {
            var options = provider.GetRequiredService<IOptions<RazorComponentsServiceOptions>>().Value;
            return new FormDataMapperOptions(provider.GetRequiredService<ILoggerFactory>())
            {
                MaxRecursionDepth = options.MaxFormMappingRecursionDepth,
                MaxErrorCount = options.MaxFormMappingErrorCount,
                MaxCollectionSize = options.MaxFormMappingCollectionSize,
                MaxKeyBufferSize = options.MaxFormMappingKeySize,
            };
        });
        AddOperationService<NavigationManager, McpNavigationManager>(services);
        AddOperationService<IFormValueMapper, McpFormValueMapper>(services);
        AddOperationService<AuthenticationStateProvider, McpAuthenticationStateProvider>(services);
    }

    private static void AddOperationService<TService, TImplementation>(IServiceCollection services)
        where TService : class
        where TImplementation : class, TService
    {
        var original = services.Last(descriptor => !descriptor.IsKeyedService && descriptor.ServiceType == typeof(TService));
        if (original.Lifetime != ServiceLifetime.Scoped)
        {
            throw new InvalidOperationException($"The component service '{typeof(TService)}' must be scoped to support independent HTTP and MCP rendering operations.");
        }

        services.Remove(original);
        if (original.ImplementationType is { } implementationType)
        {
            services.Add(new ServiceDescriptor(typeof(TService), HttpDefaultServiceKey, implementationType, original.Lifetime));
        }
        else
        {
            var factory = original.ImplementationFactory
                ?? throw new InvalidOperationException($"The scoped component service '{typeof(TService)}' has no implementation factory.");
            services.Add(new ServiceDescriptor(typeof(TService), HttpDefaultServiceKey, (provider, _) => factory(provider), original.Lifetime));
        }

        services.TryAddScoped<TImplementation>();
        services.AddScoped<TService>(provider => provider.GetRequiredService<McpOperationContext>().IsActive
            ? provider.GetRequiredService<TImplementation>()
            : provider.GetRequiredKeyedService<TService>(HttpDefaultServiceKey));
    }

    private sealed class McpServicesMarker;
}
