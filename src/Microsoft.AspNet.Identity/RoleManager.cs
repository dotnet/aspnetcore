// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Identity.Logging;
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
        private readonly HttpContext _context;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="store">The IRoleStore commits changes via the UpdateAsync/CreateAsync methods</param>
        /// <param name="roleValidators">IEnumerable of role validators</param>
        /// <param name="keyNormalizer">user property normalizers</param>
        /// <param name="errors">IdentityErrorDescribers</param>
        /// <param name="logger">Logger for RoleManager</param>
        /// <param name="contextAccessor">HttpContext accessor object</param>
        public RoleManager(IRoleStore<TRole> store,
            IEnumerable<IRoleValidator<TRole>> roleValidators,
            ILookupNormalizer keyNormalizer,
            IdentityErrorDescriber errors,
            ILoggerFactory logger,
            IHttpContextAccessor contextAccessor)
        {
            if (store == null)
            {
                throw new ArgumentNullException("store");
            }
            Store = store;
            KeyNormalizer = keyNormalizer ?? new UpperInvariantLookupNormalizer();
            ErrorDescriber = errors ?? new IdentityErrorDescriber();
            _context = contextAccessor?.HttpContext;

            if (roleValidators != null)
            {
                foreach (var v in roleValidators)
                {
                    RoleValidators.Add(v);
                }
            }

            Logger = logger?.CreateLogger<RoleManager<TRole>>() ?? new Logger<RoleManager<TRole>>(new LoggerFactory());
        }

        /// <summary>
        ///     Persistence abstraction that the Manager operates against
        /// </summary>
        protected IRoleStore<TRole> Store { get; private set; }

        /// <summary>
        ///     Used for logging results
        /// </summary>
        protected internal virtual ILogger Logger { get; set; }

        /// <summary>
        ///     Used to validate roles before persisting changes
        /// </summary>
        internal IList<IRoleValidator<TRole>> RoleValidators { get; } = new List<IRoleValidator<TRole>>();

        /// <summary>
        ///     Used to generate public API error messages
        /// </summary>
        internal IdentityErrorDescriber ErrorDescriber { get; set; }

        /// <summary>
        ///     Used to normalize user names, role names, emails for uniqueness
        /// </summary>
        internal ILookupNormalizer KeyNormalizer { get; set; }

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

        private CancellationToken CancellationToken => _context?.RequestAborted ?? CancellationToken.None;

        /// <summary>
        ///     Dispose this object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private async Task<IdentityResult> ValidateRoleInternal(TRole role)
        {
            var errors = new List<IdentityError>();
            foreach (var v in RoleValidators)
            {
                var result = await v.ValidateAsync(this, role);
                if (!result.Succeeded)
                {
                    errors.AddRange(result.Errors);
                }
            }
            return errors.Count > 0 ? IdentityResult.Failed(errors.ToArray()) : IdentityResult.Success;
        }

        /// <summary>
        ///     Create a role
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> CreateAsync(TRole role)
        {
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }
            var result = await ValidateRoleInternal(role);
            if (!result.Succeeded)
            {
                return result;
            }
            await UpdateNormalizedRoleNameAsync(role);
            result = await Store.CreateAsync(role, CancellationToken);
            using (await BeginLoggingScopeAsync(role))
            {
                return Logger.Log(result);
            }
        }

        /// <summary>
        /// Update the user's normalized user name
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public virtual async Task UpdateNormalizedRoleNameAsync(TRole role)
        {
            var name = await GetRoleNameAsync(role);
            await Store.SetNormalizedRoleNameAsync(role, NormalizeKey(name), CancellationToken);
        }


        /// <summary>
        ///     Update an existing role
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> UpdateAsync(TRole role)
        {
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }

            using (await BeginLoggingScopeAsync(role))
            {
                return Logger.Log(await UpdateRoleAsync(role));
            }
        }

        private async Task<IdentityResult> UpdateRoleAsync(TRole role)
        {
            var result = await ValidateRoleInternal(role);
            if (!result.Succeeded)
            {
                return result;
            }
            await UpdateNormalizedRoleNameAsync(role);
            return await Store.UpdateAsync(role, CancellationToken);
        }

        /// <summary>
        ///     Delete a role
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> DeleteAsync(TRole role)
        {
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }

            using (await BeginLoggingScopeAsync(role))
            {
                return Logger.Log(await Store.DeleteAsync(role, CancellationToken));
            }
        }

        /// <summary>
        ///     Returns true if the role exists
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public virtual async Task<bool> RoleExistsAsync(string roleName)
        {
            ThrowIfDisposed();
            if (roleName == null)
            {
                throw new ArgumentNullException("roleName");
            }

            return await FindByNameAsync(NormalizeKey(roleName)) != null;
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
        ///     Find a role by id
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public virtual async Task<TRole> FindByIdAsync(string roleId)
        {
            ThrowIfDisposed();
            return await Store.FindByIdAsync(roleId, CancellationToken);
        }

        /// <summary>
        /// Return the name of the role
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public virtual async Task<string> GetRoleNameAsync(TRole role)
        {
            ThrowIfDisposed();
            return await Store.GetRoleNameAsync(role, CancellationToken);
        }

        /// <summary>
        /// Set the name of the role
        /// </summary>
        /// <param name="role"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> SetRoleNameAsync(TRole role, string name)
        {
            ThrowIfDisposed();

            using (await BeginLoggingScopeAsync(role))
            {
                await Store.SetRoleNameAsync(role, name, CancellationToken);
                await UpdateNormalizedRoleNameAsync(role);
                return Logger.Log(IdentityResult.Success);
            }
        }

        /// <summary>
        /// Return the role id for a role
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public virtual async Task<string> GetRoleIdAsync(TRole role)
        {
            ThrowIfDisposed();
            return await Store.GetRoleIdAsync(role, CancellationToken);
        }

        /// <summary>
        ///     FindByLoginAsync a role by name
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public virtual async Task<TRole> FindByNameAsync(string roleName)
        {
            ThrowIfDisposed();
            if (roleName == null)
            {
                throw new ArgumentNullException("roleName");
            }

            return await Store.FindByNameAsync(NormalizeKey(roleName), CancellationToken);
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
        /// <returns></returns>
        public virtual async Task<IdentityResult> AddClaimAsync(TRole role, Claim claim)
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

            using (await BeginLoggingScopeAsync(role))
            {
                await claimStore.AddClaimAsync(role, claim, CancellationToken);
                return Logger.Log(await UpdateRoleAsync(role));
            }
        }

        /// <summary>
        ///     Remove a user claim
        /// </summary>
        /// <param name="role"></param>
        /// <param name="claim"></param>
        /// <returns></returns>
        public virtual async Task<IdentityResult> RemoveClaimAsync(TRole role, Claim claim)
        {
            ThrowIfDisposed();
            var claimStore = GetClaimStore();
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }

            using (await BeginLoggingScopeAsync(role))
            {
                await claimStore.RemoveClaimAsync(role, claim, CancellationToken);
                return Logger.Log(await UpdateRoleAsync(role));
            }
        }

        /// <summary>
        ///     Get a role's claims
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        public virtual async Task<IList<Claim>> GetClaimsAsync(TRole role)
        {
            ThrowIfDisposed();
            var claimStore = GetClaimStore();
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }
            return await claimStore.GetClaimsAsync(role, CancellationToken);
        }

        protected virtual async Task<IDisposable> BeginLoggingScopeAsync(TRole role, [CallerMemberName] string methodName = null)
        {
            var state = Resources.FormatLoggingResultMessageForRole(methodName, await GetRoleIdAsync(role));
            return Logger.BeginScope(state);
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