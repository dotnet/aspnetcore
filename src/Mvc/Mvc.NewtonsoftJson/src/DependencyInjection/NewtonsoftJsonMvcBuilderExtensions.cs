// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions methods for configuring MVC via an <see cref="IMvcBuilder"/>.
/// </summary>
public static class NewtonsoftJsonMvcBuilderExtensions
{
    /// <summary>
    /// Configures Newtonsoft.Json specific features such as input and output formatters.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
    /// <returns>The <see cref="IMvcBuilder"/>.</returns>
    public static IMvcBuilder AddNewtonsoftJson(this IMvcBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        NewtonsoftJsonMvcCoreBuilderExtensions.AddServicesCore(builder.Services);
        return builder;
    }

    /// <summary>
    /// Configures Newtonsoft.Json specific features such as input and output formatters.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
    /// <param name="setupAction">Callback to configure <see cref="MvcNewtonsoftJsonOptions"/>.</param>
    /// <returns>The <see cref="IMvcBuilder"/>.</returns>
    public static IMvcBuilder AddNewtonsoftJson(
        this IMvcBuilder builder,
        Action<MvcNewtonsoftJsonOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(setupAction);

        NewtonsoftJsonMvcCoreBuilderExtensions.AddServicesCore(builder.Services);
        builder.Services.Configure(setupAction);

        return builder;
    }
}
