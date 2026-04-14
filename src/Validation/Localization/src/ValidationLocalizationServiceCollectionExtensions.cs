// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;
using Microsoft.Extensions.Validation.Localization;
using Microsoft.Extensions.Validation.Localization.AttributeFormatters;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Extension methods for adding validation localization services.
/// </summary>
public static class ValidationLocalizationServiceCollectionExtensions
{
    /// <summary>
    /// Adds localization support for validation error messages.
    /// Registers <see cref="T:Microsoft.Extensions.Localization.IStringLocalizerFactory"/> (via <see cref="LocalizationServiceCollectionExtensions.AddLocalization(IServiceCollection)"/>)
    /// and configures <see cref="ValidationOptions.ErrorMessageProvider"/> to use it for
    /// resolving localized error messages.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configureOptions">
    /// An optional action to configure <see cref="ValidationLocalizationOptions"/>.
    /// When <see langword="null"/>, default settings are used:
    /// error messages are looked up per declaring type using the attribute's
    /// <see cref="ValidationAttribute.ErrorMessage"/> as the resource key.
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
        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        services.AddLocalization();

        // Register built-in formatters for standard validation attributes.
        services.TryAddEnumerable(ServiceDescriptor.Singleton<
            IConfigureOptions<ValidationAttributeFormatterRegistry>, BuiltInFormatterRegistration>());

        // Register the bridge that reads ValidationLocalizationOptions and sets up IStringLocalizer-based implementations
        // of ErrorMessageProvider and DisplayNameProvider in ValidationOptions.
        services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<ValidationOptions>, ValidationLocalizationSetup>());

        return services;
    }

    /// <summary>
    /// Adds localization support for validation error messages using a shared resource type.
    /// All validation messages are looked up in the resource files associated with
    /// <typeparamref name="TResource"/>, regardless of the declaring type.
    /// </summary>
    /// <typeparam name="TResource">
    /// A marker type whose name and namespace determine the resource file location.
    /// For example, <c>SharedValidationMessages</c> resolves to
    /// <c>Resources/SharedValidationMessages.{culture}.resx</c>.
    /// </typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configureOptions">
    /// An optional action to configure <see cref="ValidationLocalizationOptions"/>.
    /// When <see langword="null"/>, default settings are used, routing all messages
    /// through the shared resource specified by <typeparamref name="TResource"/>.
    /// </param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddValidation();
    /// builder.Services.AddValidationLocalization&lt;SharedValidationMessages&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddValidationLocalization<TResource>(
        this IServiceCollection services, Action<ValidationLocalizationOptions>? configureOptions = null)
    {
        return services.AddValidationLocalization(options =>
        {
            options.LocalizerProvider = (_, factory) => factory.Create(typeof(TResource));

            configureOptions?.Invoke(options);
        });
    }

    /// <summary>
    /// Registers a formatter factory for a specific <see cref="ValidationAttribute"/> type.
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
        where TAttribute : ValidationAttribute
    {
        services.Configure<ValidationAttributeFormatterRegistry>(registry =>
            registry.AddFormatter(factory));

        return services;
    }

    /// <summary>
    /// Registers built-in formatters for standard validation attributes into the
    /// <see cref="ValidationAttributeFormatterRegistry"/>.
    /// </summary>
    private sealed class BuiltInFormatterRegistration : IConfigureOptions<ValidationAttributeFormatterRegistry>
    {
        public void Configure(ValidationAttributeFormatterRegistry registry)
        {
            registry.AddFormatter<RangeAttribute>(a => new RangeAttributeFormatter(a));
            registry.AddFormatter<MinLengthAttribute>(a => new MinLengthAttributeFormatter(a));
            registry.AddFormatter<MaxLengthAttribute>(a => new MaxLengthAttributeFormatter(a));
            registry.AddFormatter<LengthAttribute>(a => new LengthAttributeFormatter(a));
            registry.AddFormatter<StringLengthAttribute>(a => new StringLengthAttributeFormatter(a));
            registry.AddFormatter<RegularExpressionAttribute>(a => new RegularExpressionAttributeFormatter(a));
            registry.AddFormatter<FileExtensionsAttribute>(a => new FileExtensionsAttributeFormatter(a));
            registry.AddFormatter<CompareAttribute>(a => new CompareAttributeFormatter(a));
        }
    }
}
