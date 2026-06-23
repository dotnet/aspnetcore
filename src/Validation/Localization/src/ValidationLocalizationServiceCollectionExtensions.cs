// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;
using Microsoft.Extensions.Validation.Localization;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering the default <see cref="IStringLocalizer"/>-based validation
/// localizer.
/// </summary>
public static class ValidationLocalizationServiceCollectionExtensions
{
    /// <summary>
    /// Adds the default <see cref="IStringLocalizer"/>-based validation localizer to the
    /// service collection and wires it up by setting <see cref="ValidationOptions.Localizer"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Internally calls <see cref="LocalizationServiceCollectionExtensions.AddLocalization(IServiceCollection)"/>
    /// to ensure an <see cref="IStringLocalizerFactory"/> is registered, and registers an
    /// <see cref="IConfigureOptions{TOptions}"/> bridge that sets
    /// <see cref="ValidationOptions.Localizer"/> to a default
    /// instance (only when <see cref="ValidationOptions.Localizer"/> has not already been set).
    /// </para>
    /// <para>
    /// Call <c>AddValidation()</c> separately to register the validation pipeline itself.
    /// Call order does not matter.
    /// </para>
    /// <para>
    /// <b>Minimal API parameter validation:</b> top-level Minimal API parameters do not have
    /// a declaring type.
    /// For applications that validate Minimal API parameters, prefer the
    /// <see cref="AddValidationLocalization{TResource}(IServiceCollection, Action{ValidationLocalizationOptions})"/> overload (shared-resource pattern),
    /// or set <see cref="ValidationLocalizationOptions.LocalizerProvider"/> explicitly to a
    /// delegate that does not depend on the declaring type (which is passed as
    /// <see langword="null"/> in the parameter case).
    /// </para>
    /// <para>
    /// The default <see cref="IStringLocalizerFactory"/> registered by
    /// <c>AddLocalization()</c> reads strings from .resx resource files. To localize against
    /// other sources (databases, JSON files, in-memory dictionaries, third-party translation
    /// services), register your own <see cref="IStringLocalizerFactory"/> implementation
    /// either before or after <see cref="AddValidationLocalization(IServiceCollection, Action{ValidationLocalizationOptions})"/>.
    /// The validation localizer resolves the factory at validation time, so registration order
    /// does not matter:
    /// </para>
    /// <example>
    /// <code>
    /// builder.Services.AddValidation();
    /// builder.Services.AddValidationLocalization();
    /// builder.Services.AddSingleton&lt;IStringLocalizerFactory, MyDatabaseLocalizerFactory&gt;();
    /// </code>
    /// </example>
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configureOptions">An optional callback to configure
    /// <see cref="ValidationLocalizationOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddValidationLocalization(
        this IServiceCollection services,
        Action<ValidationLocalizationOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        services.AddLocalization();

        // Register the bridge that reads ValidationLocalizationOptions and sets up IStringLocalizer-based implementation
        // of IValidationLocalizer.
        services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<ValidationOptions>, ValidationLocalizationSetup>());

        return services;
    }

    /// <summary>
    /// Adds the default <see cref="IStringLocalizer"/>-based validation localizer configured to
    /// resolve localized strings against the resource type <typeparamref name="TResource"/>
    /// for all types being validated.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this when validation messages live in a single shared resource file rather than
    /// per-type resource files.
    /// </para>
    /// <para>
    /// Equivalent to setting <see cref="ValidationLocalizationOptions.LocalizerProvider"/> to
    /// <c>(_, factory) =&gt; factory.Create(typeof(TResource))</c>, but the configured provider
    /// resolves the <see cref="IStringLocalizer"/> once and reuses the same instance for every
    /// declaring type, avoiding repeated factory lookups.
    /// </para>
    /// </remarks>
    /// <typeparam name="TResource">The type that identifies the shared resource source.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configureOptions">An optional callback to further configure
    /// <see cref="ValidationLocalizationOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddValidationLocalization<TResource>(
        this IServiceCollection services,
        Action<ValidationLocalizationOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddValidationLocalization(options =>
        {
            // Cache the resolved IStringLocalizer once via thread-safe lazy initialization.
            IStringLocalizer? sharedLocalizer = null;
            options.LocalizerProvider = (_, factory) =>
                LazyInitializer.EnsureInitialized(
                    ref sharedLocalizer,
                    () => factory.Create(typeof(TResource)));

            configureOptions?.Invoke(options);
        });
    }
}
