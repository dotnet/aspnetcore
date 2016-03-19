// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting
{
    /// <summary>
    /// A builder for <see cref="IWebHost"/>.
    /// </summary>
    public interface IWebHostBuilder
    {
        /// <summary>
        /// Builds an <see cref="IWebHost"/> which hosts a web application.
        /// </summary>
        IWebHost Build();

        /// <summary>
        /// Specify the <see cref="ILoggerFactory"/> to be used by the web host.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to be used.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        IWebHostBuilder UseLoggerFactory(ILoggerFactory loggerFactory);

        /// <summary>
        /// Specify the <see cref="IServerFactory"/> to be used by the web host.
        /// </summary>
        /// <param name="factory">The <see cref="IServerFactory"/> to be used.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        IWebHostBuilder UseServer(IServerFactory factory);

        /// <summary>
        /// Specify the startup type to be used by the web host. 
        /// </summary>
        /// <param name="startupType">The <see cref="Type"/> to be used.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        IWebHostBuilder UseStartup(Type startupType);

        /// <summary>
        /// Specify the delegate that is used to configure the services of the web application.
        /// </summary>
        /// <param name="configureServices">The delegate that configures the <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        IWebHostBuilder ConfigureServices(Action<IServiceCollection> configureServices);

        /// <summary>
        /// Specify the startup method to be used to configure the web application. 
        /// </summary>
        /// <param name="configureApplication">The delegate that configures the <see cref="IApplicationBuilder"/>.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        IWebHostBuilder Configure(Action<IApplicationBuilder> configureApplication);

        /// <summary>
        /// Adds a delegate for configuring the provided <see cref="ILoggerFactory"/>. This may be called multiple times.
        /// </summary>
        /// <param name="configureLogging">The delegate that configures the <see cref="ILoggerFactory"/>.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        IWebHostBuilder ConfigureLogging(Action<ILoggerFactory> configureLogging);

        /// <summary>
        /// Add or replace a setting in the configuration.
        /// </summary>
        /// <param name="key">The key of the setting to add or replace.</param>
        /// <param name="value">The value of the setting to add or replace.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        IWebHostBuilder UseSetting(string key, string value);

        /// <summary>
        /// Get the setting value from the configuration.
        /// </summary>
        /// <param name="key">The key of the setting to look up.</param>
        /// <returns>The value the setting currently contains.</returns>
        string GetSetting(string key);
    }
}