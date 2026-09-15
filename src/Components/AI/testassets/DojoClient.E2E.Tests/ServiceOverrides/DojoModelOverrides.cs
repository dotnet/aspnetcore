// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DojoAgent;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DojoClient.E2E.Tests.ServiceOverrides;

internal sealed class DojoModelOverrides
{
    public static void ConfigureApi(IServiceCollection services)
    {
        AddRunStore(services, DojoBackendKind.AGUI);
        services.AddScoped<IChatClient, RunSelectedChatClient>();
        services.AddKeyedScoped<IChatClient>(
            ChatClientAgentFactory.PredictiveStateUpdatesServiceKey,
            (sp, _) => new RunSelectedChatClient(sp.GetRequiredService<DojoRunStore>(), predictive: true));
    }

    public static void ConfigureAguiUI(IServiceCollection services) => ConfigureUI(services, DojoBackendKind.AGUI);

    public static void ConfigureDirectUI(IServiceCollection services) => ConfigureUI(services, DojoBackendKind.Direct);

    internal static void ConfigureUI(IServiceCollection services, DojoBackendKind backend)
    {
        if (backend == DojoBackendKind.Direct)
        {
            if (!services.Any(service => service.ServiceType == typeof(IChatClient) &&
                Equals(service.ServiceKey, ChatClientAgentFactory.ModelServiceKey)))
            {
                throw new InvalidOperationException("The direct dojo model registration is missing.");
            }

            AddRunStore(services, backend);
            services.AddKeyedScoped<IChatClient>(ChatClientAgentFactory.ModelServiceKey,
                (sp, _) => new RunSelectedChatClient(sp.GetRequiredService<DojoRunStore>()));
            services.AddKeyedScoped<IChatClient>(ChatClientAgentFactory.PredictiveStateUpdatesServiceKey,
                (sp, _) => new RunSelectedChatClient(sp.GetRequiredService<DojoRunStore>(), predictive: true));
        }

        for (var index = 0; index < services.Count; index++)
        {
            var descriptor = services[index];
            if (descriptor.ServiceType != typeof(IChatClient) ||
                descriptor.ServiceKey is ChatClientAgentFactory.ModelServiceKey or
                    ChatClientAgentFactory.PredictiveStateUpdatesServiceKey)
            {
                continue;
            }

            if (descriptor.IsKeyedService)
            {
                var factory = descriptor.KeyedImplementationFactory
                    ?? throw UnsupportedRegistration(descriptor);
                services[index] = ServiceDescriptor.DescribeKeyed(typeof(IChatClient), descriptor.ServiceKey,
                    (sp, key) => new RunForwardingChatClient(
                        (IChatClient)factory(sp, key), sp.GetRequiredService<NavigationManager>(), backend),
                    descriptor.Lifetime);
            }
            else
            {
                var factory = descriptor.ImplementationFactory
                    ?? throw UnsupportedRegistration(descriptor);
                services[index] = ServiceDescriptor.Describe(typeof(IChatClient),
                    sp => new RunForwardingChatClient(
                        (IChatClient)factory(sp), sp.GetRequiredService<NavigationManager>(), backend),
                    descriptor.Lifetime);
            }
        }
    }

    private static void AddRunStore(IServiceCollection services, DojoBackendKind backend)
    {
        services.AddSingleton(sp => new DojoRunStore(sp.GetRequiredService<IConfiguration>(), backend));
        services.AddTransient<IStartupFilter, DojoRunStartupFilter>();
    }

    private static InvalidOperationException UnsupportedRegistration(ServiceDescriptor descriptor)
    {
        var implementationType = descriptor.IsKeyedService
            ? descriptor.KeyedImplementationType ?? descriptor.KeyedImplementationInstance?.GetType()
            : descriptor.ImplementationType ?? descriptor.ImplementationInstance?.GetType();

        return new InvalidOperationException(
            $"Cannot decorate {nameof(IChatClient)} registration with key '{descriptor.ServiceKey ?? "<default>"}', " +
            $"implementation '{implementationType?.FullName ?? "<unknown>"}', lifetime '{descriptor.Lifetime}'. " +
            "Dojo UI clients must use factory registrations.");
    }
}
