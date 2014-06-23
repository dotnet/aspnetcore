// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;

namespace Microsoft.AspNet.Identity.EntityFramework.InMemory.Test
{
    public class InMemoryContext :
        InMemoryContext<InMemoryUser, IdentityRole, string, IdentityUserLogin, IdentityUserRole, IdentityUserClaim>
    {
        public InMemoryContext() { }
        public InMemoryContext(IServiceProvider serviceProvider) : base(serviceProvider) { }
    }

    public class InMemoryContext<TUser> :
        InMemoryContext<TUser, IdentityRole, string, IdentityUserLogin, IdentityUserRole, IdentityUserClaim>
        where TUser : InMemoryUser<string, IdentityUserLogin, IdentityUserRole, IdentityUserClaim>
    {
        public InMemoryContext() { }
        public InMemoryContext(IServiceProvider serviceProvider) : base(serviceProvider) { }
    }

    public class InMemoryContext<TUser, TRole, TKey, TUserLogin, TUserRole, TUserClaim> : DbContext
        where TUser : InMemoryUser<TKey, TUserLogin, TUserRole, TUserClaim>
        where TRole : IdentityRole<TKey>
        where TUserLogin : IdentityUserLogin<TKey>
        where TUserRole : IdentityUserRole<TKey>
        where TUserClaim : IdentityUserClaim<TKey>
        where TKey : IEquatable<TKey>
    {

        public DbSet<TUser> Users { get; set; }
        public DbSet<TRole> Roles { get; set; }
        public DbSet<IdentityRoleClaim> RoleClaims { get; set; }

        public InMemoryContext(IServiceProvider serviceProvider)
        : base(serviceProvider) { }

        public InMemoryContext() { }

        protected override void OnConfiguring(DbContextOptions builder)
        {
            builder.UseInMemoryStore();
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<TUser>()
                .Key(u => u.Id)
                .Properties(ps => ps.Property(u => u.UserName))
                .ToTable("AspNetUsers");

            builder.Entity<TRole>()
                .Key(r => r.Id)
                .ToTable("AspNetRoles");

            builder.Entity<TUserRole>()
                .Key(r => new { r.UserId, r.RoleId })
                .ForeignKeys(fk => fk.ForeignKey<TUser>(f => f.UserId))
                .ForeignKeys(fk => fk.ForeignKey<TRole>(f => f.RoleId))
                .ToTable("AspNetUserRoles");

            builder.Entity<TUserLogin>()
                .Key(l => new { l.LoginProvider, l.ProviderKey, l.UserId })
                .ForeignKeys(fk => fk.ForeignKey<TUser>(f => f.UserId))
                .ToTable("AspNetUserLogins");

            builder.Entity<TUserClaim>()
                .Key(c => c.Id)
                .ForeignKeys(fk => fk.ForeignKey<TUser>(f => f.UserId))
                .ToTable("AspNetUserClaims");

            builder.Entity<IdentityRoleClaim>()
                .Key(c => c.Id)
                .ForeignKeys(fk => fk.ForeignKey<TRole>(f => f.RoleId))
                .ToTable("AspNetRoleClaims");

        }
    }
}