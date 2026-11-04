// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity.Test;

public class NoopUserStore : IUserStore<PocoUser>
{
    public Task<string> GetUserIdAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(user.Id);
    }

    public Task<string> GetUserNameAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(user.UserName);
    }

    public Task SetUserNameAsync(PocoUser user, string userName, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(0);
    }

    public Task<IdentityResult> CreateAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<IdentityResult> UpdateAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<PocoUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult<PocoUser>(null);
    }

    public Task<PocoUser> FindByNameAsync(string userName, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult<PocoUser>(null);
    }

    public void Dispose()
    {
    }

    public Task<IdentityResult> DeleteAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(IdentityResult.Success);
    }

    public Task<string> GetNormalizedUserNameAsync(PocoUser user, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult<string>(null);
    }

    public Task SetNormalizedUserNameAsync(PocoUser user, string userName, CancellationToken cancellationToken = default(CancellationToken))
    {
        return Task.FromResult(0);
    }
}
