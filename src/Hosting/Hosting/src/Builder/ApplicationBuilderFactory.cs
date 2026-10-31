// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Hosting.Builder;

/// <summary>
/// A factory for creating <see cref="IApplicationBuilder" /> instances.
/// </summary>
public class ApplicationBuilderFactory : IApplicationBuilderFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initialize a new factory instance with an <see cref="IServiceProvider" />.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to resolve dependencies and initialize components.</param>
    public ApplicationBuilderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Create an <see cref="IApplicationBuilder" /> builder given a <paramref name="serverFeatures" />.
    /// </summary>
    /// <param name="serverFeatures">An <see cref="IFeatureCollection"/> of HTTP features.</param>
    /// <returns>An <see cref="IApplicationBuilder"/> configured with <paramref name="serverFeatures"/>.</returns>
    public IApplicationBuilder CreateBuilder(IFeatureCollection serverFeatures)
    {
        return new ApplicationBuilder(_serviceProvider, serverFeatures);
    }
}
