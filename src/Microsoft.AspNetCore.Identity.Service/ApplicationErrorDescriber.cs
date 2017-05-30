// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class ApplicationErrorDescriber
    {
        public virtual IdentityServiceError InvalidApplicationName(string applicationName) => new IdentityServiceError
        {
            Code = nameof(InvalidApplicationName),
            Description = $"The application name '{applicationName}' is not valid."
        };

        public virtual IdentityServiceError DuplicateApplicationName(string applicationName) => new IdentityServiceError
        {
            Code = nameof(DuplicateApplicationName),
            Description = $"An application with name '{applicationName}' already exists."
        };

        public virtual IdentityServiceError InvalidApplicationClientId(string clientId) => new IdentityServiceError
        {
            Code = nameof(InvalidApplicationClientId),
            Description = $"The application client ID '{clientId}' is not valid."
        };

        public virtual IdentityServiceError DuplicateApplicationClientId(string clientId) => new IdentityServiceError
        {
            Code = nameof(DuplicateApplicationClientId),
            Description = $"An application with client ID '{clientId}' already exists."
        };

        public virtual IdentityServiceError DuplicateLogoutUri(string logoutUri) => new IdentityServiceError
        {
            Code = nameof(DuplicateLogoutUri),
            Description = $"The application already contains a logout uri '{logoutUri}'."
        };

        public virtual IdentityServiceError InvalidLogoutUri(string logoutUri) => new IdentityServiceError
        {
            Code = nameof(InvalidLogoutUri),
            Description = $"The logout uri '{logoutUri}' is not valid."
        };

        public virtual IdentityServiceError NoHttpsUri(string logoutUri) => new IdentityServiceError
        {
            Code = nameof(NoHttpsUri),
            Description = $"The uri '{logoutUri}' must use https."
        };

        public virtual IdentityServiceError DifferentDomains() => new IdentityServiceError
        {
            Code = nameof(DifferentDomains),
            Description = $"All the URIs in an application must have the same domain."
        };

        public virtual IdentityServiceError DuplicateRedirectUri(string redirectUri) => new IdentityServiceError
        {
            Code = nameof(DuplicateRedirectUri),
            Description = $"The application already contains a redirect uri '{redirectUri}'."
        };

        public virtual IdentityServiceError InvalidRedirectUri(string redirectUri) => new IdentityServiceError
        {
            Code = nameof(InvalidRedirectUri),
            Description = $"The redirect URI '{redirectUri}' is not valid."
        };

        public virtual IdentityServiceError InvalidScope(string scope) => new IdentityServiceError
        {
            Code = nameof(InvalidScope),
            Description = $"The scope '{scope}' is not valid."
        };

        public virtual IdentityServiceError DuplicateScope(string scope) => new IdentityServiceError
        {
            Code = nameof(DuplicateScope),
            Description = $"The application already contains a scope '{scope}'."
        };

        public virtual IdentityServiceError ApplicationAlreadyHasClientSecret() => new IdentityServiceError {
            Code = nameof(ApplicationAlreadyHasClientSecret),
            Description = $"The application already has a client secret."
        };

        public virtual IdentityServiceError RedirectUriNotFound(string redirectUri) => new IdentityServiceError
        {
            Code = nameof(RedirectUriNotFound),
            Description = $"The redirect uri '{redirectUri}' can not be found."
        };

        public virtual IdentityServiceError LogoutUriNotFound(string logoutUri) => new IdentityServiceError
        {
            Code = nameof(LogoutUriNotFound),
            Description = $"The logout uri '{logoutUri}' can not be found."
        };

        public virtual IdentityServiceError ConcurrencyFailure() => new IdentityServiceError
        {
            Code = nameof(ConcurrencyFailure),
            Description = $"Optimistic concurrency failure, object has been modified."
        };

        public virtual IdentityServiceError ScopeNotFound(string scope) => new IdentityServiceError
        {
            Code = nameof(ScopeNotFound),
            Description = $"The scope '{scope}' can not be found."
        };
    }
}
