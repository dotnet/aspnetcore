// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring JSON options for components.
/// </summary>
public static class JsonServiceCollectionExtensions
{
    /// <summary>
    /// Configures options used for serializing JSON in components functionality.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to configure options on.</param>
    /// <param name="configureOptions">The <see cref="Action{JsonOptions}"/> to configure the <see cref="JsonOptions"/>.</param>
    /// <returns>The modified <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection ConfigureComponentsJsonOptions(this IServiceCollection services, Action<JsonOptions> configureOptions)
    {
        services.Configure(configureOptions);
        return services;
    }
}
