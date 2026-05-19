// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for configuring MVC using an <see cref="IMvcCoreBuilder"/>.
/// </summary>
public static class MvcCoreMvcCoreBuilderExtensions
{
    /// <summary>
    /// Registers an action to configure <see cref="MvcOptions"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <param name="setupAction">An <see cref="Action{MvcOptions}"/>.</param>
    /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
    public static IMvcCoreBuilder AddMvcOptions(
        this IMvcCoreBuilder builder,
        Action<MvcOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(setupAction);

        builder.Services.Configure(setupAction);
        return builder;
    }

    /// <summary>
    /// Configures <see cref="JsonOptions"/> for the specified <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
    /// <param name="configure">An <see cref="Action"/> to configure the <see cref="JsonOptions"/>.</param>
    /// <returns>The <see cref="IMvcBuilder"/>.</returns>
    public static IMvcCoreBuilder AddJsonOptions(
        this IMvcCoreBuilder builder,
        Action<JsonOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.Configure(configure);
        return builder;
    }

    /// <summary>
    /// Adds services to support <see cref="FormatterMappings"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
    /// <returns>The <see cref="IMvcBuilder"/>.</returns>
    public static IMvcCoreBuilder AddFormatterMappings(this IMvcCoreBuilder builder)
    {
        AddFormatterMappingsServices(builder.Services);
        return builder;
    }

    /// <summary>
    /// Configures <see cref="FormatterMappings"/> for the specified <paramref name="setupAction"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <param name="setupAction">An <see cref="Action"/> to configure the <see cref="FormatterMappings"/>.</param>
    /// <returns>The <see cref="IMvcBuilder"/>.</returns>
    public static IMvcCoreBuilder AddFormatterMappings(
        this IMvcCoreBuilder builder,
        Action<FormatterMappings> setupAction)
    {
        AddFormatterMappingsServices(builder.Services);

        if (setupAction != null)
        {
            builder.Services.Configure<MvcOptions>((options) => setupAction(options.FormatterMappings));
        }

        return builder;
    }

    // Internal for testing.
    internal static void AddFormatterMappingsServices(IServiceCollection services)
    {
        services.TryAddSingleton<FormatFilter, FormatFilter>();
    }

    /// <summary>
    /// Configures authentication and authorization services for <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
    public static IMvcCoreBuilder AddAuthorization(this IMvcCoreBuilder builder)
    {
        AddAuthorizationServices(builder.Services);
        return builder;
    }

    /// <summary>
    /// Configures authentication and authorization services for <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <param name="setupAction">An <see cref="Action"/> to configure the <see cref="AuthorizationOptions"/>.</param>
    /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
    public static IMvcCoreBuilder AddAuthorization(
        this IMvcCoreBuilder builder,
        Action<AuthorizationOptions> setupAction)
    {
        AddAuthorizationServices(builder.Services);

        if (setupAction != null)
        {
            builder.Services.Configure(setupAction);
        }

        return builder;
    }

    // Internal for testing.
    internal static void AddAuthorizationServices(IServiceCollection services)
    {
        services.AddAuthenticationCore();
        services.AddAuthorization();

        services.TryAddEnumerable(
            ServiceDescriptor.Transient<IApplicationModelProvider, AuthorizationApplicationModelProvider>());
    }

    /// <summary>
    /// Registers discovered controllers as services in the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
    public static IMvcCoreBuilder AddControllersAsServices(this IMvcCoreBuilder builder)
    {
        var feature = new ControllerFeature();
        builder.PartManager.PopulateFeature(feature);

        foreach (var controller in feature.Controllers.Select(c => c.AsType()))
        {
            builder.Services.TryAddTransient(controller, controller);
        }

        builder.Services.Replace(ServiceDescriptor.Transient<IControllerActivator, ServiceBasedControllerActivator>());

        return builder;
    }

    /// <summary>
    /// Adds an <see cref="ApplicationPart"/> to the list of <see cref="ApplicationPartManager.ApplicationParts"/> on the
    /// <see cref="IMvcCoreBuilder.PartManager"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <param name="assembly">The <see cref="Assembly"/> of the <see cref="ApplicationPart"/>.</param>
    /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
    public static IMvcCoreBuilder AddApplicationPart(this IMvcCoreBuilder builder, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(assembly);

        builder.ConfigureApplicationPartManager(manager =>
        {
            var partFactory = ApplicationPartFactory.GetApplicationPartFactory(assembly);
            foreach (var applicationPart in partFactory.GetApplicationParts(assembly))
            {
                manager.ApplicationParts.Add(applicationPart);
            }
        });

        return builder;
    }

    /// <summary>
    /// Configures the <see cref="ApplicationPartManager"/> of the <see cref="IMvcCoreBuilder.PartManager"/> using
    /// the given <see cref="Action{ApplicationPartManager}"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <param name="setupAction">The <see cref="Action{ApplicationPartManager}"/></param>
    /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
    public static IMvcCoreBuilder ConfigureApplicationPartManager(
        this IMvcCoreBuilder builder,
        Action<ApplicationPartManager> setupAction)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(setupAction);

        setupAction(builder.PartManager);

        return builder;
    }

    /// <summary>
    /// Configures <see cref="ApiBehaviorOptions"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <param name="setupAction">The configure action.</param>
    /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
    public static IMvcCoreBuilder ConfigureApiBehaviorOptions(
        this IMvcCoreBuilder builder,
        Action<ApiBehaviorOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(setupAction);

        builder.Services.Configure(setupAction);

        return builder;
    }

    /// <summary>
    /// Sets the <see cref="CompatibilityVersion"/> for ASP.NET Core MVC for the application.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <param name="version">The <see cref="CompatibilityVersion"/> value to configure.</param>
    /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
    [Obsolete("This API is obsolete and will be removed in a future version. Consider removing usages.",
        DiagnosticId = "ASP5001",
        UrlFormat = "https://aka.ms/aspnetcore-warnings/{0}")]
    public static IMvcCoreBuilder SetCompatibilityVersion(this IMvcCoreBuilder builder, CompatibilityVersion version)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.Configure<MvcCompatibilityOptions>(o => o.CompatibilityVersion = version);
        return builder;
    }
}
