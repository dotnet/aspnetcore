// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Web.Infrastructure;

/// <summary>
/// Extension methods for configuring web-specific JSON options for components.
/// </summary>
public static class JsonOptionsServiceCollectionExtensions
{
    /// <summary>
    /// Configures options used for serializing JSON in web-specific components functionality.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to configure options on.</param>
    /// <returns>The modified <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection ConfigureComponentsWebJsonOptions(this IServiceCollection services)
    {
        services.ConfigureComponentsJsonOptions(static options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, WebRendererJsonSerializerContext.Default);
        });

        return services;
    }
}
