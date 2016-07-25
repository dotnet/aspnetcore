// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.AspNetCore.Server.Kestrel.Filter;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting
{
    public static class KestrelServerOptionsHttpsExtensions
    {
        /// <summary>
        /// Configure Kestrel to use HTTPS.
        /// </summary>
        /// <param name="options">
        /// The Microsoft.AspNetCore.Server.KestrelServerOptions to configure.
        /// </param>
        /// <param name="fileName">
        /// The name of a certificate file, relative to the directory that contains the application content files.
        /// </param>
        /// <returns>
        /// The Microsoft.AspNetCore.Server.KestrelServerOptions.
        /// </returns>
        public static KestrelServerOptions UseHttps(this KestrelServerOptions options, string fileName)
        {
            var env = options.ApplicationServices.GetRequiredService<IHostingEnvironment>();
            return options.UseHttps(new X509Certificate2(Path.Combine(env.ContentRootPath, fileName)));
        }

        /// <summary>
        /// Configure Kestrel to use HTTPS.
        /// </summary>
        /// <param name="options">
        /// The Microsoft.AspNetCore.Server.KestrelServerOptions to configure.
        /// </param>
        /// <param name="fileName">
        /// The name of a certificate file, relative to the directory that contains the application content files.
        /// </param>
        /// <param name="password">
        /// The password required to access the X.509 certificate data.
        /// </param>
        /// <returns>
        /// The Microsoft.AspNetCore.Server.KestrelServerOptions.
        /// </returns>
        public static KestrelServerOptions UseHttps(this KestrelServerOptions options, string fileName, string password)
        {
            var env = options.ApplicationServices.GetRequiredService<IHostingEnvironment>();
            return options.UseHttps(new X509Certificate2(Path.Combine(env.ContentRootPath, fileName), password));
        }

        /// <summary>
        /// Configure Kestrel to use HTTPS.
        /// </summary>
        /// <param name="options">
        /// The Microsoft.AspNetCore.Server.KestrelServerOptions to configure.
        /// </param>
        /// <param name="serverCertificate">
        /// The X.509 certificate.
        /// </param>
        /// <returns>
        /// The Microsoft.AspNetCore.Server.KestrelServerOptions.
        /// </returns>
        public static KestrelServerOptions UseHttps(this KestrelServerOptions options, X509Certificate2 serverCertificate)
        {
            return options.UseHttps(new HttpsConnectionFilterOptions { ServerCertificate = serverCertificate });
        }

        /// <summary>
        /// Configure Kestrel to use HTTPS.
        /// </summary>
        /// <param name="options">
        /// The Microsoft.AspNetCore.Server.KestrelServerOptions to configure.
        /// </param>
        /// <param name="httpsOptions">
        /// Options to configure HTTPS.
        /// </param>
        /// <returns>
        /// The Microsoft.AspNetCore.Server.KestrelServerOptions.
        /// </returns>
        public static KestrelServerOptions UseHttps(this KestrelServerOptions options, HttpsConnectionFilterOptions httpsOptions)
        {
            var prevFilter = options.ConnectionFilter ?? new NoOpConnectionFilter();
            var loggerFactory = options.ApplicationServices.GetRequiredService<ILoggerFactory>();
            options.ConnectionFilter = new HttpsConnectionFilter(httpsOptions, prevFilter, loggerFactory);
            return options;
        }
    }
}
