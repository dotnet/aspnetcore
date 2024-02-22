// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.ApiDescriptions;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// OpenAPI-related methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Adds OpenAPI services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddOpenApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSingleton<OpenApiComponentService>();
        services.AddTransient<OpenApiDocumentService>();
        services.AddSingleton<IDocumentProvider, OpenApiDocumentProvider>();
        return services;
    }
}
