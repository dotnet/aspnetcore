// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

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

        // Auto-detect IStringLocalizerFactory and activate localization if available.
        services.TryAddEnumerable(
            ServiceDescriptor.Transient<IConfigureOptions<ValidationOptions>, ValidationLocalizationAutoSetup>());

        return services;
    }

    /// <summary>
    /// Registers a formatter factory for a specific <see cref="System.ComponentModel.DataAnnotations.ValidationAttribute"/> type.
    /// The factory is called during localization to create an <see cref="IValidationAttributeFormatter"/>
    /// that formats the localized error message template with attribute-specific arguments.
    /// </summary>
    /// <typeparam name="TAttribute">The validation attribute type to register a formatter for.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="factory">
    /// A factory delegate that creates an <see cref="IValidationAttributeFormatter"/>
    /// from the attribute instance.
    /// </param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddValidationAttributeFormatter&lt;CreditCardAttribute&gt;(
    ///     attribute =&gt; new CreditCardAttributeFormatter(attribute));
    /// </code>
    /// </example>
    public static IServiceCollection AddValidationAttributeFormatter<TAttribute>(
        this IServiceCollection services,
        Func<TAttribute, IValidationAttributeFormatter> factory)
        where TAttribute : System.ComponentModel.DataAnnotations.ValidationAttribute
    {
        services.Configure<ValidationOptions>(options =>
            options.AttributeFormatters.AddFormatter(factory));

        return services;
    }
}
