// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class ApplicationManager<TApplication> : IDisposable where TApplication : class
    {
        private bool _disposed;

        public ApplicationManager(
            IApplicationStore<TApplication> store,
            IPasswordHasher<TApplication> passwordHasher,
            IEnumerable<IApplicationValidator<TApplication>> applicationValidators,
            ILogger<ApplicationManager<TApplication>> logger)
        {
            Store = store;
            PasswordHasher = passwordHasher;
            ApplicationValidators = applicationValidators;
            Logger = Logger;
        }

        public IApplicationStore<TApplication> Store { get; set; }
        public IPasswordHasher<TApplication> PasswordHasher { get; set; }
        public IEnumerable<IApplicationValidator<TApplication>> ApplicationValidators { get; set; }
        public ILogger<ApplicationManager<TApplication>> Logger { get; set; }
        public CancellationToken CancellationToken { get; set; }

        public virtual bool SupportsQueryableApplications
        {
            get
            {
                ThrowIfDisposed();
                return Store is IQueryableUserStore<TApplication>;
            }
        }

        public virtual IQueryable<TApplication> Applications
        {
            get
            {
                var queryableStore = Store as IQueryableApplicationStore<TApplication>;
                if (queryableStore == null)
                {
                    throw new NotSupportedException("Store not IQueryableApplicationStore");
                }
                return queryableStore.Applications;
            }
        }

        public Task<TApplication> FindByIdAsync(string applicationId)
        {
            return Store.FindByIdAsync(applicationId, CancellationToken);
        }

        public Task<TApplication> FindByClientIdAsync(string clientId)
        {
            return Store.FindByClientIdAsync(clientId, CancellationToken.None);
        }

        public Task<TApplication> FindByNameAsync(string name)
        {
            return Store.FindByNameAsync(name, CancellationToken.None);
        }

        public virtual async Task<IdentityServiceResult> CreateAsync(TApplication application)
        {
            ThrowIfDisposed();
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            var result = await ValidateApplicationAsync(application);
            if (!result.Succeeded)
            {
                return result;
            }

            return await Store.CreateAsync(application, CancellationToken);
        }

        public virtual async Task<IdentityServiceResult> DeleteAsync(TApplication application)
        {
            ThrowIfDisposed();
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            return await Store.DeleteAsync(application, CancellationToken);
        }

        public virtual async Task<IdentityServiceResult> UpdateAsync(TApplication application)
        {
            ThrowIfDisposed();
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            var result = await ValidateApplicationAsync(application);
            if (!result.Succeeded)
            {
                return result;
            }

            return await Store.UpdateAsync(application, CancellationToken);
        }

        private async Task<IdentityServiceResult> ValidateApplicationAsync(TApplication application)
        {
            var errors = new List<IdentityServiceError>();
            foreach (var v in ApplicationValidators)
            {
                var result = await v.ValidateAsync(this, application);
                if (!result.Succeeded)
                {
                    errors.AddRange(result.Errors);
                }
            }

            if (errors.Count > 0)
            {
                return IdentityServiceResult.Failed(errors.ToArray());
            }

            return IdentityServiceResult.Success;
        }

        public Task<string> GetApplicationIdAsync(TApplication application)
        {
            ThrowIfDisposed();
            return Store.GetApplicationIdAsync(application, CancellationToken);
        }

        public Task<string> GetApplicationClientIdAsync(TApplication application)
        {
            ThrowIfDisposed();
            return Store.GetApplicationClientIdAsync(application, CancellationToken);
        }

        public void Dispose()
        {
            _disposed = true;
        }

        public Task<string> GenerateClientSecretAsync()
        {
            return Task.FromResult(Guid.NewGuid().ToString());
        }

        public async Task<IdentityServiceResult> AddClientSecretAsync(TApplication application, string clientSecret)
        {
            ThrowIfDisposed();
            var store = GetClientSecretStore();
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            var hash = await store.GetClientSecretHashAsync(application, CancellationToken);
            if (hash != null)
            {
                Logger.LogWarning(1, "User {clientId} already has a password.", await GetApplicationClientIdAsync(application));
                return IdentityServiceResult.Failed();
            }
            var result = await UpdateClientSecretHashAsync(store, application, clientSecret);
            if (!result.Succeeded)
            {
                return result;
            }

            return await UpdateAsync(application);
        }

        public async Task<IdentityServiceResult> ChangeClientSecretAsync(TApplication application, string newClientSecret)
        {
            ThrowIfDisposed();
            var store = GetClientSecretStore();
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            var result = await UpdateClientSecretHashAsync(store, application, newClientSecret);
            if (!result.Succeeded)
            {
                return result;
            }

            return await UpdateAsync(application);
        }

        public async Task<IdentityServiceResult> RemoveClientSecretAsync(TApplication application)
        {
            ThrowIfDisposed();
            var store = GetClientSecretStore();
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            var result = await UpdateClientSecretHashAsync(store, application, clientSecret: null);
            if (!result.Succeeded)
            {
                return result;
            }

            return await UpdateAsync(application);
        }

        private IRedirectUriStore<TApplication> GetRedirectUriStore()
        {
            if (Store is IRedirectUriStore<TApplication> cast)
            {
                return cast;
            }

            throw new NotSupportedException();
        }

        public async Task<IdentityServiceResult> RegisterRedirectUriAsync(TApplication application, string redirectUri)
        {
            var redirectStore = GetRedirectUriStore();
            var result = await redirectStore.RegisterRedirectUriAsync(application, redirectUri, CancellationToken);
            if (!result.Succeeded)
            {
                return result;
            }

            return await redirectStore.UpdateAsync(application, CancellationToken);
        }

        public Task<IEnumerable<string>> FindRegisteredUrisAsync(TApplication application)
        {
            var redirectStore = GetRedirectUriStore();
            return redirectStore.FindRegisteredUrisAsync(application, CancellationToken);
        }

        public Task<IEnumerable<string>> FindRegisteredLogoutUrisAsync(TApplication application)
        {
            var redirectStore = GetRedirectUriStore();
            return redirectStore.FindRegisteredLogoutUrisAsync(application, CancellationToken);
        }

        public async Task<IdentityServiceResult> UnregisterRedirectUriAsync(TApplication application, string redirectUri)
        {
            var redirectStore = GetRedirectUriStore();
            var result = await redirectStore.UnregisterRedirectUriAsync(application, redirectUri, CancellationToken);
            if (!result.Succeeded)
            {
                return result;
            }

            return await redirectStore.UpdateAsync(application, CancellationToken);
        }

        public async Task<IdentityServiceResult> UpdateRedirectUriAsync(TApplication application, string oldRedirectUri, string newRedirectUri)
        {
            var redirectStore = GetRedirectUriStore();
            var result = await redirectStore.UpdateRedirectUriAsync(application, oldRedirectUri, newRedirectUri, CancellationToken);
            if (!result.Succeeded)
            {
                return result;
            }

            return await redirectStore.UpdateAsync(application, CancellationToken);
        }

        public async Task<bool> ValidateClientCredentialsAsync(string clientId, string clientSecret)
        {
            var application = await FindByClientIdAsync(clientId);
            if (application == null)
            {
                return false;
            }

            var clientSecretStore = GetClientSecretStore();
            if (!await clientSecretStore.HasClientSecretAsync(application, CancellationToken))
            {
                // Should we fail if clientSecret != null?
                return true;
            }

            if (clientSecret == null)
            {
                return false;
            }

            var result = await VerifyClientSecretAsync(clientSecretStore, application, clientSecret);
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                await UpdateClientSecretHashAsync(clientSecretStore, application, clientSecret);
                await UpdateAsync(application);
                return true;
            }

            return result == PasswordVerificationResult.Success;
        }

        private async Task<IdentityServiceResult> UpdateClientSecretHashAsync(
            IApplicationClientSecretStore<TApplication> clientSecretStore,
            TApplication application,
            string clientSecret)
        {
            var hash = PasswordHasher.HashPassword(application, clientSecret);
            await clientSecretStore.SetClientSecretHashAsync(application, hash, CancellationToken);
            return IdentityServiceResult.Success;
        }

        protected virtual async Task<PasswordVerificationResult> VerifyClientSecretAsync(
            IApplicationClientSecretStore<TApplication> store,
            TApplication application,
            string clientSecret)
        {
            var hash = await store.GetClientSecretHashAsync(application, CancellationToken);
            if (hash == null)
            {
                return PasswordVerificationResult.Failed;
            }

            return PasswordHasher.VerifyHashedPassword(application, hash, clientSecret);
        }

        private IApplicationClientSecretStore<TApplication> GetClientSecretStore()
        {
            if (Store is IApplicationClientSecretStore<TApplication> cast)
            {
                return cast;
            }

            throw new NotSupportedException();
        }

        public Task<IEnumerable<string>> FindScopesAsync(TApplication application)
        {
            var scopeStore = GetScopeStore();
            return scopeStore.FindScopesAsync(application, CancellationToken);
        }

        public async Task<IdentityServiceResult> AddScopeAsync(TApplication application, string scope)
        {
            var scopeStore = GetScopeStore();
            var result = await scopeStore.AddScopeAsync(application, scope, CancellationToken);
            if (!result.Succeeded)
            {
                return result;
            }

            return await scopeStore.UpdateAsync(application, CancellationToken);
        }

        public async Task<IdentityServiceResult> RemoveScopeAsync(TApplication application, string scope)
        {
            var scopeStore = GetScopeStore();
            var result = await scopeStore.RemoveScopeAsync(application, scope, CancellationToken);
            if (!result.Succeeded)
            {
                return result;
            }

            return await scopeStore.UpdateAsync(application, CancellationToken);
        }

        public async Task<IdentityServiceResult> UpdateScopeAsync(TApplication application, string oldScope, string newScope)
        {
            var scopeStore = GetScopeStore();
            var result = await scopeStore.UpdateScopeAsync(application, oldScope, newScope, CancellationToken);
            if (!result.Succeeded)
            {
                return result;
            }

            return await scopeStore.UpdateAsync(application, CancellationToken);
        }

        private IApplicationScopeStore<TApplication> GetScopeStore()
        {
            if (Store is IApplicationScopeStore<TApplication> cast)
            {
                return cast;
            }

            throw new NotSupportedException();
        }

        public virtual Task<IdentityServiceResult> AddClaimAsync(TApplication application, Claim claim)
        {
            ThrowIfDisposed();
            var claimStore = GetApplicationClaimStore();
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }
            return AddClaimsAsync(application, new Claim[] { claim });
        }

        public virtual async Task<IdentityServiceResult> AddClaimsAsync(TApplication application, IEnumerable<Claim> claims)
        {
            ThrowIfDisposed();
            var claimStore = GetApplicationClaimStore();
            if (claims == null)
            {
                throw new ArgumentNullException(nameof(claims));
            }
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            await claimStore.AddClaimsAsync(application, claims, CancellationToken);
            return await UpdateAsync(application);
        }

        public virtual async Task<IdentityServiceResult> ReplaceClaimAsync(TApplication application, Claim claim, Claim newClaim)
        {
            ThrowIfDisposed();
            var claimStore = GetApplicationClaimStore();
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }
            if (newClaim == null)
            {
                throw new ArgumentNullException(nameof(newClaim));
            }
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            await claimStore.ReplaceClaimAsync(application, claim, newClaim, CancellationToken);
            return await UpdateAsync(application);
        }

        public virtual Task<IdentityServiceResult> RemoveClaimAsync(TApplication application, Claim claim)
        {
            ThrowIfDisposed();
            var claimStore = GetApplicationClaimStore();
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }
            return RemoveClaimsAsync(application, new Claim[] { claim });
        }

        public virtual async Task<IdentityServiceResult> RemoveClaimsAsync(TApplication application, IEnumerable<Claim> claims)
        {
            ThrowIfDisposed();
            var claimStore = GetApplicationClaimStore();
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }
            if (claims == null)
            {
                throw new ArgumentNullException(nameof(claims));
            }

            await claimStore.RemoveClaimsAsync(application, claims, CancellationToken);
            return await UpdateAsync(application);
        }

        public virtual async Task<IList<Claim>> GetClaimsAsync(TApplication application)
        {
            ThrowIfDisposed();
            var claimStore = GetApplicationClaimStore();
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }
            return await claimStore.GetClaimsAsync(application, CancellationToken);
        }

        private IApplicationClaimStore<TApplication> GetApplicationClaimStore()
        {
            if (Store is IApplicationClaimStore<TApplication> cast)
            {
                return cast;
            }

            throw new NotSupportedException();
        }

        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
