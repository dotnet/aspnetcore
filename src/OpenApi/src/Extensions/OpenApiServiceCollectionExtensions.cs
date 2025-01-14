// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.ApiDescriptions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// OpenAPI-related methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class OpenApiServiceCollectionExtensions
{
    /// <summary>
    /// Adds OpenAPI services related to the given document name to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register services onto.</param>
    /// <param name="documentName">The name of the OpenAPI document associated with registered services.</param>
    /// <example>
    /// This method is commonly used to add OpenAPI services to the <see cref="WebApplicationBuilder.Services"/>
    /// of a <see cref="WebApplicationBuilder"/>, as shown in the following example:
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.Services.AddOpenApi("MyWebApi");
    /// </code>
    /// </example>
    public static IServiceCollection AddOpenApi(this IServiceCollection services, string documentName)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddOpenApi(documentName, _ => { });
    }

    /// <summary>
    /// Adds OpenAPI services related to the given document name to the specified <see cref="IServiceCollection"/> with the specified options.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register services onto.</param>
    /// <param name="documentName">The name of the OpenAPI document associated with registered services.</param>
    /// <param name="configureOptions">A delegate used to configure the target <see cref="OpenApiOptions"/>.</param>
    /// <example>
    /// This method is commonly used to add OpenAPI services to the <see cref="WebApplicationBuilder.Services"/>
    /// of a <see cref="WebApplicationBuilder"/>, as shown in the following example:
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.Services.AddOpenApi("MyWebApi", options => {
    ///     // Add a custom schema transformer for decimal types
    ///     options.AddSchemaTransformer(DecimalTransformer.TransformAsync);
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddOpenApi(this IServiceCollection services, string documentName, Action<OpenApiOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        // We need to register the document name in a case-insensitive manner to support case-insensitive document name resolution.
        // The document name is used to store and retrieve keyed services and configuration options, which are all case-sensitive.
        // To achieve parity with ASP.NET Core routing, which is case-insensitive, we need to ensure the document name is lowercased.
        var lowercasedDocumentName = documentName.ToLowerInvariant();

        services.AddOpenApiCore(lowercasedDocumentName);
        services.Configure<OpenApiOptions>(lowercasedDocumentName, options =>
        {
            options.DocumentName = lowercasedDocumentName;
            configureOptions(options);
        });
        return services;
    }

    /// <summary>
    /// Adds OpenAPI services related to the default document to the specified <see cref="IServiceCollection"/> with the specified options.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register services onto.</param>
    /// <param name="configureOptions">A delegate used to configure the target <see cref="OpenApiOptions"/>.</param>
    /// <example>
    /// This method is commonly used to add OpenAPI services to the <see cref="WebApplicationBuilder.Services"/>
    /// of a <see cref="WebApplicationBuilder"/>, as shown in the following example:
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.Services.AddOpenApi(options => {
    ///     // Add a custom schema transformer for decimal types
    ///     options.AddSchemaTransformer(DecimalTransformer.TransformAsync);
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddOpenApi(this IServiceCollection services, Action<OpenApiOptions> configureOptions)
            => services.AddOpenApi(OpenApiConstants.DefaultDocumentName, configureOptions);

    /// <summary>
    /// Adds OpenAPI services related to the default document to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register services onto.</param>
    /// <example>
    /// This method is commonly used to add OpenAPI services to the <see cref="WebApplicationBuilder.Services"/>
    /// of a <see cref="WebApplicationBuilder"/>, as shown in the following example:
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.Services.AddOpenApi();
    /// </code>
    /// </example>
    public static IServiceCollection AddOpenApi(this IServiceCollection services)
        => services.AddOpenApi(OpenApiConstants.DefaultDocumentName);

    private static IServiceCollection AddOpenApiCore(this IServiceCollection services, string documentName)
    {
        services.AddEndpointsApiExplorer();
        services.AddKeyedSingleton<OpenApiSchemaService>(documentName);
        services.AddKeyedSingleton<OpenApiDocumentService>(documentName);
        // Required for build-time generation
        services.AddSingleton<IDocumentProvider, OpenApiDocumentProvider>();
        // Required to resolve document names for build-time generation
        services.AddSingleton(new NamedService<OpenApiDocumentService>(documentName));
        // Required to support JSON serializations
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<JsonOptions>, OpenApiSchemaJsonOptions>());
        return services;
    }
}
