// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Data.Common;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore.EntityConfiguration;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.InMemory.Test
{
    public class InMemoryNonIdentityDerivedContext :
        InMemoryNonIdentityDerivedContext<IdentityUser, IdentityRole, string>
    {
        private InMemoryNonIdentityDerivedContext(DbConnection connection) : base(connection)
        { }

        public new static InMemoryNonIdentityDerivedContext Create(DbConnection connection)
            => Initialize(new InMemoryNonIdentityDerivedContext(connection));

        public static TContext Initialize<TContext>(TContext context) where TContext : DbContext
        {
            context.Database.EnsureCreated();

            return context;
        }
    }

    public class InMemoryNonIdentityDerivedContext<TUser> :
        DbContext
        where TUser : IdentityUser
    {
        private readonly DbConnection _connection;
        private InMemoryNonIdentityDerivedContext(DbConnection connection)
        {
            _connection = connection;
        }

        public static InMemoryNonIdentityDerivedContext<TUser> Create(DbConnection connection)
            => InMemoryNonIdentityDerivedContext.Initialize(new InMemoryNonIdentityDerivedContext<TUser>(connection));

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

    public class InMemoryNonIdentityDerivedContext<TUser, TRole, TKey> : InMemoryNonIdentityDerivedContext<TUser, TRole, TKey, IdentityUserClaim<TKey>, IdentityUserRole<TKey>, IdentityUserLogin<TKey>, IdentityRoleClaim<TKey>, IdentityUserToken<TKey>>
        where TUser : IdentityUser<TKey>
        where TRole : IdentityRole<TKey>
        where TKey : IEquatable<TKey>
    {
        private readonly DbConnection _connection;

        protected InMemoryNonIdentityDerivedContext(DbConnection connection)
        {
            _connection = connection;
        }

        public static InMemoryNonIdentityDerivedContext<TUser, TRole, TKey> Create(DbConnection connection)
            => InMemoryNonIdentityDerivedContext.Initialize(new InMemoryNonIdentityDerivedContext<TUser, TRole, TKey>(connection));

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

    public abstract class InMemoryNonIdentityDerivedContext<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken> :
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
        protected InMemoryNonIdentityDerivedContext() { }

        protected InMemoryNonIdentityDerivedContext(DbContextOptions options)
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
