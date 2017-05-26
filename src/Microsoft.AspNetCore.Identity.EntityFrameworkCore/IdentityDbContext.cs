// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore
{
    /// <summary>
    /// Base class for the Entity Framework database context used for identity.
    /// </summary>
    public class IdentityDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="IdentityDbContext"/>.
        /// </summary>
        /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
        public IdentityDbContext(DbContextOptions options) : base(options) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityDbContext" /> class.
        /// </summary>
        protected IdentityDbContext() { }
    }

    /// <summary>
    /// Base class for the Entity Framework database context used for identity.
    /// </summary>
    /// <typeparam name="TUser">The type of the user objects.</typeparam>
    public class IdentityDbContext<TUser> : IdentityDbContext<TUser, IdentityRole, string> where TUser : IdentityUser
    {
        /// <summary>
        /// Initializes a new instance of <see cref="IdentityDbContext"/>.
        /// </summary>
        /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
        public IdentityDbContext(DbContextOptions options) : base(options) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="IdentityDbContext" /> class.
        /// </summary>
        protected IdentityDbContext() { }
    }

    /// <summary>
    /// Base class for the Entity Framework database context used for identity.
    /// </summary>
    /// <typeparam name="TUser">The type of user objects.</typeparam>
    /// <typeparam name="TRole">The type of role objects.</typeparam>
    /// <typeparam name="TKey">The type of the primary key for users and roles.</typeparam>
    public class IdentityDbContext<TUser, TRole, TKey> : IdentityDbContext<TUser, TRole, TKey, IdentityUserClaim<TKey>, IdentityUserRole<TKey>, IdentityUserLogin<TKey>, IdentityRoleClaim<TKey>, IdentityUserToken<TKey>>
        where TUser : IdentityUser<TKey>
        where TRole : IdentityRole<TKey>
        where TKey : IEquatable<TKey>
    {
        /// <summary>
        /// Initializes a new instance of the db context.
        /// </summary>
        /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
        public IdentityDbContext(DbContextOptions options) : base(options) { }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        protected IdentityDbContext() { }
    }

    /// <summary>
    /// Base class for the Entity Framework database context used for identity.
    /// </summary>
    /// <typeparam name="TUser">The type of user objects.</typeparam>
    /// <typeparam name="TRole">The type of role objects.</typeparam>
    /// <typeparam name="TKey">The type of the primary key for users and roles.</typeparam>
    /// <typeparam name="TUserClaim">The type of the user claim object.</typeparam>
    /// <typeparam name="TUserRole">The type of the user role object.</typeparam>
    /// <typeparam name="TUserLogin">The type of the user login object.</typeparam>
    /// <typeparam name="TRoleClaim">The type of the role claim object.</typeparam>
    /// <typeparam name="TUserToken">The type of the user token object.</typeparam>
    public abstract class IdentityDbContext<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken> : DbContext
        where TUser : IdentityUser<TKey, TUserClaim, TUserRole, TUserLogin, TUserToken>
        where TRole : IdentityRole<TKey, TUserRole, TRoleClaim>
        where TKey : IEquatable<TKey>
        where TUserClaim : IdentityUserClaim<TKey>
        where TUserRole : IdentityUserRole<TKey>
        where TUserLogin : IdentityUserLogin<TKey>
        where TRoleClaim : IdentityRoleClaim<TKey>
        where TUserToken : IdentityUserToken<TKey>
    {
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
        public IdentityDbContext(DbContextOptions options) : base(options) { }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        protected IdentityDbContext() { }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> of Users.
        /// </summary>
        public DbSet<TUser> Users { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> of User claims.
        /// </summary>
        public DbSet<TUserClaim> UserClaims { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> of User logins.
        /// </summary>
        public DbSet<TUserLogin> UserLogins { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> of User roles.
        /// </summary>
        public DbSet<TUserRole> UserRoles { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> of User tokens.
        /// </summary>
        public DbSet<TUserToken> UserTokens { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> of roles.
        /// </summary>
        public DbSet<TRole> Roles { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DbSet{TEntity}"/> of role claims.
        /// </summary>
        public DbSet<TRoleClaim> RoleClaims { get; set; }

        private string _version;
        /// <summary>
        /// Gets or sets the version to use during <see cref="OnModelCreating(ModelBuilder)"/>.
        /// If not set, defaults to the value in the <see cref="IdentityStoreOptions.Version"/>
        /// </summary>
        public string Version {
            get => _version ?? 
                this.GetService<IDbContextOptions>()
                .Extensions.OfType<CoreOptionsExtension>()
                .FirstOrDefault()?.ApplicationServiceProvider
                ?.GetService<IOptions<IdentityStoreOptions>>()
                ?.Value?.Version;
            set => _version = value;
        }

        /// <summary>
        /// Creates the latest identity model.
        /// </summary>
        /// <param name="builder"></param>
        protected void OnModelCreatingLatest(ModelBuilder builder)
            => OnModelCreatingV2(builder);

        /// <summary>
        /// Configures the schema needed for the identity framework version <see cref="IdentityStoreOptions.Version2_0"/>.
        /// </summary>
        /// <param name="builder">
        /// The builder being used to construct the model for this context.
        /// </param>
        protected void OnModelCreatingV2(ModelBuilder builder)
        {
            builder.Entity<TUser>(b =>
            {
                b.HasKey(u => u.Id);
                b.HasIndex(u => u.NormalizedUserName).HasName("UserNameIndex").IsUnique();
                b.HasIndex(u => u.NormalizedEmail).HasName("EmailIndex");
                b.ToTable("AspNetUsers");
                b.Property(u => u.ConcurrencyStamp).IsConcurrencyToken();

                b.Property(u => u.UserName).HasMaxLength(256);
                b.Property(u => u.NormalizedUserName).HasMaxLength(256);
                b.Property(u => u.Email).HasMaxLength(256);
                b.Property(u => u.NormalizedEmail).HasMaxLength(256);

                b.HasMany<TUserClaim>().WithOne().HasForeignKey(uc => uc.UserId).IsRequired();
                b.HasMany<TUserLogin>().WithOne().HasForeignKey(ul => ul.UserId).IsRequired();
                b.HasMany<TUserRole>().WithOne().HasForeignKey(ur => ur.UserId).IsRequired();
                b.HasMany<TUserToken>().WithOne().HasForeignKey(ut => ut.UserId).IsRequired();
            });

            builder.Entity<TRole>(b =>
            {
                b.HasKey(r => r.Id);
                b.HasIndex(r => r.NormalizedName).HasName("RoleNameIndex").IsUnique();
                b.ToTable("AspNetRoles");
                b.Property(r => r.ConcurrencyStamp).IsConcurrencyToken();

                b.Property(u => u.Name).HasMaxLength(256);
                b.Property(u => u.NormalizedName).HasMaxLength(256);

                b.HasMany<TUserRole>().WithOne().HasForeignKey(ur => ur.RoleId).IsRequired();
                b.HasMany<TRoleClaim>().WithOne().HasForeignKey(rc => rc.RoleId).IsRequired();
            });

            builder.Entity<TUserClaim>(b =>
            {
                b.HasKey(uc => uc.Id);
                b.ToTable("AspNetUserClaims");
            });

            builder.Entity<TRoleClaim>(b =>
            {
                b.HasKey(rc => rc.Id);
                b.ToTable("AspNetRoleClaims");
            });

            builder.Entity<TUserRole>(b =>
            {
                b.HasKey(r => new { r.UserId, r.RoleId });
                b.ToTable("AspNetUserRoles");
            });

            builder.Entity<TUserLogin>(b =>
            {
                b.HasKey(l => new { l.LoginProvider, l.ProviderKey });
                b.ToTable("AspNetUserLogins");
            });

            builder.Entity<TUserToken>(b =>
            {
                b.HasKey(l => new { l.UserId, l.LoginProvider, l.Name });
                b.ToTable("AspNetUserTokens");
            });
        }

        /// <summary>
        /// Configures the schema needed for the identity framework version <see cref="IdentityStoreOptions.Version1_0"/>.
        /// </summary>
        /// <param name="builder">
        /// The builder being used to construct the model for this context.
        /// </param>
        protected void OnModelCreatingV1(ModelBuilder builder)
        {

            OnModelCreatingV2(builder);

            // Ignore the additional 2.0 properties that were added
            builder.Entity<TUser>(b =>
            {
                b.Ignore(u => u.LastPasswordChangeDate);
                b.Ignore(u => u.LastSignInDate);
                b.Ignore(u => u.CreateDate);
            });
        }

        /// <summary>
        /// Configures the schema needed for the identity framework.
        /// </summary>
        /// <param name="builder">
        /// The builder being used to construct the model for this context.
        /// </param>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            if (Version == IdentityStoreOptions.Version_Latest)
            {
                OnModelCreatingLatest(builder);
            }
            else if (Version == IdentityStoreOptions.Version2_0)
            {
                OnModelCreatingV2(builder);
            }
            else // Default is always the v1 schema
            {
                OnModelCreatingV1(builder);
            }
        }
    }
}