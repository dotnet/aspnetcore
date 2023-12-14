// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore;

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
public abstract class IdentityDbContext<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken> : IdentityUserContext<TUser, TKey, TUserClaim, TUserLogin, TUserToken>
    where TUser : IdentityUser<TKey>
    where TRole : IdentityRole<TKey>
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
    /// Gets or sets the <see cref="DbSet{TEntity}"/> of User roles.
    /// </summary>
    public virtual DbSet<TUserRole> UserRoles { get; set; } = default!;

    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> of roles.
    /// </summary>
    public virtual DbSet<TRole> Roles { get; set; } = default!;

    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> of role claims.
    /// </summary>
    public virtual DbSet<TRoleClaim> RoleClaims { get; set; } = default!;

    /// <summary>
    /// Configures the schema needed for the identity framework.
    /// </summary>
    /// <param name="builder">
    /// The builder being used to construct the model for this context.
    /// </param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }

    /// <summary>
    /// Configures the schema needed for the identity framework for schema version 2.0
    /// </summary>
    /// <param name="builder">
    /// The builder being used to construct the model for this context.
    /// </param>
    internal override void OnModelCreatingVersion2(ModelBuilder builder)
    {
        base.OnModelCreatingVersion2(builder);

        // Current no differences between Version 2 and Version 1
        builder.Entity<TUser>(b =>
        {
            b.HasMany<TUserRole>().WithOne().HasForeignKey(ur => ur.UserId).IsRequired();
        });

        builder.Entity<TRole>(b =>
        {
            b.HasKey(r => r.Id);
            b.HasIndex(r => r.NormalizedName).HasDatabaseName("RoleNameIndex").IsUnique();
            b.ToTable("AspNetRoles");
            b.Property(r => r.ConcurrencyStamp).IsConcurrencyToken();

            b.Property(u => u.Name).HasMaxLength(256);
            b.Property(u => u.NormalizedName).HasMaxLength(256);

            b.HasMany<TUserRole>().WithOne().HasForeignKey(ur => ur.RoleId).IsRequired();
            b.HasMany<TRoleClaim>().WithOne().HasForeignKey(rc => rc.RoleId).IsRequired();
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
    }

    /// <summary>
    /// Configures the schema needed for the identity framework for schema version 1.0
    /// </summary>
    /// <param name="builder">
    /// The builder being used to construct the model for this context.
    /// </param>
    internal override void OnModelCreatingVersion1(ModelBuilder builder)
    {
        base.OnModelCreatingVersion1(builder);
        builder.Entity<TUser>(b =>
        {
            b.HasMany<TUserRole>().WithOne().HasForeignKey(ur => ur.UserId).IsRequired();
        });

        builder.Entity<TRole>(b =>
        {
            b.HasKey(r => r.Id);
            b.HasIndex(r => r.NormalizedName).HasDatabaseName("RoleNameIndex").IsUnique();
            b.ToTable("AspNetRoles");
            b.Property(r => r.ConcurrencyStamp).IsConcurrencyToken();

            b.Property(u => u.Name).HasMaxLength(256);
            b.Property(u => u.NormalizedName).HasMaxLength(256);

            b.HasMany<TUserRole>().WithOne().HasForeignKey(ur => ur.RoleId).IsRequired();
            b.HasMany<TRoleClaim>().WithOne().HasForeignKey(rc => rc.RoleId).IsRequired();
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
    }
}
