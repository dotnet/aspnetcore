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
    public class NoopUserStore : IUserStore<TestUser>
    {
        public Task<string> GetUserIdAsync(TestUser user, CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(user.Id);
        }

        public Task<string> GetUserNameAsync(TestUser user, CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(user.UserName);
        }

        public Task SetUserNameAsync(TestUser user, string userName, CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(0);
        }

        public Task CreateAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task UpdateAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }

        public Task<TestUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<TestUser>(null);
        }

        public Task<TestUser> FindByNameAsync(string userName, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<TestUser>(null);
        }

        public void Dispose()
        {
        }

        public Task DeleteAsync(TestUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(0);
        }
    }
}