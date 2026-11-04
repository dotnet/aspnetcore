// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.InMemory.Test;

public class InMemoryContext :
    InMemoryContext<IdentityUser, IdentityRole, string>
{
    private InMemoryContext(DbConnection connection, IServiceProvider serviceProvider) : base(connection, serviceProvider)
    { }

    public static new InMemoryContext Create(DbConnection connection, IServiceCollection services = null)
    {
        services = ConfigureDbServices(services);
        return Initialize(new InMemoryContext(connection, services.BuildServiceProvider()));
    }

    public static IServiceCollection ConfigureDbServices(IServiceCollection services = null)
    {
        services ??= new ServiceCollection();
        services.Configure<IdentityOptions>(options =>
        {
            options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
        });
        return services;
    }

    public static TContext Initialize<TContext>(TContext context) where TContext : DbContext
    {
        context.Database.EnsureCreated();

        return context;
    }
}

public class InMemoryContext<TUser> :
    IdentityUserContext<TUser, string>
    where TUser : IdentityUser
{
    private readonly DbConnection _connection;
    private readonly IServiceProvider _serviceProvider;

    private InMemoryContext(DbConnection connection, IServiceProvider serviceProvider)
    {
        _connection = connection;
        _serviceProvider = serviceProvider;
    }

    public static InMemoryContext<TUser> Create(DbConnection connection, IServiceCollection services = null)
    {
        services = InMemoryContext.ConfigureDbServices(services);
        return InMemoryContext.Initialize(new InMemoryContext<TUser>(connection, services.BuildServiceProvider()));
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(_connection);
        optionsBuilder.UseApplicationServiceProvider(_serviceProvider);
    }
}

public class InMemoryContext<TUser, TRole, TKey> : IdentityDbContext<TUser, TRole, TKey>
    where TUser : IdentityUser<TKey>
    where TRole : IdentityRole<TKey>
    where TKey : IEquatable<TKey>
{
    private readonly DbConnection _connection;
    private readonly IServiceProvider _serviceProvider;

    protected InMemoryContext(DbConnection connection, IServiceProvider serviceProvider)
    {
        _connection = connection;
        _serviceProvider = serviceProvider;
    }

    public static InMemoryContext<TUser, TRole, TKey> Create(DbConnection connection, IServiceCollection services = null)
    {
        services = InMemoryContext.ConfigureDbServices(services);
        return InMemoryContext.Initialize(new InMemoryContext<TUser, TRole, TKey>(connection, services.BuildServiceProvider()));
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(_connection);
        optionsBuilder.UseApplicationServiceProvider(_serviceProvider);
    }
}

public abstract class InMemoryContext<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken> :
        IdentityDbContext<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken>
    where TUser : IdentityUser<TKey>
    where TRole : IdentityRole<TKey>
    where TKey : IEquatable<TKey>
    where TUserClaim : IdentityUserClaim<TKey>
    where TUserRole : IdentityUserRole<TKey>
    where TUserLogin : IdentityUserLogin<TKey>
    where TRoleClaim : IdentityRoleClaim<TKey>
    where TUserToken : IdentityUserToken<TKey>
{
    protected InMemoryContext(DbContextOptions options)
        : base(options)
    {
    }
}
