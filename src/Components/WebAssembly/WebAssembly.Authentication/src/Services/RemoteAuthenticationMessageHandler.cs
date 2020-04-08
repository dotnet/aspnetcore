// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    public class RemoteAuthenticationMessageHandler : DelegatingHandler
    {
        private readonly IAccessTokenProvider _provider;
        private readonly NavigationManager _navigation;

        private AccessToken _lastToken;
        private AuthenticationHeaderValue _cachedHeader;

        public RemoteAuthenticationMessageHandler(IAccessTokenProvider provider, NavigationManager navigation)
        {
            _provider = provider;
            _navigation = navigation;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.Now;
            if (_lastToken == null || now >= _lastToken.Expires.AddMinutes(-5))
            {
                // Request token options are not meant to be dynamic, but it is also not worth checking that the same options are passed all
                // the time since it is done as an implementation detail and users need to go out of their way to pass in different options
                // for different requests.
                var tokenResult = request.Properties.TryGetValue(AccessTokenRequestOptionsMessageHandler.AccessTokenRequestOptionsKey, out var options) ?
                    await _provider.RequestAccessToken((AccessTokenRequestOptions)options) :
                    await _provider.RequestAccessToken();

                if (tokenResult.TryGetToken(out var token))
                {
                    _lastToken = token;
                    _cachedHeader = new AuthenticationHeaderValue("Bearer", _lastToken.Value);
                }
                else
                {
                    throw new AccessTokenNotAvailableException(tokenResult);
                }
            }

            // We don't try to handle 401s and retry the request with a new token automatically since that would mean we need to copy the request
            // headers and buffer the body and we expect that the user instead handles the 401s. (Also, we can't really handle all 401s as we might
            // not be able to provision a token without user interaction).
            request.Headers.Authorization = _cachedHeader;
            return await base.SendAsync(request, cancellationToken);
        }

    }
}
