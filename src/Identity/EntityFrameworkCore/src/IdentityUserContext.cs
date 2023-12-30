// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore;

/// <summary>
/// Base class for the Entity Framework database context used for identity.
/// </summary>
/// <typeparam name="TUser">The type of the user objects.</typeparam>
public class IdentityUserContext<TUser> : IdentityUserContext<TUser, string> where TUser : IdentityUser
{
    /// <summary>
    /// Initializes a new instance of <see cref="IdentityUserContext{TUser}"/>.
    /// </summary>
    /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
    public IdentityUserContext(DbContextOptions options) : base(options) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentityUserContext{TUser}" /> class.
    /// </summary>
    protected IdentityUserContext() { }
}

/// <summary>
/// Base class for the Entity Framework database context used for identity.
/// </summary>
/// <typeparam name="TUser">The type of user objects.</typeparam>
/// <typeparam name="TKey">The type of the primary key for users and roles.</typeparam>
public class IdentityUserContext<TUser, TKey> : IdentityUserContext<TUser, TKey, IdentityUserClaim<TKey>, IdentityUserLogin<TKey>, IdentityUserToken<TKey>>
    where TUser : IdentityUser<TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// Initializes a new instance of the db context.
    /// </summary>
    /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
    public IdentityUserContext(DbContextOptions options) : base(options) { }

    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    protected IdentityUserContext() { }
}

/// <summary>
/// Base class for the Entity Framework database context used for identity.
/// </summary>
/// <typeparam name="TUser">The type of user objects.</typeparam>
/// <typeparam name="TKey">The type of the primary key for users and roles.</typeparam>
/// <typeparam name="TUserClaim">The type of the user claim object.</typeparam>
/// <typeparam name="TUserLogin">The type of the user login object.</typeparam>
/// <typeparam name="TUserToken">The type of the user token object.</typeparam>
public abstract class IdentityUserContext<TUser, TKey, TUserClaim, TUserLogin, TUserToken> : DbContext
    where TUser : IdentityUser<TKey>
    where TKey : IEquatable<TKey>
    where TUserClaim : IdentityUserClaim<TKey>
    where TUserLogin : IdentityUserLogin<TKey>
    where TUserToken : IdentityUserToken<TKey>
{
    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
    public IdentityUserContext(DbContextOptions options) : base(options) { }

    /// <summary>
    /// Initializes a new instance of the class.
    /// </summary>
    protected IdentityUserContext() { }

    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> of Users.
    /// </summary>
    public virtual DbSet<TUser> Users { get; set; } = default!;

    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> of User claims.
    /// </summary>
    public virtual DbSet<TUserClaim> UserClaims { get; set; } = default!;

    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> of User logins.
    /// </summary>
    public virtual DbSet<TUserLogin> UserLogins { get; set; } = default!;

    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> of User tokens.
    /// </summary>
    public virtual DbSet<TUserToken> UserTokens { get; set; } = default!;

    /// <summary>
    /// Gets the schema version used for versioning.
    /// </summary>
    protected virtual Version SchemaVersion { get => GetStoreOptions()?.SchemaVersion ?? IdentitySchemaVersions.Version1; }

    private StoreOptions? GetStoreOptions() => this.GetService<IDbContextOptions>()
                        .Extensions.OfType<CoreOptionsExtension>()
                        .FirstOrDefault()?.ApplicationServiceProvider
                        ?.GetService<IOptions<IdentityOptions>>()
                        ?.Value?.Stores;

    private sealed class PersonalDataConverter : ValueConverter<string, string>
    {
        public PersonalDataConverter(IPersonalDataProtector protector) : base(s => protector.Protect(s), s => protector.Unprotect(s), default)
        { }
    }

    /// <summary>
    /// Configures the schema needed for the identity framework.
    /// </summary>
    /// <param name="builder">
    /// The builder being used to construct the model for this context.
    /// </param>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        var version = GetStoreOptions()?.SchemaVersion ?? IdentitySchemaVersions.Version1;
        OnModelCreatingVersion(builder, version);
    }

    /// <summary>
    /// Configures the schema needed for the identity framework for a specific schema version.
    /// </summary>
    /// <param name="builder">
    /// The builder being used to construct the model for this context.
    /// </param>
    /// <param name="schemaVersion">The schema version.</param>
    internal virtual void OnModelCreatingVersion(ModelBuilder builder, Version schemaVersion)
    {
        if (schemaVersion >= IdentitySchemaVersions.Version2)
        {
            OnModelCreatingVersion2(builder);
        }
        else
        {
            OnModelCreatingVersion1(builder);
        }
    }

    /// <summary>
    /// Configures the schema needed for the identity framework for schema version 2.0
    /// </summary>
    /// <param name="builder">
    /// The builder being used to construct the model for this context.
    /// </param>
    internal virtual void OnModelCreatingVersion2(ModelBuilder builder)
    {
        // Differences from Version 1:
        // - maxKeyLength defaults to 128
        // - PhoneNumber has a 256 max length

        var storeOptions = GetStoreOptions();
        var maxKeyLength = storeOptions?.MaxLengthForKeys ?? 0;
        if (maxKeyLength == 0)
        {
            maxKeyLength = 128;
        }
        var encryptPersonalData = storeOptions?.ProtectPersonalData ?? false;
        PersonalDataConverter? converter = null;

        builder.Entity<TUser>(b =>
        {
            b.HasKey(u => u.Id);
            b.HasIndex(u => u.NormalizedUserName).HasDatabaseName("UserNameIndex").IsUnique();
            b.HasIndex(u => u.NormalizedEmail).HasDatabaseName("EmailIndex");
            b.ToTable("AspNetUsers");
            b.Property(u => u.ConcurrencyStamp).IsConcurrencyToken();

            b.Property(u => u.UserName).HasMaxLength(256);
            b.Property(u => u.NormalizedUserName).HasMaxLength(256);
            b.Property(u => u.Email).HasMaxLength(256);
            b.Property(u => u.NormalizedEmail).HasMaxLength(256);
            b.Property(u => u.PhoneNumber).HasMaxLength(256);

            if (encryptPersonalData)
            {
                converter = new PersonalDataConverter(this.GetService<IPersonalDataProtector>());
                var personalDataProps = typeof(TUser).GetProperties().Where(
                                prop => Attribute.IsDefined(prop, typeof(ProtectedPersonalDataAttribute)));
                foreach (var p in personalDataProps)
                {
                    if (p.PropertyType != typeof(string))
                    {
                        throw new InvalidOperationException(Resources.CanOnlyProtectStrings);
                    }
                    b.Property(typeof(string), p.Name).HasConversion(converter);
                }
            }

            b.HasMany<TUserClaim>().WithOne().HasForeignKey(uc => uc.UserId).IsRequired();
            b.HasMany<TUserLogin>().WithOne().HasForeignKey(ul => ul.UserId).IsRequired();
            b.HasMany<TUserToken>().WithOne().HasForeignKey(ut => ut.UserId).IsRequired();
        });

        builder.Entity<TUserClaim>(b =>
        {
            b.HasKey(uc => uc.Id);
            b.ToTable("AspNetUserClaims");
        });

        builder.Entity<TUserLogin>(b =>
        {
            b.HasKey(l => new { l.LoginProvider, l.ProviderKey });

            if (maxKeyLength > 0)
            {
                b.Property(l => l.LoginProvider).HasMaxLength(maxKeyLength);
                b.Property(l => l.ProviderKey).HasMaxLength(maxKeyLength);
            }

            b.ToTable("AspNetUserLogins");
        });

        builder.Entity<TUserToken>(b =>
        {
            b.HasKey(t => new { t.UserId, t.LoginProvider, t.Name });

            if (maxKeyLength > 0)
            {
                b.Property(t => t.LoginProvider).HasMaxLength(maxKeyLength);
                b.Property(t => t.Name).HasMaxLength(maxKeyLength);
            }

            if (encryptPersonalData)
            {
                var tokenProps = typeof(TUserToken).GetProperties().Where(
                                prop => Attribute.IsDefined(prop, typeof(ProtectedPersonalDataAttribute)));
                foreach (var p in tokenProps)
                {
                    if (p.PropertyType != typeof(string))
                    {
                        throw new InvalidOperationException(Resources.CanOnlyProtectStrings);
                    }
                    b.Property(typeof(string), p.Name).HasConversion(converter);
                }
            }

            b.ToTable("AspNetUserTokens");
        });
    }

    /// <summary>
    /// Configures the schema needed for the identity framework for schema version 1.0
    /// </summary>
    /// <param name="builder">
    /// The builder being used to construct the model for this context.
    /// </param>
    internal virtual void OnModelCreatingVersion1(ModelBuilder builder)
    {
        var storeOptions = GetStoreOptions();
        var maxKeyLength = storeOptions?.MaxLengthForKeys ?? 0;
        var encryptPersonalData = storeOptions?.ProtectPersonalData ?? false;
        PersonalDataConverter? converter = null;

        builder.Entity<TUser>(b =>
        {
            b.HasKey(u => u.Id);
            b.HasIndex(u => u.NormalizedUserName).HasDatabaseName("UserNameIndex").IsUnique();
            b.HasIndex(u => u.NormalizedEmail).HasDatabaseName("EmailIndex");
            b.ToTable("AspNetUsers");
            b.Property(u => u.ConcurrencyStamp).IsConcurrencyToken();

            b.Property(u => u.UserName).HasMaxLength(256);
            b.Property(u => u.NormalizedUserName).HasMaxLength(256);
            b.Property(u => u.Email).HasMaxLength(256);
            b.Property(u => u.NormalizedEmail).HasMaxLength(256);

            if (encryptPersonalData)
            {
                converter = new PersonalDataConverter(this.GetService<IPersonalDataProtector>());
                var personalDataProps = typeof(TUser).GetProperties().Where(
                                prop => Attribute.IsDefined(prop, typeof(ProtectedPersonalDataAttribute)));
                foreach (var p in personalDataProps)
                {
                    if (p.PropertyType != typeof(string))
                    {
                        throw new InvalidOperationException(Resources.CanOnlyProtectStrings);
                    }
                    b.Property(typeof(string), p.Name).HasConversion(converter);
                }
            }

            b.HasMany<TUserClaim>().WithOne().HasForeignKey(uc => uc.UserId).IsRequired();
            b.HasMany<TUserLogin>().WithOne().HasForeignKey(ul => ul.UserId).IsRequired();
            b.HasMany<TUserToken>().WithOne().HasForeignKey(ut => ut.UserId).IsRequired();
        });

        builder.Entity<TUserClaim>(b =>
        {
            b.HasKey(uc => uc.Id);
            b.ToTable("AspNetUserClaims");
        });

        builder.Entity<TUserLogin>(b =>
        {
            b.HasKey(l => new { l.LoginProvider, l.ProviderKey });

            if (maxKeyLength > 0)
            {
                b.Property(l => l.LoginProvider).HasMaxLength(maxKeyLength);
                b.Property(l => l.ProviderKey).HasMaxLength(maxKeyLength);
            }

            b.ToTable("AspNetUserLogins");
        });

        builder.Entity<TUserToken>(b =>
        {
            b.HasKey(t => new { t.UserId, t.LoginProvider, t.Name });

            if (maxKeyLength > 0)
            {
                b.Property(t => t.LoginProvider).HasMaxLength(maxKeyLength);
                b.Property(t => t.Name).HasMaxLength(maxKeyLength);
            }

            if (encryptPersonalData)
            {
                var tokenProps = typeof(TUserToken).GetProperties().Where(
                                prop => Attribute.IsDefined(prop, typeof(ProtectedPersonalDataAttribute)));
                foreach (var p in tokenProps)
                {
                    if (p.PropertyType != typeof(string))
                    {
                        throw new InvalidOperationException(Resources.CanOnlyProtectStrings);
                    }
                    b.Property(typeof(string), p.Name).HasConversion(converter);
                }
            }

            b.ToTable("AspNetUserTokens");
        });
    }
}
