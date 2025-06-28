// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Validation;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding validation services.
/// </summary>
public static class ValidationServiceCollectionExtensions
{
    /// <summary>
    /// Adds the validation services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
    /// <param name="configureOptions">An optional action to configure the <see cref="ValidationOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection" /> for chaining.</returns>
    /// <remarks>
    /// This API enables both the source-generated and runtime-based implementation of the built-in validation resolver.
    /// It is not recommended for use in applications where native AoT compat is required. In those
    /// scenarios, it is recommend to use <see cref="ValidationServiceCollectionExtensions.AddValidationCore(IServiceCollection, Action{ValidationOptions}?)" />.
    /// </remarks>
    [RequiresUnreferencedCode("AddValidation enables the RuntimeValidatableInfoResolver by default which is not compatible with trimming or AOT compilation.")]
    public static IServiceCollection AddValidation(this IServiceCollection services, Action<ValidationOptions>? configureOptions = null)
    {
        services.Configure<ValidationOptions>(options =>
        {
            if (configureOptions is not null)
            {
                configureOptions(options);
            }
            // Support both ParameterInfo and TypeInfo resolution at runtime
#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            options.Resolvers.Add(new RuntimeValidatableInfoResolver());
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        });
        return services;
    }

    /// <summary>
    /// Adds the validation services to the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add the services to.</param>
    /// <param name="configureOptions">An optional action to configure the <see cref="ValidationOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection" /> for chaining.</returns>
    /// <remarks>
    /// This API only enables the source generator-based implementation of the built-in validation resolver, by default
    /// and is recommended for use in applications where native AoT compat is required.
    /// </remarks>
    public static IServiceCollection AddValidationCore(this IServiceCollection services, Action<ValidationOptions>? configureOptions = null)
    {
        services.Configure<ValidationOptions>(options =>
        {
            if (configureOptions is not null)
            {
                configureOptions(options);
            }
        });
        return services;
    }
}
