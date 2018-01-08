// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Certificates.Generation;
using Microsoft.AspNetCore.Server.Kestrel.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    public class KestrelServerOptionsSetup : IConfigureOptions<KestrelServerOptions>
    {
        private IServiceProvider _services;

        public KestrelServerOptionsSetup(IServiceProvider services)
        {
            _services = services;
        }

        public void Configure(KestrelServerOptions options)
        {
            options.ApplicationServices = _services;
            UseDefaultDeveloperCertificate(options);
        }

        private void UseDefaultDeveloperCertificate(KestrelServerOptions options)
        {
            var logger = options.ApplicationServices.GetRequiredService<ILogger<KestrelServer>>();
            X509Certificate2 certificate = null;
            try
            {
                var certificateManager = new CertificateManager();
                certificate = certificateManager.ListCertificates(CertificatePurpose.HTTPS, StoreName.My, StoreLocation.CurrentUser, isValid: true)
                    .FirstOrDefault();
            }
            catch
            {
                logger.UnableToLocateDevelopmentCertificate();
            }

            if (certificate != null)
            {
                logger.LocatedDevelopmentCertificate(certificate);
                options.DefaultCertificate = certificate;
            }
            else
            {
                logger.UnableToLocateDevelopmentCertificate();
            }
        }
    }
}
