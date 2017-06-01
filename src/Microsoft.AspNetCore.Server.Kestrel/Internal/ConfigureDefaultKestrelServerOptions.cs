// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Certificates.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal
{
    public class ConfigureDefaultKestrelServerOptions : ConfigureDefaultOptions<KestrelServerOptions>
    {
        private const string DefaultCertificateSubjectName = "CN=localhost";
        private const string DevelopmentSSLCertificateName = "localhost";

        private readonly IServiceProvider _services;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IConfiguration _configurationRoot;
        private readonly ILoggerFactory _loggerFactory;

        public ConfigureDefaultKestrelServerOptions(
            IServiceProvider services,
            IHostingEnvironment hostingEnvironment,
            IConfiguration configurationRoot,
            ILoggerFactory loggerFactory)
        {
            _services = services;
            _hostingEnvironment = hostingEnvironment;
            _configurationRoot = configurationRoot;
            _loggerFactory = loggerFactory;
        }

        public override void Configure(string name, KestrelServerOptions options)
        {
            // Don't assume KestrelServerOptionsSetup has already set the services. Needed for UseHttps.
            options.ApplicationServices = _services;
            BindConfiguration(options);
        }

        private void BindConfiguration(KestrelServerOptions options)
        {
            var certificateLoader = new CertificateLoader(_configurationRoot.GetSection("Certificates"), _loggerFactory, _hostingEnvironment.EnvironmentName);

            foreach (var endPoint in _configurationRoot.GetSection("Microsoft:AspNetCore:Server:Kestrel:EndPoints").GetChildren())
            {
                BindEndPoint(options, endPoint, certificateLoader);
            }
        }

        private void BindEndPoint(
            KestrelServerOptions options,
            IConfigurationSection endPoint,
            CertificateLoader certificateLoader)
        {
            var configAddress = endPoint.GetValue<string>("Address");
            var configPort = endPoint.GetValue<string>("Port");

            if (!IPAddress.TryParse(configAddress, out var address))
            {
                throw new InvalidOperationException(KestrelStrings.FormatInvalidIp(configAddress));
            }

            if (!int.TryParse(configPort, out var port))
            {
                throw new InvalidOperationException(KestrelStrings.FormatInvalidPort(configPort));
            }

            options.Listen(address, port, listenOptions =>
            {
                var certificateConfig = endPoint.GetSection("Certificate");
                X509Certificate2 certificate = null;
                if (certificateConfig.Exists())
                {
                    try
                    {
                        try
                        {
                            certificate = certificateLoader.Load(certificateConfig).FirstOrDefault();
                        }
                        catch (KeyNotFoundException) when (certificateConfig.Value.Equals(DevelopmentSSLCertificateName, StringComparison.Ordinal) && _hostingEnvironment.IsDevelopment())
                        {
                            var storeLoader = new CertificateStoreLoader();
                            certificate = storeLoader.Load(DefaultCertificateSubjectName, "My", StoreLocation.CurrentUser, validOnly: false) ??
                                storeLoader.Load(DefaultCertificateSubjectName, "My", StoreLocation.LocalMachine, validOnly: false);

                            if (certificate == null)
                            {
                                var logger = _loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.KestrelServerConfigureOptions");
                                logger.LogError(KestrelStrings.NoCertFoundLog);
                            }
                        }

                        if (certificate == null)
                        {
                            throw new InvalidOperationException(KestrelStrings.FormatNoCertForEndpoint(endPoint.Key));
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(KestrelStrings.UnableToConfigureHttps, ex);
                    }

                    listenOptions.UseHttps(certificate);
                }
            });
        }
    }
}
