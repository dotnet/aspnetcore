// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Extensions;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Framework.Internal;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.AspNet.Mvc
{
    internal sealed class AntiForgeryWorker
    {
        private readonly AntiForgeryOptions _config;
        private readonly IAntiForgeryTokenSerializer _serializer;
        private readonly IAntiForgeryTokenStore _tokenStore;
        private readonly IAntiForgeryTokenValidator _validator;
        private readonly IAntiForgeryTokenGenerator _generator;
        private readonly IHtmlEncoder _htmlEncoder;

        internal AntiForgeryWorker([NotNull] IAntiForgeryTokenSerializer serializer,
                                   [NotNull] AntiForgeryOptions config,
                                   [NotNull] IAntiForgeryTokenStore tokenStore,
                                   [NotNull] IAntiForgeryTokenGenerator generator,
                                   [NotNull] IAntiForgeryTokenValidator validator,
                                   [NotNull] IHtmlEncoder htmlEncoder)
        {
            _serializer = serializer;
            _config = config;
            _tokenStore = tokenStore;
            _generator = generator;
            _validator = validator;
            _htmlEncoder = htmlEncoder;
        }

        private void CheckSSLConfig(HttpContext httpContext)
        {
            if (_config.RequireSSL && !httpContext.Request.IsHttps)
            {
                throw new InvalidOperationException(Resources.AntiForgeryWorker_RequireSSL);
            }
        }

        private AntiForgeryToken DeserializeToken(string serializedToken)
        {
            return (!string.IsNullOrEmpty(serializedToken))
                ? _serializer.Deserialize(serializedToken)
                : null;
        }

        private AntiForgeryToken DeserializeTokenDoesNotThrow(string serializedToken)
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

        private static ClaimsIdentity ExtractIdentity(HttpContext httpContext)
        {
            if (httpContext != null)
            {
                var user = httpContext.User;

                if (user != null)
                {
                    // We only support ClaimsIdentity.
                    return user.Identity as ClaimsIdentity;
                }
            }

            return null;
        }

        private AntiForgeryToken GetCookieTokenDoesNotThrow(HttpContext httpContext)
        {
            try
            {
                return _tokenStore.GetCookieToken(httpContext);
            }
            catch
            {
                // ignore failures since we'll just generate a new token
                return null;
            }
        }

        // [ ENTRY POINT ]
        // Generates an anti-XSRF token pair for the current user. The return
        // value is the hidden input form element that should be rendered in
        // the <form>. This method has a side effect: it may set a response
        // cookie.
        public TagBuilder GetFormInputElement([NotNull] HttpContext httpContext)
        {
            CheckSSLConfig(httpContext);

            var cookieToken = GetCookieTokenDoesNotThrow(httpContext);
            var tokenSet = GetTokens(httpContext, cookieToken);
            cookieToken = tokenSet.CookieToken;
            var formToken = tokenSet.FormToken;

            SaveCookieTokenAndHeader(httpContext, cookieToken);

            // <input type="hidden" name="__AntiForgeryToken" value="..." />
            var inputTag = new TagBuilder("input", _htmlEncoder)
            {
                Attributes =
                {
                    { "type", "hidden" },
                    { "name", _config.FormFieldName },
                    { "value", _serializer.Serialize(formToken) }
                }
            };
            return inputTag;
        }

        // [ ENTRY POINT ]
        // Generates a (cookie, form) serialized token pair for the current user.
        // The caller may specify an existing cookie value if one exists. If the
        // 'new cookie value' out param is non-null, the caller *must* persist
        // the new value to cookie storage since the original value was null or
        // invalid. This method is side-effect free.
        public AntiForgeryTokenSet GetTokens([NotNull] HttpContext httpContext, string cookieToken)
        {
            CheckSSLConfig(httpContext);
            var deSerializedcookieToken = DeserializeTokenDoesNotThrow(cookieToken);
            var tokenSet = GetTokens(httpContext, deSerializedcookieToken);

            var serializedCookieToken = Serialize(tokenSet.CookieToken);
            var serializedFormToken = Serialize(tokenSet.FormToken);
            return new AntiForgeryTokenSet(serializedFormToken, serializedCookieToken);
        }

        private AntiForgeryTokenSetInternal GetTokens(HttpContext httpContext, AntiForgeryToken cookieToken)
        {
            var newCookieToken = ValidateAndGenerateNewCookieToken(cookieToken);
            if (newCookieToken != null)
            {
                cookieToken = newCookieToken;
            }
            var formToken = _generator.GenerateFormToken(
                httpContext,
                ExtractIdentity(httpContext),
                cookieToken);

            return new AntiForgeryTokenSetInternal()
            {
                // Note : The new cookie would be null if the old cookie is valid.
                CookieToken = newCookieToken,
                FormToken = formToken
            };
        }

        private string Serialize(AntiForgeryToken token)
        {
            return (token != null) ? _serializer.Serialize(token) : null;
        }

        // [ ENTRY POINT ]
        // Given an HttpContext, validates that the anti-XSRF tokens contained
        // in the cookies & form are OK for this request.
        public async Task ValidateAsync([NotNull] HttpContext httpContext)
        {
            CheckSSLConfig(httpContext);

            // Extract cookie & form tokens
            var cookieToken = _tokenStore.GetCookieToken(httpContext);
            var formToken = await _tokenStore.GetFormTokenAsync(httpContext);

            // Validate
            _validator.ValidateTokens(httpContext, ExtractIdentity(httpContext), cookieToken, formToken);
        }

        // [ ENTRY POINT ]
        // Given the serialized string representations of a cookie & form token,
        // validates that the pair is OK for this request.
        public void Validate([NotNull] HttpContext httpContext, string cookieToken, string formToken)
        {
            CheckSSLConfig(httpContext);

            // Extract cookie & form tokens
            var deserializedCookieToken = DeserializeToken(cookieToken);
            var deserializedFormToken = DeserializeToken(formToken);

            // Validate
            _validator.ValidateTokens(
                httpContext,
                ExtractIdentity(httpContext),
                deserializedCookieToken,
                deserializedFormToken);
        }


        /// <summary>
        /// Generates and sets an anti-forgery cookie if one is not available or not valid. Also sets response headers.
        /// </summary>
        /// <param name="context">The HTTP context associated with the current call.</param>
        public void SetCookieTokenAndHeader([NotNull] HttpContext httpContext)
        {
            CheckSSLConfig(httpContext);

            var cookieToken = GetCookieTokenDoesNotThrow(httpContext);
            cookieToken = ValidateAndGenerateNewCookieToken(cookieToken);

            SaveCookieTokenAndHeader(httpContext, cookieToken);
        }

        // This method returns null if oldCookieToken is valid.
        private AntiForgeryToken ValidateAndGenerateNewCookieToken(AntiForgeryToken cookieToken)
        {
            if (!_validator.IsCookieTokenValid(cookieToken))
            {
                // Need to make sure we're always operating with a good cookie token.
                var newCookieToken = _generator.GenerateCookieToken();
                Debug.Assert(_validator.IsCookieTokenValid(newCookieToken));
                return newCookieToken;
            }

            return null;
        }

        private void SaveCookieTokenAndHeader(
            [NotNull] HttpContext httpContext,
            AntiForgeryToken cookieToken)
        {
            if (cookieToken != null)
            {
                // Persist the new cookie if it is not null.
                _tokenStore.SaveCookieToken(httpContext, cookieToken);
            }

            if (!_config.SuppressXFrameOptionsHeader)
            {
                // Adding X-Frame-Options header to prevent ClickJacking. See
                // http://tools.ietf.org/html/draft-ietf-websec-x-frame-options-10
                // for more information.
                httpContext.Response.Headers.Set("X-Frame-Options", "SAMEORIGIN");
            }
        }

        private class AntiForgeryTokenSetInternal
        {
            public AntiForgeryToken FormToken { get; set; }

            public AntiForgeryToken CookieToken { get; set; }
        }
    }
}