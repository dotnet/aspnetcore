// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Antiforgery.Internal
{
    /// <summary>
    /// Provides access to the antiforgery system, which provides protection against
    /// Cross-site Request Forgery (XSRF, also called CSRF) attacks.
    /// </summary>
    public class DefaultAntiforgery : IAntiforgery
    {
        private readonly AntiforgeryOptions _options;
        private readonly IAntiforgeryTokenGenerator _tokenGenerator;
        private readonly IAntiforgeryTokenSerializer _tokenSerializer;
        private readonly IAntiforgeryTokenStore _tokenStore;
        private readonly ILogger<DefaultAntiforgery> _logger;

        public DefaultAntiforgery(
            IOptions<AntiforgeryOptions> antiforgeryOptionsAccessor,
            IAntiforgeryTokenGenerator tokenGenerator,
            IAntiforgeryTokenSerializer tokenSerializer,
            IAntiforgeryTokenStore tokenStore,
            ILoggerFactory loggerFactory)
        {
            _options = antiforgeryOptionsAccessor.Value;
            _tokenGenerator = tokenGenerator;
            _tokenSerializer = tokenSerializer;
            _tokenStore = tokenStore;
            _logger = loggerFactory.CreateLogger<DefaultAntiforgery>();
        }

        /// <inheritdoc />
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            CheckSSLConfig(httpContext);

            var antiforgeryFeature = GetTokensInternal(httpContext);
            var tokenSet = Serialize(antiforgeryFeature);

            if (!antiforgeryFeature.HaveStoredNewCookieToken)
            {
                if (antiforgeryFeature.NewCookieToken != null)
                {
                    // Serialize handles the new cookie token string.
                    Debug.Assert(antiforgeryFeature.NewCookieTokenString != null);

                    SaveCookieTokenAndHeader(httpContext, antiforgeryFeature.NewCookieTokenString);
                    antiforgeryFeature.HaveStoredNewCookieToken = true;
                    _logger.NewCookieToken();
                }
                else
                {
                    _logger.ReusedCookieToken();
                }
            }

            return tokenSet;
        }

        /// <inheritdoc />
        public AntiforgeryTokenSet GetTokens(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            CheckSSLConfig(httpContext);

            var antiforgeryFeature = GetTokensInternal(httpContext);
            return Serialize(antiforgeryFeature);
        }

        /// <inheritdoc />
        public async Task<bool> IsRequestValidAsync(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            CheckSSLConfig(httpContext);

            var method = httpContext.Request.Method;
            if (string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(method, "HEAD", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(method, "OPTIONS", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(method, "TRACE", StringComparison.OrdinalIgnoreCase))
            {
                // Validation not needed for these request types.
                return true;
            }

            var tokens = await _tokenStore.GetRequestTokensAsync(httpContext);
            if (tokens.CookieToken == null)
            {
                _logger.MissingCookieToken(_options.CookieName);
                return false;
            }

            if (tokens.RequestToken == null)
            {
                _logger.MissingRequestToken(_options.FormFieldName, _options.HeaderName);
                return false;
            }

            // Extract cookie & request tokens
            AntiforgeryToken deserializedCookieToken;
            AntiforgeryToken deserializedRequestToken;
            DeserializeTokens(httpContext, tokens, out deserializedCookieToken, out deserializedRequestToken);

            // Validate
            string message;
            var result = _tokenGenerator.TryValidateTokenSet(
                httpContext,
                deserializedCookieToken,
                deserializedRequestToken,
                out message);

            if (result)
            {
                _logger.ValidatedAntiforgeryToken();
            }
            else
            {
                _logger.ValidationFailed(message);
            }

            return result;
        }

        /// <inheritdoc />
        public async Task ValidateRequestAsync(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            CheckSSLConfig(httpContext);

            var tokens = await _tokenStore.GetRequestTokensAsync(httpContext);
            if (tokens.CookieToken == null)
            {
                throw new AntiforgeryValidationException(
                    Resources.FormatAntiforgery_CookieToken_MustBeProvided(_options.CookieName));
            }

            if (tokens.RequestToken == null)
            {
                if (_options.HeaderName == null)
                {
                    var message = Resources.FormatAntiforgery_FormToken_MustBeProvided(_options.FormFieldName);
                    throw new AntiforgeryValidationException(message);
                }
                else if (!httpContext.Request.HasFormContentType)
                {
                    var message = Resources.FormatAntiforgery_HeaderToken_MustBeProvided(_options.HeaderName);
                    throw new AntiforgeryValidationException(message);
                }
                else
                {
                    var message = Resources.FormatAntiforgery_RequestToken_MustBeProvided(
                        _options.FormFieldName,
                        _options.HeaderName);
                    throw new AntiforgeryValidationException(message);
                }
            }

            ValidateTokens(httpContext, tokens);

            _logger.ValidatedAntiforgeryToken();
        }

        private void ValidateTokens(HttpContext httpContext, AntiforgeryTokenSet antiforgeryTokenSet)
        {
            Debug.Assert(!string.IsNullOrEmpty(antiforgeryTokenSet.CookieToken));
            Debug.Assert(!string.IsNullOrEmpty(antiforgeryTokenSet.RequestToken));

            // Extract cookie & request tokens
            AntiforgeryToken deserializedCookieToken;
            AntiforgeryToken deserializedRequestToken;
            DeserializeTokens(
                httpContext,
                antiforgeryTokenSet,
                out deserializedCookieToken,
                out deserializedRequestToken);

            // Validate
            string message;
            if (!_tokenGenerator.TryValidateTokenSet(
                httpContext,
                deserializedCookieToken,
                deserializedRequestToken,
                out message))
            {
                throw new AntiforgeryValidationException(message);
            }
        }

        /// <inheritdoc />
        public void SetCookieTokenAndHeader(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            CheckSSLConfig(httpContext);

            var antiforgeryFeature = GetCookieTokens(httpContext);
            if (!antiforgeryFeature.HaveStoredNewCookieToken && antiforgeryFeature.NewCookieToken != null)
            {
                if (antiforgeryFeature.NewCookieTokenString == null)
                {
                    antiforgeryFeature.NewCookieTokenString =
                        _tokenSerializer.Serialize(antiforgeryFeature.NewCookieToken);
                }

                SaveCookieTokenAndHeader(httpContext, antiforgeryFeature.NewCookieTokenString);
                antiforgeryFeature.HaveStoredNewCookieToken = true;
                _logger.NewCookieToken();
            }
            else
            {
                _logger.ReusedCookieToken();
            }
        }

        private void SaveCookieTokenAndHeader(HttpContext httpContext, string cookieToken)
        {
            if (cookieToken != null)
            {
                // Persist the new cookie if it is not null.
                _tokenStore.SaveCookieToken(httpContext, cookieToken);
            }

            if (!_options.SuppressXFrameOptionsHeader)
            {
                // Adding X-Frame-Options header to prevent ClickJacking. See
                // http://tools.ietf.org/html/draft-ietf-websec-x-frame-options-10
                // for more information.
                httpContext.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
            }
        }

        private void CheckSSLConfig(HttpContext context)
        {
            if (_options.RequireSsl && !context.Request.IsHttps)
            {
                throw new InvalidOperationException(Resources.FormatAntiforgeryWorker_RequireSSL(
                    nameof(AntiforgeryOptions),
                    nameof(AntiforgeryOptions.RequireSsl),
                    "true"));
            }
        }

        private static IAntiforgeryFeature GetAntiforgeryFeature(HttpContext httpContext)
        {
            var antiforgeryFeature = httpContext.Features.Get<IAntiforgeryFeature>();
            if (antiforgeryFeature == null)
            {
                antiforgeryFeature = new AntiforgeryFeature();
                httpContext.Features.Set(antiforgeryFeature);
            }

            return antiforgeryFeature;
        }

        private IAntiforgeryFeature GetCookieTokens(HttpContext httpContext)
        {
            var antiforgeryFeature = GetAntiforgeryFeature(httpContext);

            if (antiforgeryFeature.HaveGeneratedNewCookieToken)
            {
                Debug.Assert(antiforgeryFeature.HaveDeserializedCookieToken);

                // Have executed this method earlier in the context of this request.
                return antiforgeryFeature;
            }

            AntiforgeryToken cookieToken;
            if (antiforgeryFeature.HaveDeserializedCookieToken)
            {
                cookieToken = antiforgeryFeature.CookieToken;
            }
            else
            {
                cookieToken = GetCookieTokenDoesNotThrow(httpContext);

                antiforgeryFeature.CookieToken = cookieToken;
                antiforgeryFeature.HaveDeserializedCookieToken = true;
            }

            AntiforgeryToken newCookieToken;
            if (_tokenGenerator.IsCookieTokenValid(cookieToken))
            {
                // No need for the cookie token from the request after it has been verified.
                newCookieToken = null;
            }
            else
            {
                // Need to make sure we're always operating with a good cookie token.
                newCookieToken = _tokenGenerator.GenerateCookieToken();
                Debug.Assert(_tokenGenerator.IsCookieTokenValid(newCookieToken));
            }

            antiforgeryFeature.HaveGeneratedNewCookieToken = true;
            antiforgeryFeature.NewCookieToken = newCookieToken;

            return antiforgeryFeature;
        }

        private AntiforgeryToken GetCookieTokenDoesNotThrow(HttpContext httpContext)
        {
            try
            {
                var serializedToken = _tokenStore.GetCookieToken(httpContext);
                var token = _tokenSerializer.Deserialize(serializedToken);

                return token;
            }
            catch
            {
                // ignore failures since we'll just generate a new token
                return null;
            }
        }

        private IAntiforgeryFeature GetTokensInternal(HttpContext httpContext)
        {
            var antiforgeryFeature = GetCookieTokens(httpContext);
            if (antiforgeryFeature.NewRequestToken == null)
            {
                var cookieToken = antiforgeryFeature.NewCookieToken ?? antiforgeryFeature.CookieToken;
                antiforgeryFeature.NewRequestToken = _tokenGenerator.GenerateRequestToken(
                    httpContext,
                    cookieToken);
            }

            return antiforgeryFeature;
        }

        private AntiforgeryTokenSet Serialize(IAntiforgeryFeature antiforgeryFeature)
        {
            // Should only be called after new tokens have been generated.
            Debug.Assert(antiforgeryFeature.HaveGeneratedNewCookieToken);
            Debug.Assert(antiforgeryFeature.NewRequestToken != null);

            if (antiforgeryFeature.NewRequestTokenString == null)
            {
                antiforgeryFeature.NewRequestTokenString =
                    _tokenSerializer.Serialize(antiforgeryFeature.NewRequestToken);
            }

            if (antiforgeryFeature.NewCookieTokenString == null && antiforgeryFeature.NewCookieToken != null)
            {
                antiforgeryFeature.NewCookieTokenString =
                    _tokenSerializer.Serialize(antiforgeryFeature.NewCookieToken);
            }

            return new AntiforgeryTokenSet(
                antiforgeryFeature.NewRequestTokenString,
                antiforgeryFeature.NewCookieTokenString,
                _options.FormFieldName,
                _options.HeaderName);
        }

        private void DeserializeTokens(
            HttpContext httpContext,
            AntiforgeryTokenSet antiforgeryTokenSet,
            out AntiforgeryToken cookieToken,
            out AntiforgeryToken requestToken)
        {
            var antiforgeryFeature = GetAntiforgeryFeature(httpContext);

            if (antiforgeryFeature.HaveDeserializedCookieToken)
            {
                cookieToken = antiforgeryFeature.CookieToken;
            }
            else
            {
                cookieToken = _tokenSerializer.Deserialize(antiforgeryTokenSet.CookieToken);

                antiforgeryFeature.CookieToken = cookieToken;
                antiforgeryFeature.HaveDeserializedCookieToken = true;
            }

            if (antiforgeryFeature.HaveDeserializedRequestToken)
            {
                requestToken = antiforgeryFeature.RequestToken;
            }
            else
            {
                requestToken = _tokenSerializer.Deserialize(antiforgeryTokenSet.RequestToken);

                antiforgeryFeature.RequestToken = requestToken;
                antiforgeryFeature.HaveDeserializedRequestToken = true;
            }
        }
    }
}
