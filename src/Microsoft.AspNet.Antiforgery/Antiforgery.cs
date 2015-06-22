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
    /// Provides access to the anti-forgery system, which provides protection against
    /// Cross-site Request Forgery (XSRF, also called CSRF) attacks.
    /// </summary>
    public class Antiforgery
    {
        private readonly IHtmlEncoder _htmlEncoder;
        private readonly AntiforgeryOptions _options;
        private readonly IAntiforgeryTokenGenerator _tokenGenerator;
        private readonly IAntiforgeryTokenSerializer _tokenSerializer;
        private readonly IAntiforgeryTokenStore _tokenStore;

        public Antiforgery(
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

        /// <summary>
        /// Generates an anti-forgery token for this request. This token can
        /// be validated by calling the Validate() method.
        /// </summary>
        /// <param name="context">The HTTP context associated with the current call.</param>
        /// <returns>An HTML string corresponding to an &lt;input type="hidden"&gt;
        /// element. This element should be put inside a &lt;form&gt;.</returns>
        /// <remarks>
        /// This method has a side effect:
        /// A response cookie is set if there is no valid cookie associated with the request.
        /// </remarks>
        public string GetHtml([NotNull] HttpContext context)
        {
            CheckSSLConfig(context);

            var cookieToken = GetCookieTokenDoesNotThrow(context);
            var tokenSet = GetTokens(context, cookieToken);
            cookieToken = tokenSet.CookieToken;
            var formToken = tokenSet.FormToken;

            SaveCookieTokenAndHeader(context, cookieToken);

            var inputTag = string.Format(
                "<input name=\"{0}\" type=\"{1}\" value=\"{2}\" />",
                _htmlEncoder.HtmlEncode(_options.FormFieldName),
                _htmlEncoder.HtmlEncode("hidden"),
                _htmlEncoder.HtmlEncode(_tokenSerializer.Serialize(formToken)));
            return inputTag;
        }

        /// <summary>
        /// Generates an anti-forgery token pair (cookie and form token) for this request.
        /// This method is similar to GetHtml(HttpContext context), but this method gives the caller control
        /// over how to persist the returned values. To validate these tokens, call the
        /// appropriate overload of Validate.
        /// </summary>
        /// <param name="context">The HTTP context associated with the current call.</param>
        /// <param name="oldCookieToken">The anti-forgery token - if any - that already existed
        /// for this request. May be null. The anti-forgery system will try to reuse this cookie
        /// value when generating a matching form token.</param>
        /// <remarks>
        /// Unlike the GetHtml(HttpContext context) method, this method has no side effect. The caller
        /// is responsible for setting the response cookie and injecting the returned
        /// form token as appropriate.
        /// </remarks>
        public AntiforgeryTokenSet GetTokens([NotNull] HttpContext context, string oldCookieToken)
        {
            // Will contain a new cookie value if the old cookie token
            // was null or invalid. If this value is non-null when the method completes, the caller
            // must persist this value in the form of a response cookie, and the existing cookie value
            // should be discarded. If this value is null when the method completes, the existing
            // cookie value was valid and needn't be modified.
            CheckSSLConfig(context);

            var deserializedcookieToken = DeserializeTokenDoesNotThrow(oldCookieToken);
            var tokenSet = GetTokens(context, deserializedcookieToken);

            var serializedCookieToken = Serialize(tokenSet.CookieToken);
            var serializedFormToken = Serialize(tokenSet.FormToken);
            return new AntiforgeryTokenSet(serializedFormToken, serializedCookieToken);
        }

        /// <summary>
        /// Validates an anti-forgery token that was supplied for this request.
        /// The anti-forgery token may be generated by calling GetHtml(HttpContext context).
        /// </summary>
        /// <param name="context">The HTTP context associated with the current call.</param>
        public async Task ValidateAsync([NotNull] HttpContext context)
        {
            CheckSSLConfig(context);

            // Extract cookie & form tokens
            var cookieToken = _tokenStore.GetCookieToken(context);
            var formToken = await _tokenStore.GetFormTokenAsync(context);

            // Validate
            _tokenGenerator.ValidateTokens(context, cookieToken, formToken);
        }

        /// <summary>
        /// Validates an anti-forgery token pair that was generated by the GetTokens method.
        /// </summary>
        /// <param name="context">The HTTP context associated with the current call.</param>
        /// <param name="cookieToken">The token that was supplied in the request cookie.</param>
        /// <param name="formToken">The token that was supplied in the request form body.</param>
        public void Validate([NotNull] HttpContext context, string cookieToken, string formToken)
        {
            CheckSSLConfig(context);

            // Extract cookie & form tokens
            var deserializedCookieToken = DeserializeToken(cookieToken);
            var deserializedFormToken = DeserializeToken(formToken);

            // Validate
            _tokenGenerator.ValidateTokens(
                context,
                deserializedCookieToken,
                deserializedFormToken);
        }

        /// <summary>
        /// Validates an anti-forgery token pair that was generated by the GetTokens method.
        /// </summary>
        /// <param name="context">The HTTP context associated with the current call.</param>
        /// <param name="AntiforgeryTokenSet">The anti-forgery token pair (cookie and form token) for this request.
        /// </param>
        public void Validate([NotNull] HttpContext context, AntiforgeryTokenSet AntiforgeryTokenSet)
        {
            Validate(context, AntiforgeryTokenSet.CookieToken, AntiforgeryTokenSet.FormToken);
        }

        /// <summary>
        /// Generates and sets an anti-forgery cookie if one is not available or not valid. Also sets response headers.
        /// </summary>
        /// <param name="context">The HTTP context associated with the current call.</param>
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
            [NotNull] HttpContext httpContext,
            AntiforgeryToken cookieToken)
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
                httpContext.Response.Headers.Set("X-Frame-Options", "SAMEORIGIN");
            }
        }

        private void CheckSSLConfig(HttpContext httpContext)
        {
            if (_options.RequireSSL && !httpContext.Request.IsHttps)
            {
                throw new InvalidOperationException(Resources.AntiforgeryWorker_RequireSSL);
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

        private AntiforgeryToken GetCookieTokenDoesNotThrow(HttpContext httpContext)
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

        private AntiforgeryTokenSetInternal GetTokens(HttpContext httpContext, AntiforgeryToken cookieToken)
        {
            var newCookieToken = ValidateAndGenerateNewCookieToken(cookieToken);
            if (newCookieToken != null)
            {
                cookieToken = newCookieToken;
            }
            var formToken = _tokenGenerator.GenerateFormToken(
                httpContext,
                cookieToken);

            return new AntiforgeryTokenSetInternal()
            {
                // Note : The new cookie would be null if the old cookie is valid.
                CookieToken = newCookieToken,
                FormToken = formToken
            };
        }

        private string Serialize(AntiforgeryToken token)
        {
            return (token != null) ? _tokenSerializer.Serialize(token) : null;
        }

        private class AntiforgeryTokenSetInternal
        {
            public AntiforgeryToken FormToken { get; set; }

            public AntiforgeryToken CookieToken { get; set; }
        }
    }
}