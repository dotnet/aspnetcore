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
    internal class CertificateForwarderFeature : ITlsConnectionFeature
    {
        private ILogger _logger;
        private StringValues _header;
        private CertificateForwarderOptions _options;
        private X509Certificate2 _certificate;

        public CertificateForwarderFeature(ILogger logger, StringValues header, CertificateForwarderOptions options)
        {
            _logger = logger;
            _options = options;
            _header = header;
        }

        public X509Certificate2 ClientCertificate
        {
            get
            {
                if (_certificate == null && _header != StringValues.Empty)
                {
                    try
                    {
                        _certificate = _options.HeaderConverter(_header);
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning(0, e, "Could not read certificate from header.");
                    }
                }
                return _certificate;
            }
            set => _certificate = value;
        }

        public Task<X509Certificate2> GetClientCertificateAsync(CancellationToken cancellationToken)
            => Task.FromResult(ClientCertificate);
    }
}
