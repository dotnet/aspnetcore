// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    /// <summary>
    /// A <see cref="DelegatingHandler"/> that attaches access tokens to outgoing <see cref="HttpResponseMessage"/>s to endpoints
    /// for requests where any <see cref="Uri"/> in the list of <see cref="Uri"/>s defined in <see cref="AuthorizationMessageHandler.ConfigureHandler(IEnumerable{string}, IEnumerable{string}, string)"/>
    /// is a base of <see cref="HttpRequestMessage.RequestUri"/>.
    /// </summary>
    public class AuthorizationMessageHandler : DelegatingHandler
    {
        private readonly IAccessTokenProvider _provider;
        private AccessToken _lastToken;
        private AuthenticationHeaderValue _cachedHeader;
        private Uri[] _allowedUris;
        private AccessTokenRequestOptions _tokenOptions;

        /// <summary>
        /// Initializes a new instance of <see cref="AuthorizationMessageHandler"/>.
        /// </summary>
        /// <param name="provider">The <see cref="IAccessTokenProvider"/> to use for provisioning tokens.</param>
        public AuthorizationMessageHandler(
            IAccessTokenProvider provider)
        {
            _provider = provider;
        }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.Now;
            if (_allowedUris == null)
            {
                throw new InvalidOperationException($"The '{nameof(AuthorizationMessageHandler)}' is not configured. " +
                    $"Call '{nameof(AuthorizationMessageHandler.ConfigureHandler)}' and provide a list of endpoint urls to attach the token to.");
            }
            if (_allowedUris.Any(uri => uri.IsBaseOf(request.RequestUri)))
            {
                if (_lastToken == null || now >= _lastToken.Expires.AddMinutes(-5))
                {
                    var tokenResult = _tokenOptions != null ?
                        await _provider.RequestAccessToken(_tokenOptions) :
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

        /// <summary>
        /// Configures this handler to attach the token to the given list of urls and optionally request the given scopes and use the given return url
        /// instead of the defaults provided by the authentication implementation.
        /// </summary>
        /// <param name="endpointUrls">The list of base addresses of endpoint urls to which the token will be attached to.</param>
        /// <param name="scopes">The list of scopes to use when requesting a token to attach to outgoing requests.</param>
        /// <param name="returnUrl">The return url to use in case there is an issue provisioning the token and a redirection to the
        /// identity provider is necessary.
        /// </param>
        /// <returns>This <see cref="AuthorizationMessageHandler"/>.</returns>
        public AuthorizationMessageHandler ConfigureHandler(
            IEnumerable<string> endpointUrls,
            IEnumerable<string> scopes = null,
            string returnUrl = null)
        {
            if (_allowedUris != null)
            {
                throw new InvalidOperationException("Handler already configured.");
            }

            if (endpointUrls == null)
            {
                throw new ArgumentNullException(nameof(endpointUrls));
            }

            var uris = endpointUrls.Select(uri => new Uri(uri, UriKind.Absolute)).ToArray();
            if (uris.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(endpointUrls));
            }

            _allowedUris = uris;
            var scopesList = scopes?.ToArray();
            if (scopesList != null || returnUrl != null)
            {
                _tokenOptions = new AccessTokenRequestOptions
                {
                    Scopes = scopesList,
                    ReturnUrl = returnUrl
                };
            }

            return this;
        }
    }
}
