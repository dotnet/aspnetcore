// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting
{
    public static class AppServicesWebHostBuilderExtensions
    {
        /// <summary>
        /// Configures application to use Azure AppServices integration.
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <returns></returns>
        public static IWebHostBuilder UseAzureAppServices(this IWebHostBuilder hostBuilder)
        {
            if (hostBuilder == null)
            {
                throw new ArgumentNullException(nameof(hostBuilder));
            }
#pragma warning disable 618
            hostBuilder.ConfigureLogging(builder => builder.AddAzureWebAppDiagnostics());
#pragma warning restore 618
            return hostBuilder;
        }
    }
}
