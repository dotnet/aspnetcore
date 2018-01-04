// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class ApplicationValidator<TApplication> : IApplicationValidator<TApplication>
        where TApplication : class
    {
        public ApplicationValidator(ApplicationErrorDescriber errorDescriber)
        {
            ErrorDescriber = errorDescriber;
        }

        public ApplicationErrorDescriber ErrorDescriber { get; }

        public async Task<IdentityServiceResult> ValidateAsync(
            ApplicationManager<TApplication> manager,
            TApplication application)
        {
            var errors = new List<IdentityServiceError>();
            await ValidateNameAsync(manager, application, errors);
            await ValidateClientIdAsync(manager, application, errors);

            return errors.Count > 0 ? IdentityServiceResult.Failed(errors.ToArray()) : IdentityServiceResult.Success;
        }

        private async Task ValidateNameAsync(
            ApplicationManager<TApplication> manager,
            TApplication application,
            IList<IdentityServiceError> errors)
        {
            var applicationName = await manager.GetApplicationNameAsync(application);
            if (string.IsNullOrWhiteSpace(applicationName))
            {
                errors.Add(ErrorDescriber.InvalidApplicationName(applicationName));
            }
            else if (!string.IsNullOrEmpty(manager.Options.AllowedNameCharacters) &&
                applicationName.Any(c => !manager.Options.AllowedNameCharacters.Contains(c)))
            {
                errors.Add(ErrorDescriber.InvalidApplicationName(applicationName));
            }
            else if (manager.Options.MaxApplicationNameLength.HasValue &&
                applicationName.Length > manager.Options.MaxApplicationNameLength)
            {
                errors.Add(ErrorDescriber.InvalidApplicationName(applicationName));
            }
            else
            {
                var otherApplication = await manager.FindByNameAsync(applicationName);
                if (otherApplication != null &&
                    !string.Equals(
                        await manager.GetApplicationIdAsync(otherApplication),
                        await manager.GetApplicationIdAsync(application),
                        StringComparison.Ordinal))
                {
                    errors.Add(ErrorDescriber.DuplicateApplicationName(applicationName));
                }
            }
        }

        private async Task ValidateClientIdAsync(
            ApplicationManager<TApplication> manager,
            TApplication application,
            IList<IdentityServiceError> errors)
        {
            var clientId = await manager.GetApplicationClientIdAsync(application);
            if (string.IsNullOrWhiteSpace(clientId))
            {
                errors.Add(ErrorDescriber.InvalidApplicationClientId(clientId));
            }
            else if (!string.IsNullOrEmpty(manager.Options.AllowedClientIdCharacters) &&
                clientId.Any(c => !manager.Options.AllowedClientIdCharacters.Contains(c)))
            {
                errors.Add(ErrorDescriber.InvalidApplicationClientId(clientId));
            }
            else if (manager.Options.MaxApplicationNameLength.HasValue &&
                clientId.Length > manager.Options.MaxApplicationNameLength)
            {
                errors.Add(ErrorDescriber.InvalidApplicationClientId(clientId));
            }
            else
            {
                var otherApplication = await manager.FindByClientIdAsync(clientId);
                if (otherApplication != null &&
                    !string.Equals(
                        await manager.GetApplicationIdAsync(otherApplication),
                        await manager.GetApplicationIdAsync(application),
                        StringComparison.Ordinal))
                {
                    errors.Add(ErrorDescriber.DuplicateApplicationClientId(clientId));
                }
            }
        }

        public async Task<IdentityServiceResult> ValidateLogoutUriAsync(
            ApplicationManager<TApplication> manager,
            TApplication application,
            string logoutUri)
        {
            var errors = new List<IdentityServiceError>();

            var logoutUris = await manager.FindRegisteredLogoutUrisAsync(application);
            if (logoutUris.Contains(logoutUri, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add(ErrorDescriber.DuplicateLogoutUri(logoutUri));
            }

            if (!manager.Options.AllowedLogoutUris.Contains(logoutUri, StringComparer.OrdinalIgnoreCase))
            {
                if (!Uri.TryCreate(logoutUri, UriKind.Absolute, out var parsedUri))
                {
                    errors.Add(ErrorDescriber.InvalidLogoutUri(logoutUri));
                }
                else
                {
                    if (!parsedUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add(ErrorDescriber.NoHttpsUri(logoutUri));
                    }

                    var redirectUris = await manager.FindRegisteredUrisAsync(application);
                    var regularRedirectUris = redirectUris.Except(manager.Options.AllowedRedirectUris, StringComparer.Ordinal);
                    var regularLogoutUris = logoutUris.Except(manager.Options.AllowedLogoutUris, StringComparer.Ordinal);

                    var allApplicationUris = regularLogoutUris.Concat(regularRedirectUris);

                    foreach (var nonSpecialUri in allApplicationUris)
                    {
                        var existingUri = new Uri(nonSpecialUri, UriKind.Absolute);
                        if (!parsedUri.Host.Equals(existingUri.Host, StringComparison.OrdinalIgnoreCase))
                        {
                            errors.Add(ErrorDescriber.DifferentDomains());
                            break;
                        }
                    }
                }
            }

            return errors.Count > 0 ? IdentityServiceResult.Failed(errors.ToArray()) : IdentityServiceResult.Success;
        }

        public async Task<IdentityServiceResult> ValidateRedirectUriAsync(
            ApplicationManager<TApplication> manager,
            TApplication application,
            string redirectUri)
        {
            var errors = new List<IdentityServiceError>();

            var redirectUris = await manager.FindRegisteredUrisAsync(application);
            if (redirectUris.Contains(redirectUri, StringComparer.OrdinalIgnoreCase))
            {
                errors.Add(ErrorDescriber.DuplicateRedirectUri(redirectUri));
            }

            if (!manager.Options.AllowedRedirectUris.Contains(redirectUri, StringComparer.OrdinalIgnoreCase))
            {
                if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var parsedUri))
                {
                    errors.Add(ErrorDescriber.InvalidRedirectUri(redirectUri));
                }
                else
                {
                    if (!parsedUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add(ErrorDescriber.NoHttpsUri(redirectUri));
                    }

                    var logoutUris = await manager.FindRegisteredUrisAsync(application);
                    var regularLogoutUris = logoutUris.Except(manager.Options.AllowedLogoutUris, StringComparer.Ordinal);
                    var regularRedirectUris = redirectUris.Except(manager.Options.AllowedRedirectUris, StringComparer.Ordinal);

                    var allApplicationUris = regularRedirectUris.Concat(regularLogoutUris);

                    foreach (var nonSpecialUri in allApplicationUris)
                    {
                        var existingUri = new Uri(nonSpecialUri, UriKind.Absolute);
                        if (!parsedUri.Host.Equals(existingUri.Host, StringComparison.OrdinalIgnoreCase))
                        {
                            errors.Add(ErrorDescriber.DifferentDomains());
                            break;
                        }
                    }
                }
            }

            return errors.Count > 0 ? IdentityServiceResult.Failed(errors.ToArray()) : IdentityServiceResult.Success;
        }

        public async Task<IdentityServiceResult> ValidateScopeAsync(
            ApplicationManager<TApplication> manager,
            TApplication application,
            string scope)
        {
            var errors = new List<IdentityServiceError>();
            if (string.IsNullOrWhiteSpace(scope))
            {
                errors.Add(ErrorDescriber.InvalidScope(scope));
            }
            else if (!string.IsNullOrEmpty(manager.Options.AllowedScopeCharacters) &&
                scope.Any(c => !manager.Options.AllowedScopeCharacters.Contains(c)))
            {
                errors.Add(ErrorDescriber.InvalidScope(scope));
            }
            else if (manager.Options.MaxScopeLength.HasValue &&
                scope.Length > manager.Options.MaxScopeLength)
            {
                errors.Add(ErrorDescriber.InvalidScope(scope));
            }
            else
            {
                var scopes = await manager.FindScopesAsync(application);
                if (scopes != null && scopes.Contains(scope, StringComparer.OrdinalIgnoreCase))
                {
                    errors.Add(ErrorDescriber.DuplicateScope(scope));
                }
            }

            return errors.Count > 0 ? IdentityServiceResult.Failed(errors.ToArray()) : IdentityServiceResult.Success;
        }

        public Task<IdentityServiceResult> ValidateClaimAsync(ApplicationManager<TApplication> manager, TApplication application, Claim claim)
        {
            return Task.FromResult(IdentityServiceResult.Success);
        }
    }
}
