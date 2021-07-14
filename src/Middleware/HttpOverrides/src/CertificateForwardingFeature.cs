// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HttpOverrides
{
    internal class CertificateForwardingFeature : ITlsConnectionFeature
    {
        private readonly ILogger _logger;
        private readonly StringValues _header;
        private readonly CertificateForwardingOptions _options;
        private X509Certificate2? _certificate;

        public CertificateForwardingFeature(ILogger logger, StringValues header, CertificateForwardingOptions options)
        {
            _logger = logger;
            _options = options;
            _header = header;
        }

        public X509Certificate2? ClientCertificate
        {
            get
            {
                if (_certificate == null)
                {
                    try
                    {
                        _certificate = _options.HeaderConverter(_header);
                    }
                    catch (Exception e)
                    {
                        _logger.NoCertificate(e);
                    }
                }
                return _certificate;
            }
            set => _certificate = value;
        }

        public Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken)
            => Task.FromResult(ClientCertificate);
    }
}
