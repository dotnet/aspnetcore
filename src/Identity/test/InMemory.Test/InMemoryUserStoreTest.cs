// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity.Test;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Identity.InMemory.Test
{
    public class InMemoryUserStoreTest : UserManagerSpecificationTestBase<PocoUser, string>, IClassFixture<InMemoryUserStoreTest.Fixture>
    {
        protected override object CreateTestContext()
        {
            return new InMemoryUserStore<PocoUser>();
        }

        protected override void AddUserStore(IServiceCollection services, object context = null)
        {
            services.AddSingleton<IUserStore<PocoUser>>((InMemoryUserStore<PocoUser>)context);
        }

        protected override void SetUserPasswordHash(PocoUser user, string hashedPassword)
        {
            user.PasswordHash = hashedPassword;
        }

        protected override PocoUser CreateTestUser(string namePrefix = "", string email = "", string phoneNumber = "",
            bool lockoutEnabled = false, DateTimeOffset? lockoutEnd = default(DateTimeOffset?), bool useNamePrefixAsUserName = false)
        {
            return new PocoUser
            {
                UserName = useNamePrefixAsUserName ? namePrefix : string.Format("{0}{1}", namePrefix, Guid.NewGuid()),
                Email = email,
                PhoneNumber = phoneNumber,
                LockoutEnabled = lockoutEnabled,
                LockoutEnd = lockoutEnd
            };
        }

        protected override Expression<Func<PocoUser, bool>> UserNameEqualsPredicate(string userName) => u => u.UserName == userName;

        protected override Expression<Func<PocoUser, bool>> UserNameStartsWithPredicate(string userName) => u => u.UserName.StartsWith(userName);

        public class Fixture : IDisposable
        {
            private readonly SqliteConnection _connection
                = new SqliteConnection($"DataSource=:memory:");

            public Fixture()
            {
                _connection.Open();
            }

            public void Dispose()
            {
                _connection.Close();
                _connection.Dispose();
            }
        }
    }
}
