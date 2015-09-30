// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNet.Server.Kestrel.Filter;

namespace Microsoft.AspNet.Server.Kestrel.Https
{
    public class HttpsConnectionFilter : IConnectionFilter
    {
        private readonly X509Certificate2 _cert;
        private readonly IConnectionFilter _previous;

        public HttpsConnectionFilter(X509Certificate2 cert, IConnectionFilter previous)
        {
            if (cert == null)
            {
                throw new ArgumentNullException(nameof(cert));
            }
            if (previous == null)
            {
                throw new ArgumentNullException(nameof(previous));
            }

            _cert = cert;
            _previous = previous;
        }

        public async Task OnConnection(ConnectionFilterContext context)
        {
            await _previous.OnConnection(context);

            if (string.Equals(context.Address.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            {
                var sslStream = new SslStream(context.Connection);
                await sslStream.AuthenticateAsServerAsync(_cert);
                context.Connection = sslStream;
            }
        }
    }
}
