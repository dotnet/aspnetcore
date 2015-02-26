// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity.Test;

namespace Microsoft.AspNet.Identity.InMemory
{
    public class InMemoryRoleStore<TRole> : IQueryableRoleStore<TRole>, IRoleClaimStore<TRole> where TRole : TestRole

    {
        private readonly Dictionary<string, TRole> _roles = new Dictionary<string, TRole>();

        public Task<IdentityResult> CreateAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            _roles[role.Id] = role;
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> DeleteAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (role == null || !_roles.ContainsKey(role.Id))
            {
                throw new InvalidOperationException("Unknown role");
            }
            _roles.Remove(role.Id);
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<string> GetRoleIdAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(role.Id);
        }

        public Task<string> GetRoleNameAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(role.Name);
        }

        public Task SetRoleNameAsync(TRole role, string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            role.Name = roleName;
            return Task.FromResult(0);
        }

        public Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            _roles[role.Id] = role;
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<TRole> FindByIdAsync(string roleId, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_roles.ContainsKey(roleId))
            {
                return Task.FromResult(_roles[roleId]);
            }
            return Task.FromResult<TRole>(null);
        }

        public Task<TRole> FindByNameAsync(string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return
                Task.FromResult(
                    Roles.SingleOrDefault(r => String.Equals(r.Name, roleName, StringComparison.OrdinalIgnoreCase)));
        }

        public void Dispose()
        {
        }

        public Task<IList<Claim>> GetClaimsAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            var claims = role.Claims.Select(c => new Claim(c.ClaimType, c.ClaimValue)).ToList();
            return Task.FromResult<IList<Claim>>(claims);
        }

        public Task AddClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default(CancellationToken))
        {
            role.Claims.Add(new TestRoleClaim<string> { ClaimType = claim.Type, ClaimValue = claim.Value, RoleId = role.Id });
            return Task.FromResult(0);
        }

        public Task RemoveClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default(CancellationToken))
        {
            var entity =
                role.Claims.FirstOrDefault(
                    ur => ur.RoleId == role.Id && ur.ClaimType == claim.Type && ur.ClaimValue == claim.Value);
            if (entity != null)
            {
                role.Claims.Remove(entity);
            }
            return Task.FromResult(0);
        }

        public Task<string> GetNormalizedRoleNameAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(role.NormalizedName);
        }

        public Task SetNormalizedRoleNameAsync(TRole role, string normalizedName, CancellationToken cancellationToken = default(CancellationToken))
        {
            role.NormalizedName = normalizedName;
            return Task.FromResult(0);
        }

        public IQueryable<TRole> Roles
        {
            get { return _roles.Values.AsQueryable(); }
        }
    }
}