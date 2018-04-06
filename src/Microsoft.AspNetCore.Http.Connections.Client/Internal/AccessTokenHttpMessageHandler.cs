// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http.Connections.Client.Internal
{
    public class AccessTokenHttpMessageHandler : DelegatingHandler
    {
        private readonly Func<string> _accessTokenFactory;

        public AccessTokenHttpMessageHandler(HttpMessageHandler inner, Func<string> accessTokenFactory) : base(inner)
        {
            _accessTokenFactory = accessTokenFactory ?? throw new ArgumentNullException(nameof(accessTokenFactory));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessTokenFactory());

            return base.SendAsync(request, cancellationToken);
        }
    }
}
