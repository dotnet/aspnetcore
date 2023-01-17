// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Antiforgery;

internal sealed class DefaultAntiforgeryTokenGenerator : IAntiforgeryTokenGenerator
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

    /// <inheritdoc />
    public AntiforgeryToken GenerateCookieToken()
    {
        return new AntiforgeryToken()
        {
            // SecurityToken will be populated automatically.
            IsCookieToken = true
        };
    }

    /// <inheritdoc />
    public AntiforgeryToken GenerateRequestToken(
        HttpContext httpContext,
        AntiforgeryToken cookieToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(cookieToken);

        if (!IsCookieTokenValid(cookieToken))
        {
            throw new ArgumentException(
                Resources.Antiforgery_CookieToken_IsInvalid,
                nameof(cookieToken));
        }

        var requestToken = new AntiforgeryToken()
        {
            SecurityToken = cookieToken.SecurityToken,
            IsCookieToken = false
        };

        var isIdentityAuthenticated = false;

        // populate Username and ClaimUid
        var authenticatedIdentity = GetAuthenticatedIdentity(httpContext.User);
        if (authenticatedIdentity != null)
        {
            isIdentityAuthenticated = true;
            requestToken.ClaimUid = GetClaimUidBlob(_claimUidExtractor.ExtractClaimUid(httpContext.User));

            if (requestToken.ClaimUid == null)
            {
                requestToken.Username = authenticatedIdentity.Name;
            }
        }

        // populate AdditionalData
        if (_additionalDataProvider != null)
        {
            requestToken.AdditionalData = _additionalDataProvider.GetAdditionalData(httpContext);
        }

        if (isIdentityAuthenticated
            && string.IsNullOrEmpty(requestToken.Username)
            && requestToken.ClaimUid == null
            && string.IsNullOrEmpty(requestToken.AdditionalData))
        {
            // Application says user is authenticated, but we have no identifier for the user.
            throw new InvalidOperationException(
                Resources.FormatAntiforgeryTokenValidator_AuthenticatedUserWithoutUsername(
                    authenticatedIdentity?.GetType() ?? typeof(ClaimsIdentity),
                    nameof(IIdentity.IsAuthenticated),
                    "true",
                    nameof(IIdentity.Name),
                    nameof(IAntiforgeryAdditionalDataProvider),
                    nameof(DefaultAntiforgeryAdditionalDataProvider)));
        }

        return requestToken;
    }

    /// <inheritdoc />
    public bool IsCookieTokenValid(AntiforgeryToken? cookieToken)
    {
        return cookieToken != null && cookieToken.IsCookieToken;
    }

    /// <inheritdoc />
    public bool TryValidateTokenSet(
        HttpContext httpContext,
        AntiforgeryToken cookieToken,
        AntiforgeryToken requestToken,
        [NotNullWhen(false)] out string? message)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (cookieToken == null)
        {
            throw new ArgumentNullException(
                nameof(cookieToken),
                Resources.Antiforgery_CookieToken_MustBeProvided_Generic);
        }

        if (requestToken == null)
        {
            throw new ArgumentNullException(
                nameof(requestToken),
                Resources.Antiforgery_RequestToken_MustBeProvided_Generic);
        }

        // Do the tokens have the correct format?
        if (!cookieToken.IsCookieToken || requestToken.IsCookieToken)
        {
            message = Resources.AntiforgeryToken_TokensSwapped;
            return false;
        }

        // Are the security tokens embedded in each incoming token identical?
        if (!object.Equals(cookieToken.SecurityToken, requestToken.SecurityToken))
        {
            message = Resources.AntiforgeryToken_SecurityTokenMismatch;
            return false;
        }

        // Is the incoming token meant for the current user?
        var currentUsername = string.Empty;
        BinaryBlob? currentClaimUid = null;

        var authenticatedIdentity = GetAuthenticatedIdentity(httpContext.User);
        if (authenticatedIdentity != null)
        {
            currentClaimUid = GetClaimUidBlob(_claimUidExtractor.ExtractClaimUid(httpContext.User));
            if (currentClaimUid == null)
            {
                currentUsername = authenticatedIdentity.Name ?? string.Empty;
            }
        }

        // OpenID and other similar authentication schemes use URIs for the username.
        // These should be treated as case-sensitive.
        var comparer = StringComparer.OrdinalIgnoreCase;
        if (currentUsername.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            currentUsername.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            comparer = StringComparer.Ordinal;
        }

        if (!comparer.Equals(requestToken.Username, currentUsername))
        {
            message = Resources.FormatAntiforgeryToken_UsernameMismatch(requestToken.Username, currentUsername);
            return false;
        }

        if (!object.Equals(requestToken.ClaimUid, currentClaimUid))
        {
            message = Resources.AntiforgeryToken_ClaimUidMismatch;
            return false;
        }

        // Is the AdditionalData valid?
        if (_additionalDataProvider != null &&
            !_additionalDataProvider.ValidateAdditionalData(httpContext, requestToken.AdditionalData))
        {
            message = Resources.AntiforgeryToken_AdditionalDataCheckFailed;
            return false;
        }

        message = null;
        return true;
    }

    private static BinaryBlob? GetClaimUidBlob(string? base64ClaimUid)
    {
        if (base64ClaimUid == null)
        {
            return null;
        }

        return new BinaryBlob(256, Convert.FromBase64String(base64ClaimUid));
    }

    private static ClaimsIdentity? GetAuthenticatedIdentity(ClaimsPrincipal? claimsPrincipal)
    {
        if (claimsPrincipal == null)
        {
            return null;
        }

        var identitiesList = claimsPrincipal.Identities as List<ClaimsIdentity>;
        if (identitiesList != null)
        {
            for (var i = 0; i < identitiesList.Count; i++)
            {
                if (identitiesList[i].IsAuthenticated)
                {
                    return identitiesList[i];
                }
            }
        }
        else
        {
            foreach (var identity in claimsPrincipal.Identities)
            {
                if (identity.IsAuthenticated)
                {
                    return identity;
                }
            }
        }

        return null;
    }
}
