// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    /// This feature depends on a compile-time source generator, which might not be generate validatable type info for all types that
    /// you might expect to be validated. It's the application developer responsibility to ensure that validation is working.
    /// A skipped validation is **not** a security vulnerability and is a known limitation of the feature.
    /// </remarks>
    public static IServiceCollection AddValidation(this IServiceCollection services, Action<ValidationOptions>? configureOptions = null)
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
