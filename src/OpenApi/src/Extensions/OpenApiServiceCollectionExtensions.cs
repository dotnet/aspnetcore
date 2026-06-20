// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.ApiDescriptions;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OpenApiConstants = Microsoft.AspNetCore.OpenApi.OpenApiConstants;

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

        services.AddOpenApiCore();

        // Required to resolve document names for build-time generation
        services.AddSingleton(new NamedService<OpenApiDocumentService>(lowercasedDocumentName));

        services.Configure<OpenApiOptions>(lowercasedDocumentName, configureOptions);
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

    /// <summary>
    /// Adds the core OpenAPI services, not tied to a specific document name, to the specified <see cref="IServiceCollection"/> with the specified options.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register services onto.</param>
    public static IServiceCollection AddOpenApiCore(this IServiceCollection services)
    {
        // NOTE, we don't have a public AddOpenApiCore yet that allows users to configure options.
        // Today, callers can do services.Configure<OpenApiOptions>(...) themselves.
        // We can consider in the future if we want to add an overload of AddOpenApiCore that
        // takes in an Action<OpenApiOptions> to allow users to configure core OpenAPI options
        // that aren't specific to a document.
        // If we did that in the future, we should decide what option we want to go:
        // 1. ensure that such configuration callback called **BEFORE** the document-specific configuration callbacks.
        // 2. ensure that the global configuration is only applied to document names that are not explicitly registered.
        // Note that services.Configure<OpenApiOptions>(null, ...) will make the callback apply to all document names.
        // But also it's dependent on the order of registration.
        // To avoid answering this potential design question, we are not shipping the overload with configureOptions.
        // We are waiting for user feedback to understand if there is really a need to ship it and what the behavior should be.
        ArgumentNullException.ThrowIfNull(services);

        services.AddEndpointsApiExplorer();
        services.TryAddKeyedSingleton<OpenApiSchemaService>(KeyedService.AnyKey);
        services.TryAddKeyedSingleton<OpenApiDocumentService>(KeyedService.AnyKey);
        services.TryAddKeyedSingleton<IOpenApiDocumentProvider, OpenApiDocumentService>(KeyedService.AnyKey);

        // Required for build-time generation
        services.TryAddSingleton<IDocumentProvider, OpenApiDocumentProvider>();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<OpenApiOptions>, ConfigureNamedOpenApiOptions>());

        // Required to support JSON serializations
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<JsonOptions>, OpenApiSchemaJsonOptions>());

        // Historically before we added AddOpenApiCore, we used to always configure OpenApiOptions.
        // This means that users could be relying already on things like GetRequiredService<IOptionsMonitor<OpenApiOptions>>
        // To ensure that GetRequiredService can find a service to return, we must add a configuration for OpenApiOptions.
        services.Configure<OpenApiOptions>(null, options => { });
        return services;
    }

    private sealed class ConfigureNamedOpenApiOptions : IConfigureNamedOptions<OpenApiOptions>
    {
        public void Configure(string? name, OpenApiOptions options)
            => options.DocumentName = name ?? throw new UnreachableException();

        public void Configure(OpenApiOptions options)
            => throw new UnreachableException();
    }
}
