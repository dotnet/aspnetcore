// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Exposes role related api which will automatically save changes to the RoleStore
    /// </summary>
    /// <typeparam name="TRole"></typeparam>
    public class RoleManager<TRole> : IDisposable where TRole : class
    {
        private bool _disposed;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="store">The IRoleStore commits changes via the UpdateAsync/CreateAsync methods</param>
        /// <param name="roleValidator"></param>
        public RoleManager(IRoleStore<TRole> store,
            IEnumerable<IRoleValidator<TRole>> roleValidators = null,
            ILookupNormalizer keyNormalizer = null,
            IdentityErrorDescriber errors = null,
            ILoggerFactory loggerFactory = null)
        {
            if (store == null)
            {
                throw new ArgumentNullException("store");
            }
            Store = store;
            KeyNormalizer = keyNormalizer ?? new UpperInvariantLookupNormalizer();
            ErrorDescriber = errors ?? new IdentityErrorDescriber();

            if (roleValidators != null)
            {
                foreach (var v in roleValidators)
                {
                    RoleValidators.Add(v);
                }
            }

            loggerFactory = loggerFactory ?? new LoggerFactory();
            Logger = loggerFactory.Create(nameof(RoleManager<TRole>));
        }

        /// <summary>
        ///     Persistence abstraction that the Manager operates against
        /// </summary>
        protected IRoleStore<TRole> Store { get; private set; }

        /// <summary>
        ///     Used to validate roles before persisting changes
        /// </summary>
        public IList<IRoleValidator<TRole>> RoleValidators { get; } = new List<IRoleValidator<TRole>>();

        /// <summary>
        ///     Used to generate public API error messages
        /// </summary>
        public IdentityErrorDescriber ErrorDescriber { get; set; }

        /// <summary>
        ///     Used to log results
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        ///     Used to normalize user names, role names, emails for uniqueness
        /// </summary>
        public ILookupNormalizer KeyNormalizer { get; set; }

        /// <summary>
        ///     Returns an IQueryable of roles if the store is an IQueryableRoleStore
        /// </summary>
        public virtual IQueryable<TRole> Roles
        {
            get
            {
                var queryableStore = Store as IQueryableRoleStore<TRole>;
                if (queryableStore == null)
                {
                    throw new NotSupportedException(Resources.StoreNotIQueryableRoleStore);
                }
                return queryableStore.Roles;
            }
        }

        /// <summary>
        ///     Returns true if the store is an IQueryableRoleStore
        /// </summary>
        public virtual bool SupportsQueryableRoles
        {
            get
            {
                ThrowIfDisposed();
                return Store is IQueryableRoleStore<TRole>;
            }
        }

        /// <summary>
        ///     Returns true if the store is an IUserClaimStore
        /// </summary>
        public virtual bool SupportsRoleClaims
        {
            get
            {
                ThrowIfDisposed();
                return Store is IRoleClaimStore<TRole>;
            }
        }

        /// <summary>
        ///     Dispose this object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private async Task<IdentityResult> ValidateRoleInternal(TRole role, CancellationToken cancellationToken)
        {
            var errors = new List<IdentityError>();
            foreach (var v in RoleValidators)
            {
                var result = await v.ValidateAsync(this, role, cancellationToken);
                if (!result.Succeeded)
                {
                    errors.AddRange(result.Errors);
                }
            }
            return errors.Count > 0 ? IdentityResult.Failed(errors.ToArray()) : IdentityResult.Success;
        }

        /// <summary>
        ///     CreateAsync a role
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> CreateAsync(TRole role,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }

            var result = await ValidateRoleInternal(role, cancellationToken);
            if (!result.Succeeded)
            {
                return result;
            }
            await UpdateNormalizedRoleNameAsync(role, cancellationToken);
            return await LogResultAsync(await Store.CreateAsync(role, cancellationToken), role);
        }

        /// <summary>
        /// Update the user's normalized user name
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task UpdateNormalizedRoleNameAsync(TRole role,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var name = await GetRoleNameAsync(role, cancellationToken);
            await Store.SetNormalizedRoleNameAsync(role, NormalizeKey(name), cancellationToken);
        }


        /// <summary>
        ///     UpdateAsync an existing role
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> UpdateAsync(TRole role,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }

            return await LogResultAsync(await UpdateRoleAsync(role, cancellationToken), role);
        }

        private async Task<IdentityResult> UpdateRoleAsync(TRole role,
             CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = await ValidateRoleInternal(role, cancellationToken);
            if (!result.Succeeded)
            {
                return result;
            }
            await UpdateNormalizedRoleNameAsync(role, cancellationToken);
            return await Store.UpdateAsync(role, cancellationToken);
        }

        /// <summary>
        ///     DeleteAsync a role
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> DeleteAsync(TRole role,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }
            return await LogResultAsync(await Store.DeleteAsync(role, cancellationToken), role);
        }

        /// <summary>
        ///     Returns true if the role exists
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<bool> RoleExistsAsync(string roleName,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (roleName == null)
            {
                throw new ArgumentNullException("roleName");
            }

            return await FindByNameAsync(NormalizeKey(roleName), cancellationToken) != null;
        }

        /// <summary>
        /// Normalize a key (role name) for uniqueness comparisons
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public virtual string NormalizeKey(string key)
        {
            return (KeyNormalizer == null) ? key : KeyNormalizer.Normalize(key);
        }


        /// <summary>
        ///     FindByLoginAsync a role by id
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<TRole> FindByIdAsync(string roleId,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            return await Store.FindByIdAsync(roleId, cancellationToken);
        }

        /// <summary>
        /// Return the name of the role
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<string> GetRoleNameAsync(TRole role,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            return await Store.GetRoleNameAsync(role, cancellationToken);
        }

        /// <summary>
        /// Set the name of the role
        /// </summary>
        /// <param name="role"></param>
        /// <param name="name"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> SetRoleNameAsync(TRole role, string name,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            await Store.SetRoleNameAsync(role, name, cancellationToken);
            await UpdateNormalizedRoleNameAsync(role, cancellationToken);
            return await LogResultAsync(IdentityResult.Success, role);
        }

        /// <summary>
        /// Return the role id for a role
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<string> GetRoleIdAsync(TRole role,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            return await Store.GetRoleIdAsync(role, cancellationToken);
        }

        /// <summary>
        ///     FindByLoginAsync a role by name
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<TRole> FindByNameAsync(string roleName,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (roleName == null)
            {
                throw new ArgumentNullException("roleName");
            }

            return await Store.FindByNameAsync(NormalizeKey(roleName), cancellationToken);
        }

        // IRoleClaimStore methods
        private IRoleClaimStore<TRole> GetClaimStore()
        {
            var cast = Store as IRoleClaimStore<TRole>;
            if (cast == null)
            {
                throw new NotSupportedException(Resources.StoreNotIRoleClaimStore);
            }
            return cast;
        }

        /// <summary>
        ///     Add a user claim
        /// </summary>
        /// <param name="role"></param>
        /// <param name="claim"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> AddClaimAsync(TRole role, Claim claim,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var claimStore = GetClaimStore();
            if (claim == null)
            {
                throw new ArgumentNullException("claim");
            }
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }
            await claimStore.AddClaimAsync(role, claim, cancellationToken);
            return await LogResultAsync(await UpdateRoleAsync(role, cancellationToken), role);
        }

        /// <summary>
        ///     Remove a user claim
        /// </summary>
        /// <param name="role"></param>
        /// <param name="claim"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> RemoveClaimAsync(TRole role, Claim claim,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var claimStore = GetClaimStore();
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }
            await claimStore.RemoveClaimAsync(role, claim, cancellationToken);
            return await LogResultAsync(await UpdateRoleAsync(role, cancellationToken), role);
        }

        /// <summary>
        ///     Get a role's claims
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual async Task<IList<Claim>> GetClaimsAsync(TRole role,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            var claimStore = GetClaimStore();
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }
            return await claimStore.GetClaimsAsync(role, cancellationToken);
        }

        /// <summary>
        ///     Logs the current Identity Result and returns result object
        /// </summary>
        /// <param name="result"></param>
        /// <param name="user"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        protected async Task<IdentityResult> LogResultAsync(IdentityResult result,
            TRole role, [System.Runtime.CompilerServices.CallerMemberName] string methodName = "")
        {
            result.Log(Logger, Resources.FormatLoggingResultMessageForRole(methodName, await GetRoleIdAsync(role)));

            return result;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        /// <summary>
        ///     When disposing, actually dipose the store
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                Store.Dispose();
            }
            _disposed = true;
        }
    }
}