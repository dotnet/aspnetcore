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
            builder.Entity<TUser>(b =>
            {
                b.Key(u => u.Id);
                b.Property(u => u.UserName);
                b.ToTable("AspNetUsers");
            });

            builder.Entity<TRole>(b =>
            {
                b.Key(r => r.Id);
                b.ToTable("AspNetRoles");
            });

            builder.Entity<TUserRole>(b =>
            {
                b.Key(r => new { r.UserId, r.RoleId });
                b.ForeignKeys(fk => fk.ForeignKey<TUser>(f => f.UserId));
                b.ForeignKeys(fk => fk.ForeignKey<TRole>(f => f.RoleId));
                b.ToTable("AspNetUserRoles");
            });

            builder.Entity<TUserLogin>(b =>
            {
                b.Key(l => new { l.LoginProvider, l.ProviderKey, l.UserId });
                b.ForeignKeys(fk => fk.ForeignKey<TUser>(f => f.UserId));
                b.ToTable("AspNetUserLogins");
            });

            builder.Entity<TUserClaim>(b =>
            {
                b.Key(c => c.Id);
                b.ForeignKeys(fk => fk.ForeignKey<TUser>(f => f.UserId));
                b.ToTable("AspNetUserClaims");
            });

            builder.Entity<IdentityRoleClaim<TKey>>(b =>
            {
                b.Key(c => c.Id);
                b.ForeignKeys(fk => fk.ForeignKey<TRole>(f => f.RoleId));
                b.ToTable("AspNetRoleClaims");
            });
        }
    }
}