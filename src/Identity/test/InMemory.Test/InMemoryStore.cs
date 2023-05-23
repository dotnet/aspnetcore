// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Identity.Test;

namespace Microsoft.AspNetCore.Identity.InMemory;

public class InMemoryStore<TUser, TRole> :
    InMemoryUserStore<TUser>,
    IUserRoleStore<TUser>,
    IQueryableRoleStore<TRole>,
    IRoleClaimStore<TRole>
    where TRole : PocoRole
    where TUser : PocoUser
{
    // RoleId == roleName for InMemory
    public Task AddToRoleAsync(TUser user, string role, CancellationToken cancellationToken = default(CancellationToken))
    {
        var roleEntity = _roles.Values.SingleOrDefault(r => r.NormalizedName == role);
        if (roleEntity != null)
        {
            user.Roles.Add(new PocoUserRole { RoleId = roleEntity.Id, UserId = user.Id });
        }
        return Task.FromResult(0);
    }

    // RoleId == roleName for InMemory
    public Task RemoveFromRoleAsync(TUser user, string role, CancellationToken cancellationToken = default(CancellationToken))
    {
        var roleObject = _roles.Values.SingleOrDefault(r => r.NormalizedName == role);
        var roleEntity = user.Roles.SingleOrDefault(ur => ur.RoleId == roleObject.Id);
        if (roleEntity != null)
        {
            user.Roles.Remove(roleEntity);
        }
        return Task.FromResult(0);
    }

    public Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        IList<string> roles = new List<string>();
        foreach (var r in user.Roles.Select(ur => ur.RoleId))
        {
            roles.Add(_roles[r].Name);
        }
        return Task.FromResult(roles);
    }

    public Task<bool> IsInRoleAsync(TUser user, string role, CancellationToken cancellationToken = default(CancellationToken))
    {
        var roleObject = _roles.Values.SingleOrDefault(r => r.NormalizedName == role);
        bool result = roleObject != null && user.Roles.Any(ur => ur.RoleId == roleObject.Id);
        return Task.FromResult(result);
    }

    // RoleId == rolename for inmemory store tests
    public Task<IList<TUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken = default(CancellationToken))
    {
        ArgumentException.ThrowIfNullOrEmpty(roleName);

        var role = _roles.Values.Where(x => x.NormalizedName.Equals(roleName)).SingleOrDefault();
        if (role == null)
        {
            return Task.FromResult<IList<TUser>>(new List<TUser>());
        }
        return Task.FromResult<IList<TUser>>(Users.Where(u => (u.Roles.Where(x => x.RoleId == role.Id).Any())).Select(x => x).ToList());
    }

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

    Task<TRole> IRoleStore<TRole>.FindByIdAsync(string roleId, CancellationToken cancellationToken)
    {
        if (_roles.ContainsKey(roleId))
        {
            return Task.FromResult(_roles[roleId]);
        }
        return Task.FromResult<TRole>(null);
    }

    Task<TRole> IRoleStore<TRole>.FindByNameAsync(string roleName, CancellationToken cancellationToken)
    {
        return
            Task.FromResult(
                Roles.SingleOrDefault(r => String.Equals(r.NormalizedName, roleName, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<IList<Claim>> GetClaimsAsync(TRole role, CancellationToken cancellationToken = default(CancellationToken))
    {
        var claims = role.Claims.Select(c => new Claim(c.ClaimType, c.ClaimValue)).ToList();
        return Task.FromResult<IList<Claim>>(claims);
    }

    public Task AddClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default(CancellationToken))
    {
        role.Claims.Add(new PocoRoleClaim<string> { ClaimType = claim.Type, ClaimValue = claim.Value, RoleId = role.Id });
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
