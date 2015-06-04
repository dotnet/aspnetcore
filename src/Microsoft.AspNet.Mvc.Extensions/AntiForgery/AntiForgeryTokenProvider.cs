// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Extensions;

namespace Microsoft.AspNet.Mvc
{
    internal sealed class AntiForgeryTokenProvider : IAntiForgeryTokenValidator, IAntiForgeryTokenGenerator
    {
        private readonly IClaimUidExtractor _claimUidExtractor;
        private readonly AntiForgeryOptions _config;
        private readonly IAntiForgeryAdditionalDataProvider _additionalDataProvider;

        internal AntiForgeryTokenProvider(AntiForgeryOptions config,
                               IClaimUidExtractor claimUidExtractor,
                               IAntiForgeryAdditionalDataProvider additionalDataProvider)
        {
            _config = config;
            _claimUidExtractor = claimUidExtractor;
            _additionalDataProvider = additionalDataProvider;
        }

        public AntiForgeryToken GenerateCookieToken()
        {
            return new AntiForgeryToken()
            {
                // SecurityToken will be populated automatically.
                IsSessionToken = true
            };
        }

        public AntiForgeryToken GenerateFormToken(HttpContext httpContext,
                                                  ClaimsIdentity identity,
                                                  AntiForgeryToken cookieToken)
        {
            Debug.Assert(IsCookieTokenValid(cookieToken));

            var formToken = new AntiForgeryToken()
            {
                SecurityToken = cookieToken.SecurityToken,
                IsSessionToken = false
            };

            var isIdentityAuthenticated = false;

            // populate Username and ClaimUid
            if (identity != null && identity.IsAuthenticated)
            {
                isIdentityAuthenticated = true;
                formToken.ClaimUid = GetClaimUidBlob(_claimUidExtractor.ExtractClaimUid(identity));
                if (formToken.ClaimUid == null)
                {
                    formToken.Username = identity.Name;
                }
            }

            // populate AdditionalData
            if (_additionalDataProvider != null)
            {
                formToken.AdditionalData = _additionalDataProvider.GetAdditionalData(httpContext);
            }

            if (isIdentityAuthenticated
                && string.IsNullOrEmpty(formToken.Username)
                && formToken.ClaimUid == null
                && string.IsNullOrEmpty(formToken.AdditionalData))
            {
                // Application says user is authenticated, but we have no identifier for the user.
                throw new InvalidOperationException(
                    Resources.FormatTokenValidator_AuthenticatedUserWithoutUsername(identity.GetType()));
            }

            return formToken;
        }

        public bool IsCookieTokenValid(AntiForgeryToken cookieToken)
        {
            return (cookieToken != null && cookieToken.IsSessionToken);
        }

        public void ValidateTokens(
            HttpContext httpContext,
            ClaimsIdentity identity,
            AntiForgeryToken sessionToken,
            AntiForgeryToken fieldToken)
        {
            // Were the tokens even present at all?
            if (sessionToken == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatAntiForgeryToken_CookieMissing(_config.CookieName));
            }
            if (fieldToken == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatAntiForgeryToken_FormFieldMissing(_config.FormFieldName));
            }

            // Do the tokens have the correct format?
            if (!sessionToken.IsSessionToken || fieldToken.IsSessionToken)
            {
                throw new InvalidOperationException(
                    Resources.FormatAntiForgeryToken_TokensSwapped(_config.CookieName, _config.FormFieldName));
            }

            // Are the security tokens embedded in each incoming token identical?
            if (!Equals(sessionToken.SecurityToken, fieldToken.SecurityToken))
            {
                throw new InvalidOperationException(Resources.AntiForgeryToken_SecurityTokenMismatch);
            }

            // Is the incoming token meant for the current user?
            var currentUsername = string.Empty;
            BinaryBlob currentClaimUid = null;

            if (identity != null && identity.IsAuthenticated)
            {
                currentClaimUid = GetClaimUidBlob(_claimUidExtractor.ExtractClaimUid(identity));
                if (currentClaimUid == null)
                {
                    currentUsername = identity.Name ?? string.Empty;
                }
            }

            // OpenID and other similar authentication schemes use URIs for the username.
            // These should be treated as case-sensitive.
            var useCaseSensitiveUsernameComparison =
                currentUsername.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                currentUsername.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

            if (!String.Equals(fieldToken.Username,
                                currentUsername,
                                (useCaseSensitiveUsernameComparison) ?
                                                 StringComparison.Ordinal :
                                                 StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    Resources.FormatAntiForgeryToken_UsernameMismatch(fieldToken.Username, currentUsername));
            }

            if (!Equals(fieldToken.ClaimUid, currentClaimUid))
            {
                throw new InvalidOperationException(Resources.AntiForgeryToken_ClaimUidMismatch);
            }

            // Is the AdditionalData valid?
            if (_additionalDataProvider != null &&
                !_additionalDataProvider.ValidateAdditionalData(httpContext, fieldToken.AdditionalData))
            {
                throw new InvalidOperationException(Resources.AntiForgeryToken_AdditionalDataCheckFailed);
            }
        }

        private static BinaryBlob GetClaimUidBlob(string base64ClaimUid)
        {
            if (base64ClaimUid == null)
            {
                return null;
            }

            return new BinaryBlob(256, Convert.FromBase64String(base64ClaimUid));
        }
    }
}