// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test;

public class ScratchDatabaseFixture : IDisposable
{
    private readonly SqliteConnection _connection;

    public ScratchDatabaseFixture()
    {
        _connection = new SqliteConnection($"DataSource=D{Guid.NewGuid()}.db");

        using (var context = CreateEmptyContext())
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
        }
    }

    private DbContext CreateEmptyContext()
    {
        var services = new ServiceCollection();
        services.Configure<IdentityOptions>(options => options.Stores.SchemaVersion = IdentitySchemaVersions.Version3);
        return new DbContext(new DbContextOptionsBuilder()
            .UseSqlite(_connection)
            .UseApplicationServiceProvider(services.BuildServiceProvider())
            .Options);
    }

    public DbConnection Connection => _connection;

    public void Dispose()
    {
        using (var context = CreateEmptyContext())
        {
            context.Database.EnsureDeleted();
        }

        _connection.Dispose();
    }
}
