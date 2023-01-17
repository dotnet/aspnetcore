// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing.Matching;

internal sealed class DfaMatcherFactory : MatcherFactory
{
    private readonly IServiceProvider _services;

    // Using the service provider here so we can avoid coupling to the dependencies
    // of DfaMatcherBuilder.
    public DfaMatcherFactory(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _services = services;
    }

    public override Matcher CreateMatcher(EndpointDataSource dataSource)
    {
        ArgumentNullException.ThrowIfNull(dataSource);

        // Creates a tracking entry in DI to stop listening for change events
        // when the services are disposed.
        var lifetime = _services.GetRequiredService<DataSourceDependentMatcher.Lifetime>();

        return new DataSourceDependentMatcher(dataSource, lifetime, _services.GetRequiredService<DfaMatcherBuilder>);
    }
}
