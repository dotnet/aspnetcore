// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding XML formatters to MVC.
/// </summary>
public static class MvcXmlMvcCoreBuilderExtensions
{
    /// <summary>
    /// Adds configuration of <see cref="MvcXmlOptions"/> for the application.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <param name="setupAction">The <see cref="MvcXmlOptions"/> which need to be configured.</param>
    /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
    public static IMvcCoreBuilder AddXmlOptions(
       this IMvcCoreBuilder builder,
       Action<MvcXmlOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(setupAction);

        builder.Services.Configure(setupAction);
        return builder;
    }

    /// <summary>
    /// Adds the XML DataContractSerializer formatters to MVC.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
    public static IMvcCoreBuilder AddXmlDataContractSerializerFormatters(this IMvcCoreBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        AddXmlDataContractSerializerFormatterServices(builder.Services);
        return builder;
    }

    /// <summary>
    /// Adds the XML DataContractSerializer formatters to MVC.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <param name="setupAction">The <see cref="MvcXmlOptions"/> which need to be configured.</param>
    /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
    public static IMvcCoreBuilder AddXmlDataContractSerializerFormatters(
        this IMvcCoreBuilder builder,
        Action<MvcXmlOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(setupAction);

        AddXmlDataContractSerializerFormatterServices(builder.Services);
        builder.Services.Configure(setupAction);
        return builder;
    }

    /// <summary>
    /// Adds the XML Serializer formatters to MVC.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
    public static IMvcCoreBuilder AddXmlSerializerFormatters(this IMvcCoreBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        AddXmlSerializerFormatterServices(builder.Services);
        return builder;
    }

    /// <summary>
    /// Adds the XML Serializer formatters to MVC.
    /// </summary>
    /// <param name="builder">The <see cref="IMvcCoreBuilder"/>.</param>
    /// <param name="setupAction">The <see cref="MvcXmlOptions"/> which need to be configured.</param>
    /// /// <returns>The <see cref="IMvcCoreBuilder"/>.</returns>
    public static IMvcCoreBuilder AddXmlSerializerFormatters(
        this IMvcCoreBuilder builder,
        Action<MvcXmlOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(builder);

        AddXmlSerializerFormatterServices(builder.Services);
        builder.Services.Configure(setupAction);
        return builder;
    }

    // Internal for testing.
    internal static void AddXmlDataContractSerializerFormatterServices(IServiceCollection services)
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, XmlDataContractSerializerMvcOptionsSetup>());
    }

    // Internal for testing.
    internal static void AddXmlSerializerFormatterServices(IServiceCollection services)
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, XmlSerializerMvcOptionsSetup>());
    }
}
