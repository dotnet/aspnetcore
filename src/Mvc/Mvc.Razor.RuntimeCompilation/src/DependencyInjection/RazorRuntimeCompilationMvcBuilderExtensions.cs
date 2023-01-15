// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Static class that adds razor compilation extension methods.
/// </summary>
public static class RazorRuntimeCompilationMvcBuilderExtensions
{
    /// <summary>
    /// Configures <see cref="IMvcBuilder" /> to support runtime compilation of Razor views and Razor Pages.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcBuilder" />.</param>
    /// <returns>The <see cref="IMvcBuilder"/>.</returns>
    public static IMvcBuilder AddRazorRuntimeCompilation(this IMvcBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        RazorRuntimeCompilationMvcCoreBuilderExtensions.AddServices(builder.Services);
        return builder;
    }

    /// <summary>
    /// Configures <see cref="IMvcBuilder" /> to support runtime compilation of Razor views and Razor Pages.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcBuilder" />.</param>
    /// <param name="setupAction">An action to configure the <see cref="MvcRazorRuntimeCompilationOptions"/>.</param>
    /// <returns>The <see cref="IMvcBuilder"/>.</returns>
    public static IMvcBuilder AddRazorRuntimeCompilation(this IMvcBuilder builder, Action<MvcRazorRuntimeCompilationOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(setupAction);

        RazorRuntimeCompilationMvcCoreBuilderExtensions.AddServices(builder.Services);
        builder.Services.Configure(setupAction);
        return builder;
    }
}
