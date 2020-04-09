// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    public class RemoteAuthenticationMessageHandler : DelegatingHandler
    {
        private readonly IAccessTokenProvider _provider;
        private AccessToken _lastToken;
        private AuthenticationHeaderValue _cachedHeader;
        private Uri[] _allowedUris;
        private string[] _scopes;
        private string _returnUrl;
        private AccessTokenRequestOptions _tokenOptions;

        public RemoteAuthenticationMessageHandler(
            IAccessTokenProvider provider)
        {
            _provider = provider;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.Now;
            if (_allowedUris.Any(uri => uri.IsBaseOf(request.RequestUri)))
            {
                if (_lastToken == null || now >= _lastToken.Expires.AddMinutes(-5))
                {
                    var tokenOptions = _scopes != null || _returnUrl != null ?
                        (_tokenOptions ??= new AccessTokenRequestOptions { Scopes = _scopes, ReturnUrl = _returnUrl })
                        : null;

                    var tokenResult = tokenOptions != null ?
                        await _provider.RequestAccessToken(tokenOptions) :
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

                    // We don't try to handle 401s and retry the request with a new token automatically since that would mean we need to copy the request
                    // headers and buffer the body and we expect that the user instead handles the 401s. (Also, we can't really handle all 401s as we might
                    // not be able to provision a token without user interaction).
                    request.Headers.Authorization = _cachedHeader;
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }

        public RemoteAuthenticationMessageHandler UseAllowedUrls(params string[] urls)
        {
            _allowedUris = urls.Select(uri => new Uri(uri, UriKind.Absolute)).ToArray();
            return this;
        }

        public RemoteAuthenticationMessageHandler UseScopes(params string[] scopes)
        {
            _scopes = scopes;
            return this;
        }

        public RemoteAuthenticationMessageHandler UseReturnUrl(string returnUrl)
        {
            _returnUrl = returnUrl;
            return this;
        }
    }
}
