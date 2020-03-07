// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.AspNetCore.Server.IIS.Core;

namespace Microsoft.AspNetCore.Hosting
{
    public static class WebHostBuilderIISExtensions
    {
        /// <summary>
        /// Configures the port and base path the server should listen on when running behind AspNetCoreModule.
        /// The app will also be configured to capture startup errors.
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <returns></returns>
        public static IWebHostBuilder UseIIS(this IWebHostBuilder hostBuilder)
        {
            if (hostBuilder == null)
            {
                throw new ArgumentNullException(nameof(hostBuilder));
            }

            // Check if in process
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && NativeMethods.IsAspNetCoreModuleLoaded())
            {
                var iisConfigData = NativeMethods.HttpGetApplicationProperties();
                // Trim trailing slash to be consistent with other servers
                var contentRoot = iisConfigData.pwzFullApplicationPath.TrimEnd(Path.DirectorySeparatorChar);
                hostBuilder.UseContentRoot(contentRoot);
                return hostBuilder.ConfigureServices(
                    services => {
                        services.AddSingleton(new IISNativeApplication(iisConfigData.pNativeApplication));
                        services.AddSingleton<IServer, IISHttpServer>();
                        services.AddSingleton<IStartupFilter>(new IISServerSetupFilter(iisConfigData.pwzVirtualApplicationPath));
                        services.AddAuthenticationCore();
                        services.AddSingleton<IServerIntegratedAuth>(_ => new ServerIntegratedAuth()
                        {
                            IsEnabled = iisConfigData.fWindowsAuthEnabled || iisConfigData.fBasicAuthEnabled,
                            AuthenticationScheme = IISServerDefaults.AuthenticationScheme
                        });
                        services.Configure<IISServerOptions>(
                            options => {
                                options.ServerAddresses = iisConfigData.pwzBindings.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                                options.ForwardWindowsAuthentication = iisConfigData.fWindowsAuthEnabled || iisConfigData.fBasicAuthEnabled;
                                options.IisMaxRequestSizeLimit = iisConfigData.maxRequestBodySize;
                            }
                        );
                    });
            }

            return hostBuilder;
        }
    }
}
