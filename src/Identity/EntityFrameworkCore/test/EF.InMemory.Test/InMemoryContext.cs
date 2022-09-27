// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.InMemory.Test;

public class InMemoryContext :
    InMemoryContext<IdentityUser, IdentityRole, string>
{
    private InMemoryContext(DbConnection connection) : base(connection)
    { }

    public static new InMemoryContext Create(DbConnection connection)
        => Initialize(new InMemoryContext(connection));

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

    private InMemoryContext(DbConnection connection)
    {
        _connection = connection;
    }

    public static InMemoryContext<TUser> Create(DbConnection connection)
        => InMemoryContext.Initialize(new InMemoryContext<TUser>(connection));

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite(_connection);
}

public class InMemoryContext<TUser, TRole, TKey> : IdentityDbContext<TUser, TRole, TKey>
    where TUser : IdentityUser<TKey>
    where TRole : IdentityRole<TKey>
    where TKey : IEquatable<TKey>
{
    private readonly DbConnection _connection;

    protected InMemoryContext(DbConnection connection)
    {
        _connection = connection;
    }

    public static InMemoryContext<TUser, TRole, TKey> Create(DbConnection connection)
        => InMemoryContext.Initialize(new InMemoryContext<TUser, TRole, TKey>(connection));

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite(_connection);
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
