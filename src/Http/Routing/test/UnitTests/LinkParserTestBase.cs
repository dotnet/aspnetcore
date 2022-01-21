// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

public abstract class LinkParserTestBase
{
    protected ServiceCollection GetBasicServices()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddRouting();
        services.AddLogging();
        return services;
    }

    protected virtual void AddAdditionalServices(IServiceCollection services)
    {
    }

    private protected DefaultLinkParser CreateLinkParser(params Endpoint[] endpoints)
    {
        return CreateLinkParser(configureServices: null, endpoints);
    }

    private protected DefaultLinkParser CreateLinkParser(
        Action<IServiceCollection> configureServices,
        params Endpoint[] endpoints)
    {
        return CreateLinkParser(configureServices, new[] { new DefaultEndpointDataSource(endpoints ?? Array.Empty<Endpoint>()) });
    }

    private protected DefaultLinkParser CreateLinkParser(EndpointDataSource[] dataSources)
    {
        return CreateLinkParser(configureServices: null, dataSources);
    }

    private protected DefaultLinkParser CreateLinkParser(
        Action<IServiceCollection> configureServices,
        EndpointDataSource[] dataSources)
    {
        var services = GetBasicServices();
        AddAdditionalServices(services);
        configureServices?.Invoke(services);

        services.Configure<RouteOptions>(o =>
        {
            if (dataSources != null)
            {
                foreach (var dataSource in dataSources)
                {
                    o.EndpointDataSources.Add(dataSource);
                }
            }
        });

        var serviceProvider = services.BuildServiceProvider();
        var routeOptions = serviceProvider.GetRequiredService<IOptions<RouteOptions>>();

        return new DefaultLinkParser(
            new DefaultParameterPolicyFactory(routeOptions, serviceProvider),
            new CompositeEndpointDataSource(routeOptions.Value.EndpointDataSources),
            serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<DefaultLinkParser>(),
            serviceProvider);
    }
}
