// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test
{
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
            => new DbContext(new DbContextOptionsBuilder().UseSqlite(_connection).Options);

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
}
