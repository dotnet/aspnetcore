// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HttpOverrides
{
    internal sealed partial class CertificateForwardingFeature : ITlsConnectionFeature
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
                        Log.NoCertificate(_logger, e);
                    }
                }
                return _certificate;
            }
            set => _certificate = value;
        }

        public Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken)
            => Task.FromResult(ClientCertificate);

        private static partial class Log
        {
            [LoggerMessage(0, LogLevel.Warning, "Could not read certificate from header.", EventName = "NoCertificate")]
            public static partial void NoCertificate(ILogger logger, Exception exception);
        }
    }
}
