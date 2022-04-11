// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.OpenApi;

public static class OpenApiServicesExtensions
{
    public static IServiceCollection AddOpenApiGenerator(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddSingleton<OpenApiGenerator>();
        return services;
    }
}
