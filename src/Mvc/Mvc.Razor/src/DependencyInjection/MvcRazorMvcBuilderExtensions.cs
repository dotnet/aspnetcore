// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions methods for configuring MVC via an <see cref="IMvcBuilder"/>.
/// </summary>
public static class MvcRazorMvcBuilderExtensions
{
    /// <summary>
    /// Configures a set of <see cref="RazorViewEngineOptions"/> for the application.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
    /// <param name="setupAction">An action to configure the <see cref="RazorViewEngineOptions"/>.</param>
    /// <returns>The <see cref="IMvcBuilder"/>.</returns>
    public static IMvcBuilder AddRazorOptions(
        this IMvcBuilder builder,
        Action<RazorViewEngineOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(setupAction);

        builder.Services.Configure(setupAction);
        return builder;
    }

    /// <summary>
    /// Registers tag helpers as services and replaces the existing <see cref="ITagHelperActivator"/>
    /// with an <see cref="ServiceBasedTagHelperActivator"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcBuilder"/> instance this method extends.</param>
    /// <returns>The <see cref="IMvcBuilder"/> instance this method extends.</returns>
    public static IMvcBuilder AddTagHelpersAsServices(this IMvcBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        TagHelpersAsServices.AddTagHelpersAsServices(builder.PartManager, builder.Services);
        return builder;
    }

    /// <summary>
    /// Adds an initialization callback for a given <typeparamref name="TTagHelper"/>.
    /// </summary>
    /// <remarks>
    /// The callback will be invoked on any <typeparamref name="TTagHelper"/> instance before the
    /// <see cref="ITagHelperComponent.ProcessAsync(TagHelperContext, TagHelperOutput)"/> method is called.
    /// </remarks>
    /// <typeparam name="TTagHelper">The type of <see cref="ITagHelper"/> being initialized.</typeparam>
    /// <param name="builder">The <see cref="IMvcBuilder"/> instance this method extends.</param>
    /// <param name="initialize">An action to initialize the <typeparamref name="TTagHelper"/>.</param>
    /// <returns>The <see cref="IMvcBuilder"/> instance this method extends.</returns>
    public static IMvcBuilder InitializeTagHelper<TTagHelper>(
        this IMvcBuilder builder,
        Action<TTagHelper, ViewContext> initialize)
        where TTagHelper : ITagHelper
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(initialize);

        var initializer = new TagHelperInitializer<TTagHelper>(initialize);

        builder.Services.AddSingleton(typeof(ITagHelperInitializer<TTagHelper>), initializer);

        return builder;
    }
}
