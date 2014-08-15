// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Identity.EntityFramework.Test
{
    public class CustomPocoTest
    {
        private const string ConnectionString = @"Server=(localdb)\v11.0;Database=CustomPocoTest;Trusted_Connection=True;";

        public class User<TKey> where TKey : IEquatable<TKey>
        {
            public TKey Id { get; set; }
            public string UserName { get; set; }
        }

        public class CustomDbContext<TUser> : DbContext where TUser : class
            //where TUser : User<TKey> where TKey : IEquatable<TKey>
        {
            public DbSet<TUser> Users { get; set; }

            public CustomDbContext(IServiceProvider services) : base(services) { }

            protected override void OnConfiguring(DbContextOptions builder)
            {
                builder.UseSqlServer(ConnectionString);
            }

            //protected override void OnModelCreating(ModelBuilder builder)
            //{
            //    builder.Entity<TUser>()
            //        .Key(u => u.Id)
            //        .Properties(ps => ps.Property(u => u.UserName));
            //}
        }

        //public static CustomDbContext<User<TKey>, TKey> CreateContext<TKey>(bool delete = false) where TKey : IEquatable<TKey>
        public static CustomDbContext<TUser> CreateContext<TUser>(bool delete = false) where TUser : class
        {
            var services = new ServiceCollection();
            services.AddEntityFramework().AddSqlServer();
            var serviceProvider = services.BuildServiceProvider();

            var db = new CustomDbContext<TUser>(serviceProvider);
            if (delete)
            {
                db.Database.EnsureDeleted();
            }
            db.Database.EnsureCreated();
            return db;
        }

        [Fact]
        public async Task CanUpdateNameGuid()
        {
            using (var db = CreateContext<User<Guid>>(true))
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

        [Fact]
        public async Task CanUpdateNameString()
        {
            using (var db = CreateContext<User<string>>(true))
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

        [Fact]
        public async Task CanCreateUserInt()
        {
            using (var db = CreateContext<User<int>>(true))
            {
                var user = new User<int> { Id = 11 };
                db.Users.Add(user);
                await db.SaveChangesAsync();
                user.UserName = "Boo";
                await db.SaveChangesAsync();
                var fetch = db.Users.First(u => u.UserName == "Boo");
                Assert.Equal(user, fetch);
            }
        }

        [Fact]
        public async Task CanCreateUserIntViaSet()
        {
            using (var db = CreateContext<User<int>>(true))
            {
                var user = new User<int> { Id = 11 };
                var users = db.Set<User<int>>();
                users.Add(user);
                await db.SaveChangesAsync();
                user.UserName = "Boo";
                await db.SaveChangesAsync();
                var fetch = users.First(u => u.UserName == "Boo");
                Assert.Equal(user, fetch);
            }
        }

        [Fact]
        public async Task CanUpdateNameInt()
        {
            using (var db = CreateContext<User<int>>(true))
            {
                var oldName = Guid.NewGuid().ToString();
                var user = new User<int> { UserName = oldName, Id = 1 };
                db.Users.Add(user);
                await db.SaveChangesAsync();
                var newName = Guid.NewGuid().ToString();
                user.UserName = newName;
                await db.SaveChangesAsync();
                Assert.Null(db.Users.SingleOrDefault(u => u.UserName == oldName));
                Assert.Equal(user, db.Users.Single(u => u.UserName == newName));
            }
        }

        [Fact]
        public async Task CanUpdateNameIntWithSet()
        {
            using (var db = CreateContext<User<int>>(true))
            {
                var oldName = Guid.NewGuid().ToString();
                var user = new User<int> { UserName = oldName, Id = 1 };
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