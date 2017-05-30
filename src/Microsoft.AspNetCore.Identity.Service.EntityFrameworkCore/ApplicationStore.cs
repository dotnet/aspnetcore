// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.AspNetCore.Identity.Service.EntityFrameworkCore
{
    public class ApplicationStore<TApplication, TScope, TApplicationClaim, TRedirectUri, TContext, TKey, TUserKey>
        : IRedirectUriStore<TApplication>,
        IApplicationClaimStore<TApplication>,
        IApplicationClientSecretStore<TApplication>,
        IApplicationScopeStore<TApplication>,
        IQueryableApplicationStore<TApplication>
        where TApplication : IdentityServiceApplication<TKey, TUserKey, TScope, TApplicationClaim, TRedirectUri>
        where TScope : IdentityServiceScope<TKey>, new()
        where TApplicationClaim : IdentityServiceApplicationClaim<TKey>, new()
        where TRedirectUri : IdentityServiceRedirectUri<TKey>, new()
        where TContext : DbContext
        where TKey : IEquatable<TKey>
        where TUserKey : IEquatable<TUserKey>
    {
        private bool _disposed;

        public ApplicationStore(TContext context, ApplicationErrorDescriber errorDescriber)
        {
            Context = context;
            ErrorDescriber = errorDescriber;
        }

        public TContext Context { get; }

        public ApplicationErrorDescriber ErrorDescriber { get; }

        public DbSet<TApplication> ApplicationsSet => Context.Set<TApplication>();

        public DbSet<TScope> Scopes => Context.Set<TScope>();

        public DbSet<TApplicationClaim> ApplicationClaims => Context.Set<TApplicationClaim>();

        public DbSet<TRedirectUri> RedirectUris => Context.Set<TRedirectUri>();

        public virtual IQueryable<TApplication> Applications => ApplicationsSet;

        public bool AutoSaveChanges { get; set; } = true;

        protected Task SaveChanges(CancellationToken cancellationToken)
        {
            return AutoSaveChanges ? Context.SaveChangesAsync(cancellationToken) : Task.CompletedTask;
        }

        public async Task<IdentityServiceResult> CreateAsync(TApplication application, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }
            Context.Add(application);
            await SaveChanges(cancellationToken);
            return IdentityServiceResult.Success;
        }

        public async Task<IdentityServiceResult> UpdateAsync(TApplication application, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            Context.Attach(application);
            application.ConcurrencyStamp = Guid.NewGuid().ToString();
            Context.Update(application);
            try
            {
                await SaveChanges(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return IdentityServiceResult.Failed(ErrorDescriber.ConcurrencyFailure());
            }
            return IdentityServiceResult.Success;
        }

        public async Task<IdentityServiceResult> DeleteAsync(TApplication application, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            Context.Remove(application);
            try
            {
                await SaveChanges(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return IdentityServiceResult.Failed(ErrorDescriber.ConcurrencyFailure());
            }
            return IdentityServiceResult.Success;
        }

        public void Dispose()
        {
            _disposed = true;
        }

        public Task<TApplication> FindByClientIdAsync(string clientId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            var oldQueryBehavior = Context.ChangeTracker.QueryTrackingBehavior;

            try
            {
                Context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                return Applications.SingleOrDefaultAsync(a => a.ClientId == clientId, cancellationToken);
            }
            finally
            {
                Context.ChangeTracker.QueryTrackingBehavior = oldQueryBehavior;
            }
        }

        public Task<TApplication> FindByNameAsync(string name, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            var oldQueryBehavior = Context.ChangeTracker.QueryTrackingBehavior;

            try
            {
                Context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                return Applications.SingleOrDefaultAsync(a => a.Name == name, cancellationToken);
            }
            finally
            {
                Context.ChangeTracker.QueryTrackingBehavior = oldQueryBehavior;
            }
        }

        public async Task<IEnumerable<TApplication>> FindByUserIdAsync(string userId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            var id = ConvertUserIdFromString(userId);
            return await Applications.Where(a => a.UserId.Equals(id)).ToListAsync();
        }

        public Task<TApplication> FindByIdAsync(string applicationId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            var id = ConvertUserIdFromString(applicationId);
            return ApplicationsSet.FindAsync(new object[] { id }, cancellationToken);
        }

        public Task<string> GetApplicationIdAsync(TApplication application, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            var id = ConvertApplicationIdToString(application.Id);
            return Task.FromResult(id);
        }

        public Task<string> GetApplicationUserIdAsync(TApplication application, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            var id = ConvertUserIdToString(application.UserId);
            return Task.FromResult(id);
        }

        public Task<string> GetApplicationClientIdAsync(TApplication application, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return Task.FromResult(application.ClientId);
        }

        public Task<string> GetApplicationNameAsync(TApplication application, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return Task.FromResult(application.Name);
        }

        public async Task<IEnumerable<string>> FindRegisteredUrisAsync(
            TApplication app,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var redirectUris = await RedirectUris
                .Where(ru => ru.ApplicationId.Equals(app.Id) && !ru.IsLogout)
                .Select(ru => ru.Value)
                .ToListAsync(cancellationToken);

            return redirectUris;
        }

        public Task<IdentityServiceResult> RegisterRedirectUriAsync(TApplication app, string redirectUri, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (redirectUri == null)
            {
                throw new ArgumentNullException(nameof(redirectUri));
            }

            RedirectUris.Add(CreateRedirectUri(app, redirectUri, isLogout: false));

            return Task.FromResult(IdentityServiceResult.Success);
        }

        private TRedirectUri CreateRedirectUri(TApplication app, string redirectUri, bool isLogout)
        {
            var registration = new TRedirectUri
            {
                ApplicationId = app.Id,
                IsLogout = isLogout,
                Value = redirectUri
            };

            return registration;
        }

        public async Task<IdentityServiceResult> UnregisterRedirectUriAsync(TApplication app, string redirectUri, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (redirectUri == null)
            {
                throw new ArgumentNullException(nameof(redirectUri));
            }

            var registeredUri = await RedirectUris
                .SingleAsync(ru => ru.ApplicationId.Equals(app.Id) && ru.Value.Equals(redirectUri) && !ru.IsLogout);

            RedirectUris.Remove(registeredUri);

            return IdentityServiceResult.Success;
        }

        public async Task<IdentityServiceResult> UpdateRedirectUriAsync(
            TApplication app,
            string oldRedirectUri,
            string newRedirectUri,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (oldRedirectUri == null)
            {
                throw new ArgumentNullException(nameof(oldRedirectUri));
            }

            if (newRedirectUri == null)
            {
                throw new ArgumentNullException(nameof(newRedirectUri));
            }

            var existingRedirectUri = await RedirectUris
                .SingleAsync(ru => ru.ApplicationId.Equals(app.Id) && ru.Value.Equals(oldRedirectUri) && !ru.IsLogout);

            existingRedirectUri.Value = newRedirectUri;

            return IdentityServiceResult.Success;
        }

        public async Task<IEnumerable<string>> FindRegisteredLogoutUrisAsync(TApplication app, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var redirectUris = await RedirectUris
                .Where(ru => ru.ApplicationId.Equals(app.Id) && ru.IsLogout)
                .Select(ru => ru.Value)
                .ToListAsync(cancellationToken);

            return redirectUris;
        }

        public Task<IdentityServiceResult> RegisterLogoutRedirectUriAsync(TApplication app, string redirectUri, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (redirectUri == null)
            {
                throw new ArgumentNullException(nameof(redirectUri));
            }

            RedirectUris.Add(CreateRedirectUri(app, redirectUri, isLogout: true));

            return Task.FromResult(IdentityServiceResult.Success);
        }

        public async Task<IdentityServiceResult> UnregisterLogoutRedirectUriAsync(TApplication app, string redirectUri, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (redirectUri == null)
            {
                throw new ArgumentNullException(nameof(redirectUri));
            }

            var registeredUri = await RedirectUris
                .SingleAsync(ru => ru.ApplicationId.Equals(app.Id) && ru.Value.Equals(redirectUri) && ru.IsLogout);

            RedirectUris.Remove(registeredUri);

            return IdentityServiceResult.Success;
        }

        public async Task<IdentityServiceResult> UpdateLogoutRedirectUriAsync(TApplication app, string oldRedirectUri, string newRedirectUri, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (oldRedirectUri == null)
            {
                throw new ArgumentNullException(nameof(oldRedirectUri));
            }

            if (newRedirectUri == null)
            {
                throw new ArgumentNullException(nameof(newRedirectUri));
            }

            var existingRedirectUri = await RedirectUris
                .SingleAsync(ru => ru.ApplicationId.Equals(app.Id) && ru.Value.Equals(oldRedirectUri) && ru.IsLogout);

            existingRedirectUri.Value = newRedirectUri;

            return IdentityServiceResult.Success;
        }

        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        public virtual string ConvertApplicationIdToString(TKey id)
        {
            if (Equals(id, default(TKey)))
            {
                return null;
            }
            return id.ToString();
        }

        public virtual string ConvertUserIdToString(TUserKey id)
        {
            if (Equals(id, default(TUserKey)))
            {
                return null;
            }
            return id.ToString();
        }

        public virtual TKey ConvertApplicationIdFromString(string id)
        {
            if (id == null)
            {
                return default(TKey);
            }
            return (TKey)TypeDescriptor.GetConverter(typeof(TKey)).ConvertFromInvariantString(id);
        }

        public virtual TUserKey ConvertUserIdFromString(string id)
        {
            if (id == null)
            {
                return default(TUserKey);
            }
            return (TUserKey)TypeDescriptor.GetConverter(typeof(TKey)).ConvertFromInvariantString(id);
        }

        public Task SetApplicationNameAsync(TApplication application, string name, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            application.Name = name;
            return Task.CompletedTask;
        }

        public Task SetClientSecretHashAsync(TApplication application, string clientSecretHash, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            application.ClientSecretHash = clientSecretHash;
            return Task.CompletedTask;
        }

        public Task<string> GetClientSecretHashAsync(TApplication application, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            return Task.FromResult(application.ClientSecretHash);
        }

        public Task<bool> HasClientSecretAsync(TApplication application, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            return Task.FromResult(application.ClientSecretHash != null);
        }

        public async Task<IEnumerable<string>> FindScopesAsync(TApplication application, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            var scopes = await Scopes
                .Where(s => s.ApplicationId.Equals(application.Id))
                .Select(s => s.Value)
                .ToListAsync(cancellationToken);

            return scopes;
        }

        public Task<IdentityServiceResult> AddScopeAsync(TApplication application, string scope, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }

            Scopes.Add(CreateScope(application, scope));

            return Task.FromResult(IdentityServiceResult.Success);
        }

        private TScope CreateScope(TApplication application, string scope)
        {
            return new TScope
            {
                ApplicationId = application.Id,
                Value = scope
            };
        }

        public async Task<IdentityServiceResult> UpdateScopeAsync(TApplication application, string oldScope, string newScope, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            if (oldScope == null)
            {
                throw new ArgumentNullException(nameof(oldScope));
            }

            if (newScope == null)
            {
                throw new ArgumentNullException(nameof(newScope));
            }

            var existingScope = await Scopes
                .SingleAsync(s => s.ApplicationId.Equals(application.Id) && s.Value.Equals(oldScope));
            existingScope.Value = newScope;

            return IdentityServiceResult.Success;
        }

        public async Task<IdentityServiceResult> RemoveScopeAsync(TApplication application, string scope, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }

            var existingScope = await Scopes
                .SingleAsync(ru => ru.ApplicationId.Equals(application.Id) && ru.Value.Equals(scope));
            Scopes.Remove(existingScope);

            return IdentityServiceResult.Success;
        }

        public async Task<IList<Claim>> GetClaimsAsync(TApplication application, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            return await ApplicationClaims.Where(ac => ac.ApplicationId.Equals(application.Id)).Select(c => c.ToClaim()).ToListAsync(cancellationToken);
        }

        public Task AddClaimsAsync(TApplication application, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }
            if (claims == null)
            {
                throw new ArgumentNullException(nameof(claims));
            }
            foreach (var claim in claims)
            {
                ApplicationClaims.Add(CreateApplicationClaim(application, claim));
            }
            return Task.CompletedTask;
        }

        private TApplicationClaim CreateApplicationClaim(TApplication application, Claim claim) =>
            new TApplicationClaim
            {
                ApplicationId = application.Id,
                ClaimType = claim.Type,
                ClaimValue = claim.Value
            };

        public async Task ReplaceClaimAsync(TApplication application, Claim oldClaim, Claim newClaim, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }
            if (oldClaim == null)
            {
                throw new ArgumentNullException(nameof(oldClaim));
            }
            if (newClaim == null)
            {
                throw new ArgumentNullException(nameof(newClaim));
            }

            var matchedClaims = await ApplicationClaims.Where(ac => ac.ApplicationId.Equals(application.Id) && ac.ClaimValue == oldClaim.Value && ac.ClaimType == oldClaim.Type).ToListAsync(cancellationToken);
            foreach (var matchedClaim in matchedClaims)
            {
                matchedClaim.ClaimValue = newClaim.Value;
                matchedClaim.ClaimType = newClaim.Type;
            }
        }

        public async Task RemoveClaimsAsync(TApplication application, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }
            if (claims == null)
            {
                throw new ArgumentNullException(nameof(claims));
            }
            foreach (var claim in claims)
            {
                var matchedClaims = await ApplicationClaims.Where(ac => ac.ApplicationId.Equals(application.Id) && ac.ClaimValue == claim.Value && ac.ClaimType == claim.Type).ToListAsync(cancellationToken);
                foreach (var c in matchedClaims)
                {
                    ApplicationClaims.Remove(c);
                }
            }
        }
    }
}
