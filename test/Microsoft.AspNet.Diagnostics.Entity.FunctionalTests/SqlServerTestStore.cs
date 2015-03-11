// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Data.SqlClient;
using System.Threading;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Infrastructure;

namespace Microsoft.AspNet.Diagnostics.Entity.Tests
{
    public class SqlServerTestStore : IDisposable
    {
        private static int _scratchCount;

        public static SqlServerTestStore CreateScratch()
        {
            var name = "Microsoft.AspNet.Diagnostics.Entity.FunctionalTests.Scratch_" + Interlocked.Increment(ref _scratchCount);
            var db = new SqlServerTestStore(name);
            db.EnsureDeleted();
            return db;
        }

        private readonly string _connectionString;

        private SqlServerTestStore(string name)
        {
            _connectionString = new SqlConnectionStringBuilder
            {
                DataSource = @"(localdb)\MSSQLLocalDB",
                InitialCatalog = name,
                IntegratedSecurity = true,
                ConnectTimeout = 30
            }.ConnectionString; ;
        }

        public string ConnectionString
        {
            get { return _connectionString; }
        }

        private void EnsureDeleted()
        {
            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseSqlServer(_connectionString);

            using (var db = new DbContext(optionsBuilder.Options))
            {
                db.Database.EnsureDeleted();
            }
        }

        public virtual void Dispose()
        {
            EnsureDeleted();
        }
    }
}