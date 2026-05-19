// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for configuring MVC data annotations using an <see cref="IMvcBuilder"/>.
/// </summary>
public static class MvcDataAnnotationsMvcCoreBuilderExtensions
{
    /// <summary>
    /// Registers MVC data annotations.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
    /// <returns>The <see cref="IMvcBuilder"/>.</returns>
    public static IMvcCoreBuilder AddDataAnnotations(this IMvcCoreBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        AddDataAnnotationsServices(builder.Services);
        return builder;
    }

    /// <summary>
    /// Adds MVC data annotations localization to the application.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
    public static IMvcCoreBuilder AddDataAnnotationsLocalization(this IMvcCoreBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return AddDataAnnotationsLocalization(builder, setupAction: null);
    }

    /// <summary>
    /// Registers an action to configure <see cref="MvcDataAnnotationsLocalizationOptions"/> for MVC data
    /// annotations localization.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
    /// <param name="setupAction">An <see cref="Action{MvcDataAnnotationsLocalizationOptions}"/>.</param>
    /// <returns>The <see cref="IMvcBuilder"/>.</returns>
    public static IMvcCoreBuilder AddDataAnnotationsLocalization(
        this IMvcCoreBuilder builder,
        Action<MvcDataAnnotationsLocalizationOptions>? setupAction)
    {
        ArgumentNullException.ThrowIfNull(builder);

        AddDataAnnotationsLocalizationServices(builder.Services, setupAction);
        return builder;
    }

    // Internal for testing.
    internal static void AddDataAnnotationsServices(IServiceCollection services)
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, MvcDataAnnotationsMvcOptionsSetup>());
        services.TryAddSingleton<IValidationAttributeAdapterProvider, ValidationAttributeAdapterProvider>();
    }

    // Internal for testing.
    internal static void AddDataAnnotationsLocalizationServices(
        IServiceCollection services,
        Action<MvcDataAnnotationsLocalizationOptions>? setupAction)
    {
        DataAnnotationsLocalizationServices.AddDataAnnotationsLocalizationServices(services, setupAction);
    }
}
