// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Data.Sqlite;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.InMemory.Test;

public class InMemoryDatabaseFixture : IDisposable
{
    private readonly SqliteConnection _connection = new SqliteConnection($"DataSource=:memory:");

    public InMemoryDatabaseFixture()
    {
        _connection.Open();
    }

    public SqliteConnection Connection => _connection;

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}
