// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test
{
    public class CustomPocoTest : IClassFixture<ScratchDatabaseFixture>
    {
        private readonly ScratchDatabaseFixture _fixture;

        public CustomPocoTest(ScratchDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        public class User<TKey> where TKey : IEquatable<TKey>
        {
            public TKey Id { get; set; }
            public string UserName { get; set; }
        }

        public class CustomDbContext<TKey> : DbContext where TKey : IEquatable<TKey>
        {
            public CustomDbContext(DbContextOptions options) : base(options)
            { }

            public DbSet<User<TKey>> Users { get; set; }

        }

        public CustomDbContext<TKey> GetContext<TKey>() where TKey : IEquatable<TKey>
        {
            return DbUtil.Create<CustomDbContext<TKey>>(_fixture.ConnectionString);
        }

        public CustomDbContext<TKey> CreateContext<TKey>(bool delete = false) where TKey : IEquatable<TKey>
        {
            var db = GetContext<TKey>();
            if (delete)
            {
                db.Database.EnsureDeleted();
            }
            db.Database.EnsureCreated();
            return db;
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task CanUpdateNameGuid()
        {
            using (var db = CreateContext<Guid>(true))
            {
                var oldName = Guid.NewGuid().ToString();
                var user = new User<Guid> { UserName = oldName, Id = Guid.NewGuid() };
                db.Users.Add(user);
                await db.SaveChangesAsync();
                var newName = Guid.NewGuid().ToString();
                user.UserName = newName;
                await db.SaveChangesAsync();
                Assert.Null(db.Users.SingleOrDefault(u => u.UserName == oldName));
                Assert.Equal(user, db.Users.Single(u => u.UserName == newName));
            }
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task CanUpdateNameString()
        {
            using (var db = CreateContext<string>(true))
            {
                var oldName = Guid.NewGuid().ToString();
                var user = new User<string> { UserName = oldName, Id = Guid.NewGuid().ToString() };
                db.Users.Add(user);
                await db.SaveChangesAsync();
                var newName = Guid.NewGuid().ToString();
                user.UserName = newName;
                await db.SaveChangesAsync();
                Assert.Null(db.Users.SingleOrDefault(u => u.UserName == oldName));
                Assert.Equal(user, db.Users.Single(u => u.UserName == newName));
            }
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task CanCreateUserInt()
        {
            using (var db = CreateContext<int>(true))
            {
                var user = new User<int>();
                db.Users.Add(user);
                await db.SaveChangesAsync();
                user.UserName = "Boo";
                await db.SaveChangesAsync();
                var fetch = db.Users.First(u => u.UserName == "Boo");
                Assert.Equal(user, fetch);
            }
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task CanCreateUserIntViaSet()
        {
            using (var db = CreateContext<int>(true))
            {
                var user = new User<int>();
                var users = db.Set<User<int>>();
                users.Add(user);
                await db.SaveChangesAsync();
                user.UserName = "Boo";
                await db.SaveChangesAsync();
                var fetch = users.First(u => u.UserName == "Boo");
                Assert.Equal(user, fetch);
            }
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task CanUpdateNameInt()
        {
            using (var db = CreateContext<int>(true))
            {
                var oldName = Guid.NewGuid().ToString();
                var user = new User<int> { UserName = oldName };
                db.Users.Add(user);
                await db.SaveChangesAsync();
                var newName = Guid.NewGuid().ToString();
                user.UserName = newName;
                await db.SaveChangesAsync();
                Assert.Null(db.Users.SingleOrDefault(u => u.UserName == oldName));
                Assert.Equal(user, db.Users.Single(u => u.UserName == newName));
            }
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task CanUpdateNameIntWithSet()
        {
            using (var db = CreateContext<int>(true))
            {
                var oldName = Guid.NewGuid().ToString();
                var user = new User<int> { UserName = oldName };
                db.Set<User<int>>().Add(user);
                await db.SaveChangesAsync();
                var newName = Guid.NewGuid().ToString();
                user.UserName = newName;
                await db.SaveChangesAsync();
                Assert.Null(db.Set<User<int>>().SingleOrDefault(u => u.UserName == oldName));
                Assert.Equal(user, db.Set<User<int>>().Single(u => u.UserName == newName));
            }
        }
    }
}