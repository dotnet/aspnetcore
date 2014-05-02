// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Identity.Test
{
    public class NoopRoleStore : IRoleStore<TestRole>
    {
        public Task CreateAsync(TestRole user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task UpdateAsync(TestRole user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task<string> GetRoleNameAsync(TestRole role, CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult<string>(null);
        }

        public Task SetRoleNameAsync(TestRole role, string roleName, CancellationToken cancellationToken = new CancellationToken())
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

        public Task DeleteAsync(TestRole user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task<string> GetRoleIdAsync(TestRole role, CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult<string>(null);
        }
    }
}