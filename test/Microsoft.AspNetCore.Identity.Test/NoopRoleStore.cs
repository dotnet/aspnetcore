// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity.Test
{
    public class NoopRoleStore : IRoleStore<TestRole>
    {
        public Task<IdentityResult> CreateAsync(TestRole user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> UpdateAsync(TestRole user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<string> GetRoleNameAsync(TestRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<string>(null);
        }

        public Task SetRoleNameAsync(TestRole role, string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task<TestRole> FindByIdAsync(string roleId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<TestRole>(null);
        }

        public Task<TestRole> FindByNameAsync(string userName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<TestRole>(null);
        }

        public void Dispose()
        {
        }

        public Task<IdentityResult> DeleteAsync(TestRole user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<string> GetRoleIdAsync(TestRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<string>(null);
        }

        public Task<string> GetNormalizedRoleNameAsync(TestRole role, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<string>(null);
        }

        public Task SetNormalizedRoleNameAsync(TestRole role, string normalizedName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }
    }
}