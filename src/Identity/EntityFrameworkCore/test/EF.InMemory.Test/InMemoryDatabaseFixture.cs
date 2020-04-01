// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Data.Sqlite;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.InMemory.Test
{
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
}
