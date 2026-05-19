// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.DataAnnotations;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring MVC data annotations localization.
/// </summary>
public static class MvcDataAnnotationsMvcBuilderExtensions
{
    /// <summary>
    /// Adds MVC data annotations localization to the application.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
    /// <returns>The <see cref="IMvcBuilder"/>.</returns>
    public static IMvcBuilder AddDataAnnotationsLocalization(this IMvcBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return AddDataAnnotationsLocalization(builder, setupAction: null);
    }

    /// <summary>
    /// Adds MVC data annotations localization to the application.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
    /// <param name="setupAction">The action to configure <see cref="MvcDataAnnotationsLocalizationOptions"/>.
    /// </param>
    /// <returns>The <see cref="IMvcBuilder"/>.</returns>
    public static IMvcBuilder AddDataAnnotationsLocalization(
        this IMvcBuilder builder,
        Action<MvcDataAnnotationsLocalizationOptions>? setupAction)
    {
        ArgumentNullException.ThrowIfNull(builder);

        DataAnnotationsLocalizationServices.AddDataAnnotationsLocalizationServices(
            builder.Services,
            setupAction);

        return builder;
    }
}
