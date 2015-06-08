// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Metadata;

namespace Microsoft.AspNet.Identity.EntityFramework
{
    public class IdentityDbContext : IdentityDbContext<IdentityUser, IdentityRole, string> { }

    public class IdentityDbContext<TUser> : IdentityDbContext<TUser, IdentityRole, string> where TUser : IdentityUser
    { }

    public class IdentityDbContext<TUser, TRole, TKey> : DbContext
        where TUser : IdentityUser<TKey>
        where TRole : IdentityRole<TKey>
        where TKey : IEquatable<TKey>
    {
        public DbSet<TUser> Users { get; set; }
        public DbSet<IdentityUserClaim<TKey>> UserClaims { get; set; }
        public DbSet<IdentityUserLogin<TKey>> UserLogins { get; set; }
        public DbSet<IdentityUserRole<TKey>> UserRoles { get; set; }
        public DbSet<TRole> Roles { get; set; }
        public DbSet<IdentityRoleClaim<TKey>> RoleClaims { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // TODO: Temporary change. 
            // Can be reverted once https://github.com/aspnet/EntityFramework/issues/2175 is closed
            builder.ForSqlServer().UseIdentity();

            builder.Entity<TUser>(b =>
                {
                    b.Key(u => u.Id);
                    b.Index(u => u.NormalizedUserName).Name("UserNameIndex");
                    b.Index(u => u.NormalizedEmail).Name("EmailIndex");
                    b.Table("AspNetUsers");
                    b.Property(u => u.ConcurrencyStamp).ConcurrencyToken();

                    b.Property(u => u.UserName).MaxLength(256);
                    b.Property(u => u.NormalizedUserName).MaxLength(256);
                    b.Property(u => u.Email).MaxLength(256);
                    b.Property(u => u.NormalizedEmail).MaxLength(256);
                    b.Collection(u => u.Claims).InverseReference().ForeignKey(uc => uc.UserId);
                    b.Collection(u => u.Logins).InverseReference().ForeignKey(ul => ul.UserId);
                    b.Collection(u => u.Roles).InverseReference().ForeignKey(ur => ur.UserId);
                });

            builder.Entity<TRole>(b =>
                {
                    b.Key(r => r.Id);
                    b.Index(r => r.NormalizedName).Name("RoleNameIndex");
                    b.Table("AspNetRoles");
                    b.Property(r => r.ConcurrencyStamp).ConcurrencyToken();

                    b.Property(u => u.Name).MaxLength(256);
                    b.Property(u => u.NormalizedName).MaxLength(256);

                    b.Collection(r => r.Users).InverseReference().ForeignKey(ur => ur.RoleId);
                    b.Collection(r => r.Claims).InverseReference().ForeignKey(rc => rc.RoleId);
                });

            builder.Entity<IdentityUserClaim<TKey>>(b =>
                {
                    b.Key(uc => uc.Id);
                    b.Table("AspNetUserClaims");
                });

            builder.Entity<IdentityRoleClaim<TKey>>(b =>
                {
                    b.Key(rc => rc.Id);
                    b.Table("AspNetRoleClaims");
                });

            builder.Entity<IdentityUserRole<TKey>>(b =>
                {
                    b.Key(r => new { r.UserId, r.RoleId });
                    b.Table("AspNetUserRoles");
                });
            // Blocks delete currently without cascade
            //.ForeignKeys(fk => fk.ForeignKey<TUser>(f => f.UserId))
            //.ForeignKeys(fk => fk.ForeignKey<TRole>(f => f.RoleId));

            builder.Entity<IdentityUserLogin<TKey>>(b =>
                {
                    b.Key(l => new { l.LoginProvider, l.ProviderKey });
                    b.Table("AspNetUserLogins");
                });
        }
    }
}