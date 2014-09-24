// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Entity;

namespace Microsoft.AspNet.Identity.SqlServer
{
    public class RoleStore<TRole>(DbContext context) : RoleStore<TRole, DbContext, string>(context) 
        where TRole : IdentityRole
    { }

    public class RoleStore<TRole, TContext>(TContext context) : RoleStore<TRole, TContext, string>(context)
        where TRole : IdentityRole
        where TContext : DbContext
    { }

    public class RoleStore<TRole, TContext, TKey>(TContext context) : 
        IQueryableRoleStore<TRole>, 
        IRoleClaimStore<TRole>
        where TRole : IdentityRole<TKey>
        where TKey : IEquatable<TKey>
        where TContext : DbContext
    {
        // Primary constructor
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
        }

        private bool _disposed;


        public TContext Context { get; } = context;

        /// <summary>
        ///     If true will call SaveChanges after CreateAsync/UpdateAsync/DeleteAsync
        /// </summary>
        public bool AutoSaveChanges { get; set; } = true;

        private async Task SaveChanges(CancellationToken cancellationToken)
        {
            if (AutoSaveChanges)
            {
                await Context.SaveChangesAsync(cancellationToken);
            }
        }

        public virtual Task<TRole> GetRoleAggregate(Expression<Func<TRole, bool>> filter, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(Roles.FirstOrDefault(filter));
        }

        public async virtual Task CreateAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }
            await Context.AddAsync(role, cancellationToken);
            await SaveChanges(cancellationToken);
        }

        public async virtual Task UpdateAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }
            await Context.UpdateAsync(role, cancellationToken);
            await SaveChanges(cancellationToken);
        }

        public async virtual Task DeleteAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }
            Context.Delete(role);
            await SaveChanges(cancellationToken);
        }

        public Task<string> GetRoleIdAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }
            return Task.FromResult(ConvertIdToString(role.Id));
        }

        public Task<string> GetRoleNameAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }
            return Task.FromResult(role.Name);
        }

        public Task SetRoleNameAsync(TRole role, string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }
            role.Name = roleName;
            return Task.FromResult(0);
        }

        public virtual TKey ConvertIdFromString(string id)
        {
            if (id == null)
            {
                return default(TKey);
            }
            return (TKey)Convert.ChangeType(id, typeof(TKey));
        }

        public virtual string ConvertIdToString(TKey id)
        {
            if (id.Equals(default(TKey)))
            {
                return null;
            }
            return id.ToString();
        }

        /// <summary>
        ///     Find a role by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task<TRole> FindByIdAsync(string id, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            var roleId = ConvertIdFromString(id);
            return GetRoleAggregate(u => u.Id.Equals(roleId), cancellationToken);
        }

        /// <summary>
        ///     Find a role by name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public virtual Task<TRole> FindByNameAsync(string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfDisposed();
            return GetRoleAggregate(u => u.Name.ToUpper() == name.ToUpper(), cancellationToken);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        /// <summary>
        ///     Dispose the store
        /// </summary>
        public void Dispose()
        {
            _disposed = true;
        }

        public Task<IList<Claim>> GetClaimsAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }
            var result = RoleClaims.Where(rc => rc.RoleId.Equals(role.Id)).Select(c => new Claim(c.ClaimType, c.ClaimValue)).ToList();
            return Task.FromResult((IList<Claim>)result);
        }

        public Task AddClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }
            if (claim == null)
            {
                throw new ArgumentNullException("claim");
            }
            RoleClaims.Add(new IdentityRoleClaim<TKey> { RoleId = role.Id, ClaimType = claim.Type, ClaimValue = claim.Value });
            return Task.FromResult(0);
        }

        public Task RemoveClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default(CancellationToken))
        {
            ThrowIfDisposed();
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }
            if (claim == null)
            {
                throw new ArgumentNullException("claim");
            }
            var claims = RoleClaims.Where(uc => uc.ClaimValue == claim.Value && uc.ClaimType == claim.Type).ToList();
            foreach (var c in claims)
            {
                RoleClaims.Remove(c);
            }
            return Task.FromResult(0);
        }

        public IQueryable<TRole> Roles
        {
            get { return Context.Set<TRole>(); }
        }

        private DbSet<IdentityRoleClaim<TKey>> RoleClaims { get { return Context.Set<IdentityRoleClaim<TKey>>(); } }
    }
}
