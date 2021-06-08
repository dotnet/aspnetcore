// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;

namespace SampleApp
{
    internal class BufferingTlsFeature : ITlsConnectionFeature
    {
        private ITlsConnectionFeature _tlsFeature;
        private HttpContext _context;

        public BufferingTlsFeature(ITlsConnectionFeature tlsFeature, HttpContext context)
        {
            _tlsFeature = tlsFeature;
            _context = context;
        }

        public X509Certificate2 ClientCertificate
        {
            get => _tlsFeature.ClientCertificate;
            set => _tlsFeature.ClientCertificate = value;
        }

        public async Task<X509Certificate2> GetClientCertificateAsync(CancellationToken cancellationToken)
        {
            // TODO: This doesn't set a size limit for the buffering or draining by default, it relies on the server's
            // 30mb default request size limit.
            if (!_context.Request.Body.CanSeek)
            {
                _context.Request.EnableBuffering();
            }
            var body = _context.Request.Body;
            await body.DrainAsync(cancellationToken);
            body.Position = 0;

            // Negative caching, prevent buffering on future requests even if the client does not give a cert when prompted.
            var connectionItems = _context.Features.Get<IConnectionItemsFeature>();
            connectionItems.Items["tls.clientcert.negotiated"] = true;

            return await _tlsFeature.GetClientCertificateAsync(cancellationToken);
        }
    }
}
