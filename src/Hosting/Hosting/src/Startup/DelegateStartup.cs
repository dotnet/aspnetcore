// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// Used for initializing services and middlewares used by an application.
/// </summary>
public class DelegateStartup : StartupBase<IServiceCollection>
{
    private readonly Action<IApplicationBuilder> _configureApp;

    /// <summary>
    /// Creates a new <see cref="DelegateStartup" /> instance.
    /// </summary>
    /// <param name="factory">A factory for creating <see cref="IServiceProvider"/> instances.</param>
    /// <param name="configureApp">An <see cref="Action"/> for configuring the application.</param>
    public DelegateStartup(IServiceProviderFactory<IServiceCollection> factory, Action<IApplicationBuilder> configureApp) : base(factory)
    {
        _configureApp = configureApp;
    }

    /// <summary>
    /// Configures the <see cref="IApplicationBuilder"/> with the initialized <see cref="Action"/>.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
    public override void Configure(IApplicationBuilder app) => _configureApp(app);
}
