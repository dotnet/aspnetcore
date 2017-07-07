// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting
{
    /// <summary>
    /// Extension methods fro <see cref="ListenOptions"/> that configure Kestrel to use HTTPS for a given endpoint.
    /// </summary>
    public static class ListenOptionsHttpsExtensions
    {
        /// <summary>
        /// Configure Kestrel to use HTTPS.
        /// </summary>
        /// <param name="listenOptions">
        /// The <see cref="ListenOptions"/> to configure.
        /// </param>
        /// <param name="fileName">
        /// The name of a certificate file, relative to the directory that contains the application content files.
        /// </param>
        /// <returns>
        /// The <see cref="ListenOptions"/>.
        /// </returns>
        public static ListenOptions UseHttps(this ListenOptions listenOptions, string fileName)
        {
            var env = listenOptions.KestrelServerOptions.ApplicationServices.GetRequiredService<IHostingEnvironment>();
            return listenOptions.UseHttps(new X509Certificate2(Path.Combine(env.ContentRootPath, fileName)));
        }

        /// <summary>
        /// Configure Kestrel to use HTTPS.
        /// </summary>
        /// <param name="listenOptions">
        /// The <see cref="ListenOptions"/> to configure.
        /// </param>
        /// <param name="fileName">
        /// The name of a certificate file, relative to the directory that contains the application content files.
        /// </param>
        /// <param name="password">
        /// The password required to access the X.509 certificate data.
        /// </param>
        /// <returns>
        /// The <see cref="ListenOptions"/>.
        /// </returns>
        public static ListenOptions UseHttps(this ListenOptions listenOptions, string fileName, string password)
        {
            var env = listenOptions.KestrelServerOptions.ApplicationServices.GetRequiredService<IHostingEnvironment>();
            return listenOptions.UseHttps(new X509Certificate2(Path.Combine(env.ContentRootPath, fileName), password));
        }

        /// <summary>
        /// Configure Kestrel to use HTTPS.
        /// </summary>
        /// <param name="listenOptions">
        /// The <see cref="ListenOptions"/> to configure.
        /// </param>
        /// <param name="serverCertificate">
        /// The X.509 certificate.
        /// </param>
        /// <returns>
        /// The <see cref="ListenOptions"/>.
        /// </returns>
        public static ListenOptions UseHttps(this ListenOptions listenOptions, X509Certificate2 serverCertificate)
        {
            return listenOptions.UseHttps(new HttpsConnectionAdapterOptions { ServerCertificate = serverCertificate });
        }

        /// <summary>
        /// Configure Kestrel to use HTTPS.
        /// </summary>
        /// <param name="listenOptions">
        /// The <see cref="ListenOptions"/> to configure.
        /// </param>
        /// <param name="httpsOptions">
        /// Options to configure HTTPS.
        /// </param>
        /// <returns>
        /// The <see cref="ListenOptions"/>.
        /// </returns>
        public static ListenOptions UseHttps(this ListenOptions listenOptions, HttpsConnectionAdapterOptions httpsOptions)
        {
            var loggerFactory = listenOptions.KestrelServerOptions.ApplicationServices.GetRequiredService<ILoggerFactory>();
            listenOptions.ConnectionAdapters.Add(new HttpsConnectionAdapter(httpsOptions, loggerFactory));
            return listenOptions;
        }
    }
}
