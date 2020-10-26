// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore.Tests
{
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
}
