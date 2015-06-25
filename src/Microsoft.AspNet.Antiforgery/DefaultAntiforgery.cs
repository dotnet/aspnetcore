// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.AspNet.Antiforgery
{
    /// <summary>
    /// Provides access to the antiforgery system, which provides protection against
    /// Cross-site Request Forgery (XSRF, also called CSRF) attacks.
    /// </summary>
    public class DefaultAntiforgery : IAntiforgery
    {
        private readonly IHtmlEncoder _htmlEncoder;
        private readonly AntiforgeryOptions _options;
        private readonly IAntiforgeryTokenGenerator _tokenGenerator;
        private readonly IAntiforgeryTokenSerializer _tokenSerializer;
        private readonly IAntiforgeryTokenStore _tokenStore;

        public DefaultAntiforgery(
            IOptions<AntiforgeryOptions> antiforgeryOptionsAccessor,
            IAntiforgeryTokenGenerator tokenGenerator,
            IAntiforgeryTokenSerializer tokenSerializer,
            IAntiforgeryTokenStore tokenStore,
            IHtmlEncoder htmlEncoder)
        {
            _options = antiforgeryOptionsAccessor.Options;
            _tokenGenerator = tokenGenerator;
            _tokenSerializer = tokenSerializer;
            _tokenStore = tokenStore;
            _htmlEncoder = htmlEncoder;
        }

        /// <inheritdoc />
        public string GetHtml([NotNull] HttpContext context)
        {
            CheckSSLConfig(context);

            var tokenSet = GetAndStoreTokens(context);

            var inputTag = string.Format(
                "<input name=\"{0}\" type=\"{1}\" value=\"{2}\" />",
                _htmlEncoder.HtmlEncode(_options.FormFieldName),
                _htmlEncoder.HtmlEncode("hidden"),
                _htmlEncoder.HtmlEncode(tokenSet.FormToken));
            return inputTag;
        }

        /// <inheritdoc />
        public AntiforgeryTokenSet GetAndStoreTokens([NotNull] HttpContext context)
        {
            CheckSSLConfig(context);
            
            var tokenSet = GetTokensInternal(context);
            SaveCookieTokenAndHeader(context, tokenSet.CookieToken);
            return Serialize(tokenSet);
        }

        /// <inheritdoc />
        public AntiforgeryTokenSet GetTokens([NotNull] HttpContext context)
        {
            CheckSSLConfig(context);
            
            var tokenSet = GetTokensInternal(context);
            return Serialize(tokenSet);
        }

        /// <inheritdoc />
        public async Task ValidateRequestAsync([NotNull] HttpContext context)
        {
            CheckSSLConfig(context);

            // Extract cookie & form tokens
            var cookieToken = _tokenStore.GetCookieToken(context);
            var formToken = await _tokenStore.GetFormTokenAsync(context);

            // Validate
            _tokenGenerator.ValidateTokens(context, cookieToken, formToken);
        }

        /// <inheritdoc />
        public void ValidateTokens([NotNull] HttpContext context, AntiforgeryTokenSet antiforgeryTokenSet)
        {
            CheckSSLConfig(context);

            // Extract cookie & form tokens
            var deserializedCookieToken = DeserializeToken(antiforgeryTokenSet.CookieToken);
            var deserializedFormToken = DeserializeToken(antiforgeryTokenSet.FormToken);

            // Validate
            _tokenGenerator.ValidateTokens(
                context,
                deserializedCookieToken,
                deserializedFormToken);
        }

        /// <inheritdoc />
        public void SetCookieTokenAndHeader([NotNull] HttpContext context)
        {
            CheckSSLConfig(context);

            var cookieToken = GetCookieTokenDoesNotThrow(context);
            cookieToken = ValidateAndGenerateNewCookieToken(cookieToken);
            SaveCookieTokenAndHeader(context, cookieToken);
        }

        // This method returns null if oldCookieToken is valid.
        private AntiforgeryToken ValidateAndGenerateNewCookieToken(AntiforgeryToken cookieToken)
        {
            if (!_tokenGenerator.IsCookieTokenValid(cookieToken))
            {
                // Need to make sure we're always operating with a good cookie token.
                var newCookieToken = _tokenGenerator.GenerateCookieToken();
                Debug.Assert(_tokenGenerator.IsCookieTokenValid(newCookieToken));
                return newCookieToken;
            }

            return null;
        }

        private void SaveCookieTokenAndHeader(
            [NotNull] HttpContext context,
            AntiforgeryToken cookieToken)
        {
            if (cookieToken != null)
            {
                // Persist the new cookie if it is not null.
                _tokenStore.SaveCookieToken(context, cookieToken);
            }

            if (!_options.SuppressXFrameOptionsHeader)
            {
                // Adding X-Frame-Options header to prevent ClickJacking. See
                // http://tools.ietf.org/html/draft-ietf-websec-x-frame-options-10
                // for more information.
                context.Response.Headers.Set("X-Frame-Options", "SAMEORIGIN");
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

        private AntiforgeryToken DeserializeToken(string serializedToken)
        {
            return (!string.IsNullOrEmpty(serializedToken))
                ? _tokenSerializer.Deserialize(serializedToken)
                : null;
        }

        private AntiforgeryToken DeserializeTokenDoesNotThrow(string serializedToken)
        {
            try
            {
                return DeserializeToken(serializedToken);
            }
            catch
            {
                // ignore failures since we'll just generate a new token
                return null;
            }
        }

        private AntiforgeryToken GetCookieTokenDoesNotThrow(HttpContext context)
        {
            try
            {
                return _tokenStore.GetCookieToken(context);
            }
            catch
            {
                // ignore failures since we'll just generate a new token
                return null;
            }
        }

        private AntiforgeryTokenSetInternal GetTokensInternal(HttpContext context)
        {
            var cookieToken = GetCookieTokenDoesNotThrow(context);
            var newCookieToken = ValidateAndGenerateNewCookieToken(cookieToken);
            if (newCookieToken != null)
            {
                cookieToken = newCookieToken;
            }
            var formToken = _tokenGenerator.GenerateFormToken(
                context,
                cookieToken);

            return new AntiforgeryTokenSetInternal()
            {
                // Note : The new cookie would be null if the old cookie is valid.
                CookieToken = newCookieToken,
                FormToken = formToken
            };
        }

        private AntiforgeryTokenSet Serialize(AntiforgeryTokenSetInternal tokenSet)
        {
            return new AntiforgeryTokenSet(
                tokenSet.FormToken != null ? _tokenSerializer.Serialize(tokenSet.FormToken) : null,
                tokenSet.CookieToken != null ? _tokenSerializer.Serialize(tokenSet.CookieToken) : null);
        }

        private class AntiforgeryTokenSetInternal
        {
            public AntiforgeryToken FormToken { get; set; }

            public AntiforgeryToken CookieToken { get; set; }
        }
    }
}