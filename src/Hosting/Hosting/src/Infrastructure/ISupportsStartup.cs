// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Internal;

namespace Microsoft.AspNetCore.Hosting.Infrastructure;

/// <summary>
/// An interface implemented by IWebHostBuilders that handle <see cref="WebHostBuilderExtensions.Configure(IWebHostBuilder, Action{IApplicationBuilder})"/>,
/// <see cref="WebHostBuilderExtensions.UseStartup(IWebHostBuilder, Type)"/> and <see cref="WebHostBuilderExtensions.UseStartup{TStartup}(IWebHostBuilder, Func{WebHostBuilderContext, TStartup})"/>
/// directly.
/// </summary>
public interface ISupportsStartup
{
    /// <summary>
    /// Specify the startup method to be used to configure the web application.
    /// </summary>
    /// <param name="configure">The delegate that configures the <see cref="IApplicationBuilder"/>.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    IWebHostBuilder Configure(Action<IApplicationBuilder> configure);

    /// <summary>
    /// Specify the startup method to be used to configure the web application.
    /// </summary>
    /// <param name="configure">The delegate that configures the <see cref="IApplicationBuilder"/>.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    IWebHostBuilder Configure(Action<WebHostBuilderContext, IApplicationBuilder> configure);

    /// <summary>
    /// Specify the startup type to be used by the web host.
    /// </summary>
    /// <param name="startupType">The <see cref="Type"/> to be used.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    IWebHostBuilder UseStartup([DynamicallyAccessedMembers(StartupLinkerOptions.Accessibility)] Type startupType);

    /// <summary>
    /// Specify a factory that creates the startup instance to be used by the web host.
    /// </summary>
    /// <param name="startupFactory">A delegate that specifies a factory for the startup class.</param>
    /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
    /// <remarks>When in a trimmed app, all public methods of <typeparamref name="TStartup"/> are preserved. This should match the Startup type directly (and not a base type).</remarks>
    IWebHostBuilder UseStartup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] TStartup>(Func<WebHostBuilderContext, TStartup> startupFactory);
}
