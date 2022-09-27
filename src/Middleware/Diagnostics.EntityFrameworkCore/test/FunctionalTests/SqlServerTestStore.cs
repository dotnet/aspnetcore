// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.Tests;

public class SqlTestStore : IDisposable
{
    public static SqlTestStore CreateScratch() => new SqlTestStore($"D{Guid.NewGuid()}");

    private SqlTestStore(string name)
    {
        ConnectionString = $"Data Source = {name}.db";
    }

    public string ConnectionString { get; }

    private void EnsureDeleted()
    {
        using (var db = new DbContext(new DbContextOptionsBuilder().UseSqlite(ConnectionString).Options))
        {
            db.Database.EnsureDeleted();
        }
    }

    public virtual void Dispose() => EnsureDeleted();
}
