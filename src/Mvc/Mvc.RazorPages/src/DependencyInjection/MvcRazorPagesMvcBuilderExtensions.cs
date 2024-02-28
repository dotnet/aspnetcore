// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Resources = Microsoft.AspNetCore.Mvc.RazorPages.Resources;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions methods for configuring Razor Pages via an <see cref="IMvcBuilder"/>.
/// </summary>
public static class MvcRazorPagesMvcBuilderExtensions
{
    /// <summary>
    /// Configures a set of <see cref="RazorPagesOptions"/> for the application.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
    /// <param name="setupAction">An action to configure the <see cref="RazorPagesOptions"/>.</param>
    /// <returns>The <see cref="IMvcBuilder"/>.</returns>
    public static IMvcBuilder AddRazorPagesOptions(
        this IMvcBuilder builder,
        Action<RazorPagesOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(setupAction);

        builder.Services.Configure(setupAction);
        return builder;
    }

    /// <summary>
    /// Configures Razor Pages to use the specified <paramref name="rootDirectory"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <param name="rootDirectory">The application relative path to use as the root directory.</param>
    /// <returns>The <see cref="IMvcBuilder"/>.</returns>
    public static IMvcBuilder WithRazorPagesRoot(this IMvcBuilder builder, string rootDirectory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(rootDirectory);

        if (rootDirectory[0] != '/')
        {
            throw new ArgumentException(Resources.PathMustBeRootRelativePath, nameof(rootDirectory));
        }

        builder.Services.Configure<RazorPagesOptions>(options => options.RootDirectory = rootDirectory);
        return builder;
    }

    /// <summary>
    /// Configures Razor Pages to be rooted at the content root (<see cref="IHostingEnvironment.ContentRootPath"/>).
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <returns>The <see cref="IMvcBuilder"/>.</returns>
    public static IMvcBuilder WithRazorPagesAtContentRoot(this IMvcBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.Configure<RazorPagesOptions>(options => options.RootDirectory = "/");
        return builder;
    }
}
