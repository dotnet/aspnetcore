// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity.Service.InMemory.Test
{
    public class InMemoryStore<TApplication>
        : IRedirectUriStore<TApplication>,
        IApplicationClaimStore<TApplication>,
        IApplicationClientSecretStore<TApplication>,
        IApplicationScopeStore<TApplication>,
        IQueryableApplicationStore<TApplication>
        where TApplication : TestApplication
    {
        private readonly Dictionary<string, TApplication> _applications = new Dictionary<string, TApplication>();

        public IQueryable<TApplication> Applications => _applications.Values.AsQueryable();

        public Task AddClaimsAsync(TApplication application, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            foreach (var claim in claims)
            {
                application.Claims.Add(new TestApplicationClaim { Type = claim.Type, Value = claim.Value });
            }
            return Task.CompletedTask;
        }

        public Task<IdentityServiceResult> AddScopeAsync(TApplication application, string scope, CancellationToken cancellationToken)
        {
            application.Scopes.Add(new TestApplicationScope { Value = scope });
            return Task.FromResult(IdentityServiceResult.Success);
        }

        public Task<IdentityServiceResult> CreateAsync(TApplication application, CancellationToken cancellationToken)
        {
            _applications.Add(application.Id, application);
            return Task.FromResult(IdentityServiceResult.Success);
        }

        public Task<IdentityServiceResult> DeleteAsync(TApplication application, CancellationToken cancellationToken)
        {
            _applications.Remove(application.Id);
            return Task.FromResult(IdentityServiceResult.Success);
        }

        public void Dispose()
        {
        }

        public Task<TApplication> FindByClientIdAsync(string clientId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_applications.FirstOrDefault(a => a.Value.ClientId.Equals(clientId)).Value);
        }

        public Task<TApplication> FindByIdAsync(string applicationId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_applications.TryGetValue(applicationId, out var application) ? application : application);
        }

        public Task<TApplication> FindByNameAsync(string name, CancellationToken cancellationToken)
        {
            return Task.FromResult(_applications.FirstOrDefault(a => a.Value.Name.Equals(name)).Value);
        }

        public Task<IEnumerable<TApplication>> FindByUserIdAsync(string userId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> FindRegisteredLogoutUrisAsync(TApplication app, CancellationToken cancellationToken)
        {
            return Task.FromResult(app.RedirectUris.Where(ru => ru.IsLogout).Select(ru => ru.Value));
        }

        public Task<IEnumerable<string>> FindRegisteredUrisAsync(TApplication app, CancellationToken cancellationToken)
        {
            return Task.FromResult(app.RedirectUris.Where(ru => !ru.IsLogout).Select(ru => ru.Value));
        }

        public Task<IEnumerable<string>> FindScopesAsync(TApplication application, CancellationToken cancellationToken)
        {
            return Task.FromResult(application.Scopes.Select(s => s.Value));
        }

        public Task<string> GetApplicationClientIdAsync(TApplication application, CancellationToken cancellationToken)
        {
            return Task.FromResult(application.ClientId);
        }

        public Task<string> GetApplicationIdAsync(TApplication application, CancellationToken cancellationToken)
        {
            return Task.FromResult(application.Id);
        }

        public Task<string> GetApplicationNameAsync(TApplication application, CancellationToken cancellationToken)
        {
            return Task.FromResult(application.Name);
        }

        public Task<string> GetApplicationUserIdAsync(TApplication application, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IList<Claim>> GetClaimsAsync(TApplication application, CancellationToken cancellationToken)
        {
            return Task.FromResult<IList<Claim>>(application.Claims.Select(claim => new Claim(claim.Type, claim.Value)).ToList());
        }

        public Task<string> GetClientSecretHashAsync(TApplication application, CancellationToken cancellationToken)
        {
            return Task.FromResult(application.ClientSecretHash);
        }

        public Task<bool> HasClientSecretAsync(TApplication application, CancellationToken cancellationToken)
        {
            return Task.FromResult(application.ClientSecretHash != null);
        }

        public Task<IdentityServiceResult> RegisterLogoutRedirectUriAsync(TApplication app, string redirectUri, CancellationToken cancellationToken)
        {
            app.RedirectUris.Add(new TestApplicationRedirectUri { IsLogout = true, Value = redirectUri });
            return Task.FromResult(IdentityServiceResult.Success);
        }

        public Task<IdentityServiceResult> RegisterRedirectUriAsync(TApplication app, string redirectUri, CancellationToken cancellationToken)
        {
            app.RedirectUris.Add(new TestApplicationRedirectUri { IsLogout = false, Value = redirectUri });
            return Task.FromResult(IdentityServiceResult.Success);
        }

        public Task RemoveClaimsAsync(TApplication application, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            foreach (var claim in claims)
            {
                var foundClaim = application.Claims.FirstOrDefault(c => c.Type == claim.Type && c.Value == claim.Value);
                if (foundClaim != null)
                {
                    application.Claims.Remove(foundClaim);
                }
            }

            return Task.CompletedTask;
        }

        public Task<IdentityServiceResult> RemoveScopeAsync(TApplication application, string scope, CancellationToken cancellationToken)
        {
            var foundScope = application.Scopes.FirstOrDefault(s => s.Value == scope);
            if (foundScope != null)
            {
                application.Scopes.Remove(foundScope);
            }

            return Task.FromResult(IdentityServiceResult.Success);
        }

        public Task ReplaceClaimAsync(TApplication application, Claim claim, Claim newClaim, CancellationToken cancellationToken)
        {
            var matchedClaims = application.Claims.Where(uc => uc.Value == claim.Value && uc.Type == claim.Type).ToList();
            foreach (var matchedClaim in matchedClaims)
            {
                matchedClaim.Type = newClaim.Type;
                matchedClaim.Value = newClaim.Value;
            }

            return Task.CompletedTask;
        }

        public Task SetApplicationNameAsync(TApplication application, string name, CancellationToken cancellationToken)
        {
            application.Name = name;
            return Task.CompletedTask;
        }

        public Task SetClientSecretHashAsync(TApplication application, string clientSecretHash, CancellationToken cancellationToken)
        {
            application.ClientSecretHash = clientSecretHash;
            return Task.CompletedTask;
        }

        public Task<IdentityServiceResult> UnregisterLogoutRedirectUriAsync(TApplication app, string redirectUri, CancellationToken cancellationToken)
        {
            var logoutUri = app.RedirectUris.FirstOrDefault(ru => ru.IsLogout && ru.Value == redirectUri);
            if (logoutUri != null)
            {
                app.RedirectUris.Remove(logoutUri);
            }

            return Task.FromResult(IdentityServiceResult.Success);
        }

        public Task<IdentityServiceResult> UnregisterRedirectUriAsync(TApplication app, string redirectUri, CancellationToken cancellationToken)
        {
            var logoutUri = app.RedirectUris.FirstOrDefault(ru => !ru.IsLogout && ru.Value == redirectUri);
            if (logoutUri != null)
            {
                app.RedirectUris.Remove(logoutUri);
            }

            return Task.FromResult(IdentityServiceResult.Success);
        }

        public Task<IdentityServiceResult> UpdateAsync(TApplication application, CancellationToken cancellationToken)
        {
            _applications[application.Id] = application;
            return Task.FromResult(IdentityServiceResult.Success);
        }

        public Task<IdentityServiceResult> UpdateLogoutRedirectUriAsync(TApplication app, string oldRedirectUri, string newRedirectUri, CancellationToken cancellationToken)
        {
            var old = app.RedirectUris.Single(ru => ru.IsLogout && ru.Value == oldRedirectUri);
            old.Value = newRedirectUri;
            return Task.FromResult(IdentityServiceResult.Success);
        }

        public Task<IdentityServiceResult> UpdateRedirectUriAsync(TApplication app, string oldRedirectUri, string newRedirectUri, CancellationToken cancellationToken)
        {
            var old = app.RedirectUris.Single(ru => !ru.IsLogout && ru.Value == oldRedirectUri);
            old.Value = newRedirectUri;
            return Task.FromResult(IdentityServiceResult.Success);
        }

        public Task<IdentityServiceResult> UpdateScopeAsync(TApplication application, string oldScope, string newScope, CancellationToken cancellationToken)
        {
            var old = application.Scopes.Single(s => s.Value == oldScope);
            old.Value = newScope;
            return Task.FromResult(IdentityServiceResult.Success);
        }
    }
}
