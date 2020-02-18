// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Hosting
{
    /// <summary>
    /// Provides extensions method to use Http.sys as the server for the web host.
    /// </summary>    
    public static class WebHostBuilderHttpSysExtensions
    {
        /// <summary>
        /// Specify Http.sys as the server to be used by the web host.
        /// </summary>
        /// <param name="hostBuilder">
        /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
        /// </param>
        /// <returns>
        /// A reference to the <see cref="IWebHostBuilder" /> parameter object.
        /// </returns>
        public static IWebHostBuilder UseHttpSys(this IWebHostBuilder hostBuilder)
        {
            return hostBuilder.ConfigureServices(services => {
                services.AddSingleton<IServer, MessagePump>();
                services.AddSingleton<IServerIntegratedAuth>(services =>
                {
                    var options = services.GetRequiredService<IOptions<HttpSysOptions>>().Value;
                    return new ServerIntegratedAuth()
                    {
                        IsEnabled = options.Authentication.Schemes != AuthenticationSchemes.None,
                        AuthenticationScheme = HttpSysDefaults.AuthenticationScheme,
                    };
                });
                services.AddAuthenticationCore();
            });
        }

        /// <summary>
        /// Specify Http.sys as the server to be used by the web host.
        /// </summary>
        /// <param name="hostBuilder">
        /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
        /// </param>
        /// <param name="options">
        /// A callback to configure Http.sys options.
        /// </param>
        /// <returns>
        /// A reference to the <see cref="IWebHostBuilder" /> parameter object.
        /// </returns>
        public static IWebHostBuilder UseHttpSys(this IWebHostBuilder hostBuilder, Action<HttpSysOptions> options)
        {
            return hostBuilder.UseHttpSys().ConfigureServices(services =>
            {
                services.Configure(options);
            });
        }
    }
}
