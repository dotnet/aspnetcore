// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class ClientApplicationValidator<TApplication> : IClientIdValidator, IRedirectUriResolver, IScopeResolver
        where TApplication : class
    {
        private readonly IOptions<IdentityServiceOptions> _options;
        private readonly SessionManager _sessionManager;
        private readonly ApplicationManager<TApplication> _applicationManager;
        private readonly ProtocolErrorProvider _errorProvider;

        public ClientApplicationValidator(
            IOptions<IdentityServiceOptions> options,
            SessionManager sessionManager,
            ApplicationManager<TApplication> applicationManager,
            ProtocolErrorProvider errorProvider)
        {
            _options = options;
            _sessionManager = sessionManager;
            _applicationManager = applicationManager;
            _errorProvider = errorProvider;
        }

        public Task<bool> ValidateClientCredentialsAsync(string clientId, string clientSecret)
        {
            return _applicationManager.ValidateClientCredentialsAsync(clientId, clientSecret);
        }

        public async Task<bool> ValidateClientIdAsync(string clientId)
        {
            return await _applicationManager.FindByClientIdAsync(clientId) != null;
        }

        public async Task<RedirectUriResolutionResult> ResolveLogoutUriAsync(string clientId, string logoutUrl)
        {
            if (logoutUrl == null)
            {
                return RedirectUriResolutionResult.Valid(logoutUrl);
            }

            var sessions = await _sessionManager.GetCurrentSessions();
            if (clientId == null)
            {
                foreach (var identity in sessions.Identities)
                {
                    if (identity.HasClaim(IdentityServiceClaimTypes.LogoutRedirectUri, logoutUrl))
                    {
                        return RedirectUriResolutionResult.Valid(logoutUrl);
                    }
                }

                return RedirectUriResolutionResult.Invalid(null);
            }

            foreach (var identity in sessions.Identities)
            {
                if (identity.HasClaim(IdentityServiceClaimTypes.ClientId, clientId) &&
                    identity.HasClaim(IdentityServiceClaimTypes.LogoutRedirectUri, logoutUrl))
                {
                    return RedirectUriResolutionResult.Valid(logoutUrl);
                }
            }

            return RedirectUriResolutionResult.Invalid(null);
        }

        public async Task<RedirectUriResolutionResult> ResolveRedirectUriAsync(string clientId, string redirectUrl)
        {
            if (clientId == null)
            {
                throw new ArgumentNullException(nameof(clientId));
            }

            var app = await _applicationManager.FindByClientIdAsync(clientId);
            if (app == null)
            {
                return RedirectUriResolutionResult.Invalid(_errorProvider.InvalidClientId(clientId));
            }

            var redirectUris = await _applicationManager.FindRegisteredUrisAsync(app);
            if (redirectUrl == null && redirectUris.Count() == 1)
            {
                return RedirectUriResolutionResult.Valid(redirectUris.Single());
            }

            foreach (var uri in redirectUris)
            {
                if (string.Equals(uri, redirectUrl, StringComparison.Ordinal))
                {
                    return RedirectUriResolutionResult.Valid(redirectUrl);
                }
            }

            return RedirectUriResolutionResult.Invalid(_errorProvider.InvalidRedirectUri(redirectUrl));
        }

        public async Task<ScopeResolutionResult> ResolveScopesAsync(string clientId, IEnumerable<string> scopes)
        {
            var authorizedParty = await _applicationManager.FindByClientIdAsync(clientId);
            var authorizedPartyScopes = await _applicationManager.FindScopesAsync(authorizedParty);

            var result = new List<ApplicationScope>();

            string resourceName = null;
            TApplication resourceApplication = null;
            IEnumerable<string> resourceApplicationScopes = null;

            foreach (var scope in scopes)
            {
                var (wellFormed, canonical, name, scopeValue) = ParseScope(scope);
                if (!wellFormed)
                {
                    return ScopeResolutionResult.Invalid(_errorProvider.InvalidScope(scope));
                }

                if (canonical && authorizedPartyScopes.Any(s => s.Equals(scope, StringComparison.Ordinal)))
                {
                    result.Add(ApplicationScope.CanonicalScopes[scope]);
                }
                if (canonical)
                {
                    // We purposely ignore canonical scopes not allowed by the client application.
                    continue;
                }

                resourceName = resourceName ?? name;
                if (resourceName != null && !resourceName.Equals(name, StringComparison.Ordinal))
                {
                    return ScopeResolutionResult.Invalid(_errorProvider.MultipleResourcesNotSupported(resourceName, name));
                }

                if (resourceApplicationScopes == null)
                {
                    resourceApplication = await _applicationManager.FindByNameAsync(resourceName);
                    if (resourceApplication == null)
                    {
                        return ScopeResolutionResult.Invalid(_errorProvider.InvalidScope(scope));
                    }

                    resourceApplicationScopes = await _applicationManager.FindScopesAsync(resourceApplication);
                }

                if (!resourceApplicationScopes.Contains(scopeValue, StringComparer.Ordinal))
                {
                    return ScopeResolutionResult.Invalid(_errorProvider.InvalidScope(scope));
                }
                else
                {
                    var resourceClientId = await _applicationManager.GetApplicationClientIdAsync(resourceApplication);
                    result.Add(new ApplicationScope(resourceClientId, scopeValue));
                }
            }

            return ScopeResolutionResult.Valid(result);
        }

        private (bool wellFormed, bool canonical, string name, string value) ParseScope(string scope)
        {
            if (ApplicationScope.CanonicalScopes.TryGetValue(scope, out var canonicalScope))
            {
                return (true, true, null, scope);
            }

            var prefix = _options.Value.Issuer;
            if (scope.StartsWith(prefix))
            {
                var start = prefix.EndsWith("/") ? prefix.Length : prefix.Length + 1;
                var end = scope.IndexOf('/', start);
                if (end == -1 | end == scope.Length - 1)
                {
                    return (false, false, null, null);
                }
                var applicationName = scope.Substring(start, end - start);
                var scopeValue = scope.Substring(end + 1);
                return (true, false, applicationName, scopeValue);
            }
            else
            {
                return (false, false, null, null);
            }
        }
    }
}
