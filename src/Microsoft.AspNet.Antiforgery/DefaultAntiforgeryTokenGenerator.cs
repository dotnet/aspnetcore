// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Antiforgery
{
    public class DefaultAntiforgeryTokenGenerator : IAntiforgeryTokenGenerator
    {
        private readonly IClaimUidExtractor _claimUidExtractor;
        private readonly IAntiforgeryAdditionalDataProvider _additionalDataProvider;

        public DefaultAntiforgeryTokenGenerator(
            IClaimUidExtractor claimUidExtractor,
            IAntiforgeryAdditionalDataProvider additionalDataProvider)
        {
            _claimUidExtractor = claimUidExtractor;
            _additionalDataProvider = additionalDataProvider;
        }

        public AntiforgeryToken GenerateCookieToken()
        {
            return new AntiforgeryToken()
            {
                // SecurityToken will be populated automatically.
                IsSessionToken = true
            };
        }

        public AntiforgeryToken GenerateFormToken(
            HttpContext httpContext,
            AntiforgeryToken cookieToken)
        {
            Debug.Assert(IsCookieTokenValid(cookieToken));

            var formToken = new AntiforgeryToken()
            {
                SecurityToken = cookieToken.SecurityToken,
                IsSessionToken = false
            };

            var isIdentityAuthenticated = false;
            var identity = httpContext.User?.Identity as ClaimsIdentity;

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
                    Resources.FormatAntiforgeryTokenValidator_AuthenticatedUserWithoutUsername(
                        identity.GetType(),
                        nameof(IIdentity.IsAuthenticated),
                        "true",
                        nameof(IIdentity.Name),
                        nameof(IAntiforgeryAdditionalDataProvider),
                        nameof(DefaultAntiforgeryAdditionalDataProvider)));
            }

            return formToken;
        }

        public bool IsCookieTokenValid(AntiforgeryToken cookieToken)
        {
            return (cookieToken != null && cookieToken.IsSessionToken);
        }

        public void ValidateTokens(
            HttpContext httpContext,
            AntiforgeryToken sessionToken,
            AntiforgeryToken fieldToken)
        {
            if (sessionToken == null)
            {
                throw new ArgumentNullException(
                    nameof(sessionToken),
                    Resources.Antiforgery_CookieToken_MustBeProvided_Generic);
            }

            if (fieldToken == null)
            {
                throw new ArgumentNullException(
                    nameof(fieldToken),
                    Resources.Antiforgery_FormToken_MustBeProvided_Generic);
            }

            // Do the tokens have the correct format?
            if (!sessionToken.IsSessionToken || fieldToken.IsSessionToken)
            {
                throw new InvalidOperationException(Resources.AntiforgeryToken_TokensSwapped);
            }

            // Are the security tokens embedded in each incoming token identical?
            if (!Equals(sessionToken.SecurityToken, fieldToken.SecurityToken))
            {
                throw new InvalidOperationException(Resources.AntiforgeryToken_SecurityTokenMismatch);
            }

            // Is the incoming token meant for the current user?
            var currentUsername = string.Empty;
            BinaryBlob currentClaimUid = null;

            var identity = httpContext.User?.Identity as ClaimsIdentity;
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

            if (!string.Equals(fieldToken.Username,
                                currentUsername,
                                (useCaseSensitiveUsernameComparison) ?
                                                 StringComparison.Ordinal :
                                                 StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    Resources.FormatAntiforgeryToken_UsernameMismatch(fieldToken.Username, currentUsername));
            }

            if (!Equals(fieldToken.ClaimUid, currentClaimUid))
            {
                throw new InvalidOperationException(Resources.AntiforgeryToken_ClaimUidMismatch);
            }

            // Is the AdditionalData valid?
            if (_additionalDataProvider != null &&
                !_additionalDataProvider.ValidateAdditionalData(httpContext, fieldToken.AdditionalData))
            {
                throw new InvalidOperationException(Resources.AntiforgeryToken_AdditionalDataCheckFailed);
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