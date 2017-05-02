// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore
{
    internal class KestrelServerOptionsSetup : IConfigureOptions<KestrelServerOptions>
    {
        private readonly IConfiguration _configurationRoot;

        public KestrelServerOptionsSetup(IConfiguration configurationRoot)
        {
            _configurationRoot = configurationRoot;
        }

        public void Configure(KestrelServerOptions options)
        {
            BindConfiguration(options);
        }

        private void BindConfiguration(KestrelServerOptions options)
        {
            var certificateLoader = new CertificateLoader(_configurationRoot.GetSection("Certificates"));

            foreach (var endPoint in _configurationRoot.GetSection("Kestrel:EndPoints").GetChildren())
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
                throw new InvalidOperationException($"Invalid IP address in configuration: {configAddress}");
            }

            if (!int.TryParse(configPort, out var port))
            {
                throw new InvalidOperationException($"Invalid port in configuration: {configPort}");
            }

            options.Listen(address, port, listenOptions =>
            {
                var certificateConfig = endPoint.GetSection("Certificate");

                if (certificateConfig.Exists())
                {
                    var certificate = certificateLoader.Load(certificateConfig).FirstOrDefault();

                    if (certificate == null)
                    {
                        throw new InvalidOperationException($"Unable to load certificate for endpoint '{endPoint.Key}'");
                    }

                    listenOptions.UseHttps(certificate);
                }
            });
        }
    }
}
