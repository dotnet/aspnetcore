// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;
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
        public IdentityDbContext(DbContextOptions options) : base(options)
        {
            
        }

        public IdentityDbContext(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            
        }

        public IdentityDbContext(IServiceProvider serviceProvider, DbContextOptions options) : base(serviceProvider, options)
        {
            
        }

        protected IdentityDbContext()
        {
            
        }

        public DbSet<TUser> Users { get; set; }
        public DbSet<IdentityUserClaim<TKey>> UserClaims { get; set; }
        public DbSet<IdentityUserLogin<TKey>> UserLogins { get; set; }
        public DbSet<IdentityUserRole<TKey>> UserRoles { get; set; }
        public DbSet<TRole> Roles { get; set; }
        public DbSet<IdentityRoleClaim<TKey>> RoleClaims { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<TUser>(b =>
                {
                    b.HasKey(u => u.Id);
                    b.Index(u => u.NormalizedUserName).Name("UserNameIndex");
                    b.Index(u => u.NormalizedEmail).Name("EmailIndex");
                    b.ToTable("AspNetUsers");
                    b.Property(u => u.ConcurrencyStamp).IsConcurrencyToken();

                    b.Property(u => u.UserName).HasMaxLength(256);
                    b.Property(u => u.NormalizedUserName).HasMaxLength(256);
                    b.Property(u => u.Email).HasMaxLength(256);
                    b.Property(u => u.NormalizedEmail).HasMaxLength(256);
                    b.HasMany(u => u.Claims).WithOne().ForeignKey(uc => uc.UserId);
                    b.HasMany(u => u.Logins).WithOne().ForeignKey(ul => ul.UserId);
                    b.HasMany(u => u.Roles).WithOne().ForeignKey(ur => ur.UserId);
                });

            builder.Entity<TRole>(b =>
                {
                    b.HasKey(r => r.Id);
                    b.Index(r => r.NormalizedName).Name("RoleNameIndex");
                    b.ToTable("AspNetRoles");
                    b.Property(r => r.ConcurrencyStamp).IsConcurrencyToken();

                    b.Property(u => u.Name).HasMaxLength(256);
                    b.Property(u => u.NormalizedName).HasMaxLength(256);

                    b.HasMany(r => r.Users).WithOne().ForeignKey(ur => ur.RoleId);
                    b.HasMany(r => r.Claims).WithOne().ForeignKey(rc => rc.RoleId);
                });

            builder.Entity<IdentityUserClaim<TKey>>(b =>
                {
                    b.HasKey(uc => uc.Id);
                    b.ToTable("AspNetUserClaims");
                });

            builder.Entity<IdentityRoleClaim<TKey>>(b =>
                {
                    b.HasKey(rc => rc.Id);
                    b.ToTable("AspNetRoleClaims");
                });

            builder.Entity<IdentityUserRole<TKey>>(b =>
                {
                    b.HasKey(r => new { r.UserId, r.RoleId });
                    b.ToTable("AspNetUserRoles");
                });
            // Blocks delete currently without cascade
            //.ForeignKeys(fk => fk.ForeignKey<TUser>(f => f.UserId))
            //.ForeignKeys(fk => fk.ForeignKey<TRole>(f => f.RoleId));

            builder.Entity<IdentityUserLogin<TKey>>(b =>
                {
                    b.HasKey(l => new { l.LoginProvider, l.ProviderKey });
                    b.ToTable("AspNetUserLogins");
                });
        }
    }
}