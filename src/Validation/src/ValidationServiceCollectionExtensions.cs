// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;
using Microsoft.Extensions.Validation.Localization;

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
    public static IServiceCollection AddValidation(this IServiceCollection services, Action<ValidationOptions>? configureOptions = null)
    {
        services.Configure<ValidationOptions>(options =>
        {
            if (configureOptions is not null)
            {
                configureOptions(options);
            }
            // Support ParameterInfo resolution at runtime
#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            options.Resolvers.Add(new RuntimeValidatableParameterInfoResolver());
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        });
        return services;
    }


    /// <summary>
    /// Adds localization support for validation error messages.
    /// Registers <see cref="IStringLocalizerFactory"/> (via <see cref="LocalizationServiceCollectionExtensions.AddLocalization(IServiceCollection)"/>)
    /// and configures <see cref="ValidationOptions.ErrorMessageProvider"/> to use it for
    /// resolving localized error messages.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configureOptions">
    /// An optional action to configure <see cref="ValidationLocalizationOptions"/>.
    /// When <see langword="null"/>, default settings are used:
    /// error messages are looked up per declaring type using the attribute's
    /// error message template as the resource key.
    /// </param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// // Default: per-type resource files, template-based keys
    /// builder.Services.AddValidation();
    /// builder.Services.AddValidationLocalization();
    ///
    /// // Shared resource file
    /// builder.Services.AddValidation();
    /// builder.Services.AddValidationLocalization(options =&gt;
    /// {
    ///     options.LocalizerProvider = (type, factory) =&gt;
    ///         factory.Create(typeof(SharedValidationMessages));
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddValidationLocalization(this IServiceCollection services, Action<ValidationLocalizationOptions>? configureOptions = null)
    {
        services.AddLocalization();
        services.TryAddSingleton<IAttributeArgumentProvider, DefaultAttributeArgumentProvider>();

        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        // Register the bridge that reads ValidationLocalizationOptions and wires up ValidationOptions.ErrorMessageProvider (and optionally DisplayNameResolver).
        services.TryAddTransient<IConfigureOptions<ValidationOptions>, ValidationLocalizationConfigureOptions>();

        return services;
    }
}
