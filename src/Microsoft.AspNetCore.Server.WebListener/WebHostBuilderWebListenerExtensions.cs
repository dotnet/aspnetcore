// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.WebListener;
using Microsoft.AspNetCore.Server.WebListener.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Hosting
{
    public static class WebHostBuilderWebListenerExtensions
    {
        /// <summary>
        /// Specify WebListener as the server to be used by the web host.
        /// </summary>
        /// <param name="hostBuilder">
        /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
        /// </param>
        /// <returns>
        /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder.
        /// </returns>
        public static IWebHostBuilder UseWebListener(this IWebHostBuilder hostBuilder)
        {
            return hostBuilder.ConfigureServices(services => {
                services.AddTransient<IConfigureOptions<WebListenerOptions>, WebListenerOptionsSetup>();
                services.AddSingleton<IServer, MessagePump>();
            });
        }

        /// <summary>
        /// Specify WebListener as the server to be used by the web host.
        /// </summary>
        /// <param name="hostBuilder">
        /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
        /// </param>
        /// <param name="options">
        /// A callback to configure WebListener options.
        /// </param>
        /// <returns>
        /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder.
        /// </returns>
        public static IWebHostBuilder UseWebListener(this IWebHostBuilder hostBuilder, Action<WebListenerOptions> options)
        {
            return hostBuilder.UseWebListener().ConfigureServices(services =>
            {
                services.Configure(options);
            });
        }
    }
}
