// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.AspNetCore.Identity.Service.EntityFrameworkCore
{
    public abstract class IdentityServiceDbContext<TUser,TApplication> : 
        IdentityServiceDbContext<TUser,IdentityRole,string,TApplication,string>
        where TUser : IdentityUser
        where TApplication : IdentityServiceApplication
    {
        public IdentityServiceDbContext(DbContextOptions options) : base(options) { }
    }

    public abstract class IdentityServiceDbContext<TUser,TRole,TUserKey,TApplication,TApplicationKey>
        : IdentityServiceDbContext<
            TUser,
            TRole,
            TUserKey,
            IdentityUserClaim<TUserKey>,
            IdentityUserRole<TUserKey>,
            IdentityUserLogin<TUserKey>,
            IdentityRoleClaim<TUserKey>,
            IdentityUserToken<TUserKey>,
            TApplication,
            IdentityServiceScope<TApplicationKey>,
            IdentityServiceApplicationClaim<TApplicationKey>,
            IdentityServiceRedirectUri<TApplicationKey>,
            TApplicationKey>
        where TUser : IdentityUser<TUserKey>
        where TRole : IdentityRole<TUserKey>
        where TUserKey : IEquatable<TUserKey>
        where TApplication : IdentityServiceApplication<TApplicationKey,TUserKey>
        where TApplicationKey : IEquatable<TApplicationKey>
    {
        public IdentityServiceDbContext(DbContextOptions options) : base(options) { }
    }

    public abstract class IdentityServiceDbContext<
        TUser,
        TRole,
        TUserKey,
        TUserClaim,
        TUserRole,
        TUserLogin,
        TRoleClaim,
        TUserToken,
        TApplication,
        TScope,
        TApplicationClaim,
        TRedirectUri,
        TApplicationKey> :
        IdentityDbContext<TUser, TRole, TUserKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken>
        where TUser : IdentityUser<TUserKey>
        where TRole : IdentityRole<TUserKey>
        where TUserKey : IEquatable<TUserKey>
        where TUserClaim : IdentityUserClaim<TUserKey>
        where TUserRole : IdentityUserRole<TUserKey>
        where TUserLogin : IdentityUserLogin<TUserKey>
        where TRoleClaim : IdentityRoleClaim<TUserKey>
        where TUserToken : IdentityUserToken<TUserKey>
        where TApplication : IdentityServiceApplication<TApplicationKey, TUserKey, TScope, TApplicationClaim, TRedirectUri>
        where TScope : IdentityServiceScope<TApplicationKey>
        where TApplicationClaim : IdentityServiceApplicationClaim<TApplicationKey>
        where TRedirectUri : IdentityServiceRedirectUri<TApplicationKey>
        where TApplicationKey : IEquatable<TApplicationKey>
    {
        public IdentityServiceDbContext(DbContextOptions options)
            : base(options)
        { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<TApplication>(b =>
            {
                b.ToTable("AspNetApplications");
                b.HasKey(a => a.Id);

                b.Property(a => a.Name)
                    .HasMaxLength(256)
                    .IsRequired();
                b.HasIndex(a => a.Name)
                    .HasName("NameIndex")
                    .IsUnique();

                b.Property(a => a.ClientId)
                    .HasMaxLength(256)
                    .IsRequired();
                b.HasIndex(a => a.ClientId)
                    .HasName("ClientIdIndex")
                    .IsUnique();

                b.HasOne<TUser>()
                    .WithMany()
                    .HasForeignKey(a => a.UserId)
                    .IsRequired(false);

                b.Property(a => a.ConcurrencyStamp)
                .IsConcurrencyToken();

                b.HasMany(a => a.RedirectUris)
                    .WithOne()
                    .HasForeignKey(fk => fk.ApplicationId)
                    .IsRequired();

                b.HasMany(a => a.Scopes)
                    .WithOne()
                    .HasForeignKey(fk => fk.ApplicationId)
                    .IsRequired();

                b.HasMany(a => a.Claims)
                    .WithOne()
                    .HasForeignKey(fk => fk.ApplicationId)
                    .IsRequired();
            });

            builder.Entity<TRedirectUri>(b =>
            {
                b.ToTable("AspNetRedirectUris");
                b.HasKey(a => a.Id);
                b.Property(ru => ru.Value)
                    .HasMaxLength(256)
                    .IsRequired();
            });

            builder.Entity<TScope>(b =>
            {
                b.ToTable("AspNetScopes");
                b.HasKey(s => s.Id);
                b.Property(s => s.Value)
                    .HasMaxLength(256)
                    .IsRequired();
            });

            builder.Entity<TApplicationClaim>(b =>
            {
                b.ToTable("AspNetApplicationClaims");
                b.HasKey(s => s.Id);
                b.Property(s => s.ClaimType)
                    .HasMaxLength(256)
                    .IsRequired();
                b.Property(s => s.ClaimValue)
                    .HasMaxLength(256)
                    .IsRequired();
            });
        }

        public DbSet<TApplication> Applications { get; set; }
        public DbSet<TRedirectUri> RedirectUris { get; set; }
        public DbSet<TScope> Scopes { get; set; }
        public DbSet<TApplicationClaim> ApplicationClaims { get; set; }
    }
}
