// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity.Test;

public class NoopRoleStore : IRoleStore<PocoRole>
{
    public Task<IdentityResult> CreateAsync(PocoRole user, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> UpdateAsync(PocoRole user, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<string> GetRoleNameAsync(PocoRole role, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult<string>(null);
    }

    public Task SetRoleNameAsync(PocoRole role, string roleName, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(0);
    }

    public Task<PocoRole> FindByIdAsync(string roleId, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult<PocoRole>(null);
    }

    public Task<PocoRole> FindByNameAsync(string userName, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult<PocoRole>(null);
    }

    public void Dispose()
    {
    }

    public Task<IdentityResult> DeleteAsync(PocoRole user, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<string> GetRoleIdAsync(PocoRole role, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult<string>(null);
    }

    public Task<string> GetNormalizedRoleNameAsync(PocoRole role, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult<string>(null);
    }

    public Task SetNormalizedRoleNameAsync(PocoRole role, string normalizedName, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(0);
    }
}
