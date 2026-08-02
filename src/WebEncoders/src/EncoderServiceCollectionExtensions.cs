// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up web encoding services in an <see cref="IServiceCollection" />.
/// </summary>
public static class EncoderServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="HtmlEncoder"/>, <see cref="JavaScriptEncoder"/> and <see cref="UrlEncoder"/>
    /// to the specified <paramref name="services" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddWebEncoders(this IServiceCollection services)
    {
        ArgumentNullThrowHelper.ThrowIfNull(services);

        services.AddOptions();

        // Register the default encoders
        // We want to call the 'Default' property getters lazily since they perform static caching
        services.TryAddSingleton(
            CreateFactory(() => HtmlEncoder.Default, HtmlEncoder.Create));
        services.TryAddSingleton(
            CreateFactory(() => JavaScriptEncoder.Default, JavaScriptEncoder.Create));
        services.TryAddSingleton(
            CreateFactory(() => UrlEncoder.Default, UrlEncoder.Create));

        return services;
    }

    /// <summary>
    /// Adds <see cref="HtmlEncoder"/>, <see cref="JavaScriptEncoder"/> and <see cref="UrlEncoder"/>
    /// to the specified <paramref name="services" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="setupAction">An <see cref="Action{WebEncoderOptions}"/> to configure the provided <see cref="WebEncoderOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddWebEncoders(this IServiceCollection services, Action<WebEncoderOptions> setupAction)
    {
        ArgumentNullThrowHelper.ThrowIfNull(services);
        ArgumentNullThrowHelper.ThrowIfNull(setupAction);

        services.AddWebEncoders();
        services.Configure(setupAction);

        return services;
    }

    private static Func<IServiceProvider, TService> CreateFactory<TService>(
        Func<TService> defaultFactory,
        Func<TextEncoderSettings, TService> customSettingsFactory)
    {
        return serviceProvider =>
        {
            var settings = serviceProvider
                ?.GetService<IOptions<WebEncoderOptions>>()
                ?.Value
                ?.TextEncoderSettings;
            return (settings != null) ? customSettingsFactory(settings) : defaultFactory();
        };
    }
}
