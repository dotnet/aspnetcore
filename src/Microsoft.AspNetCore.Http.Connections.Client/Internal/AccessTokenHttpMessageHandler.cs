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
        private readonly Func<Task<string>> _accessTokenFactory;

        public AccessTokenHttpMessageHandler(HttpMessageHandler inner, Func<Task<string>> accessTokenFactory) : base(inner)
        {
            _accessTokenFactory = accessTokenFactory ?? throw new ArgumentNullException(nameof(accessTokenFactory));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var accessToken = await _accessTokenFactory();
            request.Headers.Authorization =  new AuthenticationHeaderValue("Bearer", accessToken);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
