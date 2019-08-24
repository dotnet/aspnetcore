// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Data.Common;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore.EntityConfiguration;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.InMemory.Test
{
    public class InMemoryCustomContext :
        InMemoryCustomContext<IdentityUser, IdentityRole, string>
    {
        private InMemoryCustomContext(DbConnection connection) : base(connection)
        { }

        public new static InMemoryCustomContext Create(DbConnection connection)
            => Initialize(new InMemoryCustomContext(connection));

        public static TContext Initialize<TContext>(TContext context) where TContext : DbContext
        {
            context.Database.EnsureCreated();

            return context;
        }
    }

    public class InMemoryCustomContext<TUser> :
        DbContext
        where TUser : IdentityUser
    {
        private readonly DbConnection _connection;
        private InMemoryCustomContext(DbConnection connection)
        {
            _connection = connection;
        }

        public static InMemoryCustomContext<TUser> Create(DbConnection connection)
            => InMemoryCustomContext.Initialize(new InMemoryCustomContext<TUser>(connection));

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var maxKeyLength = 50;

            modelBuilder.ApplyConfiguration(new UserConfiguration<TUser>() { MaxKeyLength = maxKeyLength });
            modelBuilder.ApplyConfiguration(new UserClaimConfiguration());
            modelBuilder.ApplyConfiguration(new UserLoginConfiguration() { MaxKeyLength = maxKeyLength });
            modelBuilder.ApplyConfiguration(new UserTokenConfiguration() { MaxKeyLength = maxKeyLength });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlite(_connection);
    }

    public class InMemoryCustomContext<TUser, TRole, TKey> : InMemoryCustomContext<TUser, TRole, TKey, IdentityUserClaim<TKey>, IdentityUserRole<TKey>, IdentityUserLogin<TKey>, IdentityRoleClaim<TKey>, IdentityUserToken<TKey>>
        where TUser : IdentityUser<TKey>
        where TRole : IdentityRole<TKey>
        where TKey : IEquatable<TKey>
    {
        private readonly DbConnection _connection;

        protected InMemoryCustomContext(DbConnection connection)
        {
            _connection = connection;
        }

        public static InMemoryCustomContext<TUser, TRole, TKey> Create(DbConnection connection)
            => InMemoryCustomContext.Initialize(new InMemoryCustomContext<TUser, TRole, TKey>(connection));

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlite(_connection);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new UserHasRolesConfiguration<TUser, TKey>());
            modelBuilder.ApplyConfiguration(new RoleConfiguration<TRole, TKey>());
            modelBuilder.ApplyConfiguration(new RoleClaimConfiguration<TKey>());
            modelBuilder.ApplyConfiguration(new UserRoleConfiguration<TKey>());

        }
    }

    public abstract class InMemoryCustomContext<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken> :
            DbContext
        where TUser : IdentityUser<TKey>
        where TRole : IdentityRole<TKey>
        where TKey : IEquatable<TKey>
        where TUserClaim : IdentityUserClaim<TKey>
        where TUserRole : IdentityUserRole<TKey>
        where TUserLogin : IdentityUserLogin<TKey>
        where TRoleClaim : IdentityRoleClaim<TKey>
        where TUserToken : IdentityUserToken<TKey>
    {
        protected InMemoryCustomContext() { }

        protected InMemoryCustomContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var maxKeyLength = 50;

            modelBuilder.ApplyConfiguration(new UserConfiguration<TUser, TUserClaim, TUserLogin, TUserToken, TKey>() { MaxKeyLength = maxKeyLength });
            modelBuilder.ApplyConfiguration(new UserClaimConfiguration<TUserClaim, TKey>());
            modelBuilder.ApplyConfiguration(new UserLoginConfiguration<TUserLogin, TKey>() { MaxKeyLength = maxKeyLength });
            modelBuilder.ApplyConfiguration(new UserTokenConfiguration<TUserToken, TKey>() { MaxKeyLength = maxKeyLength });
        }

    }
}
