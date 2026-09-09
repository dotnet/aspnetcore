// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DojoAgent;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace DojoClient.E2E.Tests.ServiceOverrides;

internal sealed class DojoModelOverrides
{
    public static void ConfigureApi(IServiceCollection services)
    {
        AddRunStore(services);
        services.AddScoped<IChatClient, RunSelectedChatClient>();
        services.AddKeyedScoped<IChatClient>(
            ChatClientAgentFactory.PredictiveStateUpdatesServiceKey,
            (sp, _) => new RunSelectedChatClient(sp.GetRequiredService<DojoRunStore>(), predictive: true));
    }

    public static void ConfigureUI(IServiceCollection services)
    {
        var isDirect = Environment.GetEnvironmentVariable("DOJO_BACKEND") == "Direct";
        if (isDirect)
        {
            if (!services.Any(service => service.ServiceType == typeof(IChatClient) &&
                Equals(service.ServiceKey, ChatClientAgentFactory.ModelServiceKey)))
            {
                throw new InvalidOperationException("The direct dojo model registration is missing.");
            }

            AddRunStore(services);
            services.AddKeyedScoped<IChatClient>(ChatClientAgentFactory.ModelServiceKey,
                (sp, _) => new RunSelectedChatClient(sp.GetRequiredService<DojoRunStore>()));
            services.AddKeyedScoped<IChatClient>(ChatClientAgentFactory.PredictiveStateUpdatesServiceKey,
                (sp, _) => new RunSelectedChatClient(sp.GetRequiredService<DojoRunStore>(), predictive: true));
        }

        // Decorate UI-facing clients, not the underlying model registrations. NavigationManager
        // supplies the circuit's test ID without relying on HttpContext during interactive rendering.
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
                    ?? throw new InvalidOperationException("Expected a factory for the dojo scenario client.");
                services[index] = ServiceDescriptor.DescribeKeyed(typeof(IChatClient), descriptor.ServiceKey,
                    (sp, key) => new RunForwardingChatClient(
                        (IChatClient)factory(sp, key), sp.GetRequiredService<NavigationManager>(), isDirect),
                    descriptor.Lifetime);
            }
            else
            {
                var factory = descriptor.ImplementationFactory
                    ?? throw new InvalidOperationException("Expected a factory for the dojo chat client.");
                services[index] = ServiceDescriptor.Describe(typeof(IChatClient),
                    sp => new RunForwardingChatClient(
                        (IChatClient)factory(sp), sp.GetRequiredService<NavigationManager>(), isDirect),
                    descriptor.Lifetime);
            }
        }
    }

    private static void AddRunStore(IServiceCollection services)
    {
        services.AddSingleton<DojoRunStore>();
        services.AddTransient<IStartupFilter, DojoRunStartupFilter>();
    }
}
