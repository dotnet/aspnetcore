// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Configuration;
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
        /// Adds a delegate for configuring the <see cref="IConfigurationBuilder"/> that will construct an <see cref="IConfiguration"/>.
        /// </summary>
        /// <param name="configureDelegate">The delegate for configuring the <see cref="IConfigurationBuilder" /> that will be used to construct an <see cref="IConfiguration" />.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        /// <remarks>
        /// The <see cref="IConfiguration"/> and <see cref="ILoggerFactory"/> on the <see cref="WebHostBuilderContext"/> are uninitialized at this stage.
        /// The <see cref="IConfigurationBuilder"/> is pre-populated with the settings of the <see cref="IWebHostBuilder"/>.
        /// </remarks>
        IWebHostBuilder ConfigureAppConfiguration(Action<WebHostBuilderContext, IConfigurationBuilder> configureDelegate);

        /// <summary>
        /// Adds a delegate for configuring the provided <see cref="ILoggerFactory"/>. This may be called multiple times.
        /// </summary>
        /// <param name="configureLogging">The delegate that configures the <see cref="ILoggerFactory"/>.</param>
        /// <typeparam name="T">
        /// The type of <see cref="ILoggerFactory"/> to configure.
        /// The delegate will not execute if the type provided does not match the <see cref="ILoggerFactory"/> used by the <see cref="IWebHostBuilder"/>
        /// </typeparam>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        /// <remarks>
        /// The <see cref="ILoggerFactory"/> on the <see cref="WebHostBuilderContext"/> is uninitialized at this stage.
        /// </remarks>
        IWebHostBuilder ConfigureLogging<T>(Action<WebHostBuilderContext, T> configureLogging) where T : ILoggerFactory;

        /// <summary>
        /// Adds a delegate for configuring additional services for the host or web application. This may be called
        /// multiple times.
        /// </summary>
        /// <param name="configureServices">A delegate for configuring the <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        IWebHostBuilder ConfigureServices(Action<IServiceCollection> configureServices);

        /// <summary>
        /// Adds a delegate for configuring additional services for the host or web application. This may be called
        /// multiple times.
        /// </summary>
        /// <param name="configureServices">A delegate for configuring the <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        IWebHostBuilder ConfigureServices(Action<WebHostBuilderContext, IServiceCollection> configureServices);

        /// <summary>
        /// Get the setting value from the configuration.
        /// </summary>
        /// <param name="key">The key of the setting to look up.</param>
        /// <returns>The value the setting currently contains.</returns>
        string GetSetting(string key);

        /// <summary>
        /// Add or replace a setting in the configuration.
        /// </summary>
        /// <param name="key">The key of the setting to add or replace.</param>
        /// <param name="value">The value of the setting to add or replace.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        IWebHostBuilder UseSetting(string key, string value);

        /// <summary>
        /// Specify the <see cref="ILoggerFactory"/> to be used by the web host.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/> to be used.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        IWebHostBuilder UseLoggerFactory(ILoggerFactory loggerFactory);

        /// <summary>
        /// Adds a delegate to construct the <see cref="ILoggerFactory"/> that will be registered
        /// as a singleton and used by the application.
        /// </summary>
        /// <param name="createLoggerFactory">The delegate that constructs an <see cref="IConfigurationBuilder" /></param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        /// <remarks>
        /// The <see cref="ILoggerFactory"/> on the <see cref="WebHostBuilderContext"/> is uninitialized at this stage.
        /// </remarks>
        IWebHostBuilder UseLoggerFactory(Func<WebHostBuilderContext, ILoggerFactory> createLoggerFactory);
    }
}