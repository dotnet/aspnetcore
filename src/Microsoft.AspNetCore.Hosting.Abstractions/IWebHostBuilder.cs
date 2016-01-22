// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNet.Hosting
{
    /// <summary>
    /// A builder for <see cref="IWebHost"/>
    /// </summary>
    public interface IWebHostBuilder
    {
        /// <summary>
        /// Builds an <see cref="IWebHost"/> which hosts a web application.
        /// </summary>
        IWebHost Build();
        
        /// <summary>
        /// Gets the raw settings to be used by the web host. Values specified here will override 
        /// the configuration set by <see cref="UseConfiguration(IConfiguration)"/>.
        /// </summary>
        IDictionary<string, string> Settings { get; }

        /// <summary>
        /// Specify the <see cref="IConfiguration"/> to be used by the web host. If no configuration is
        /// provided to the builder, the default configuration will be used. 
        /// </summary>
        /// <param name="configuration">The <see cref="IConfiguration"/> to be used.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        IWebHostBuilder UseConfiguration(IConfiguration configuration);

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
        /// Add or replace a setting in <see cref="Settings"/>.
        /// </summary>
        /// <param name="key">The key of the setting to add or replace.</param>
        /// <param name="value">The value of the setting to add or replace.</param>
        /// <returns>The <see cref="IWebHostBuilder"/>.</returns>
        IWebHostBuilder UseSetting(string key, string value);
    }
}