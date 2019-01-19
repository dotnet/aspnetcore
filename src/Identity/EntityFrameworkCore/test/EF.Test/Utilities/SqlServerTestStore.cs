// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Threading;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test.Utilities
{
    public class SqlServerTestStore : IDisposable
    {
        public const int CommandTimeout = 90;

        public static string CreateConnectionString(string name)
        {
            var connStrBuilder = new SqlConnectionStringBuilder(TestEnvironment.Config["Test:SqlServer:DefaultConnectionString"])
            {
                InitialCatalog = name
            };

            return connStrBuilder.ConnectionString;
        }

        public static SqlServerTestStore CreateScratch(bool createDatabase = true)
            => new SqlServerTestStore(GetScratchDbName()).CreateTransient(createDatabase);

        private SqlConnection _connection;
        private readonly string _name;
        private bool _deleteDatabase;

        private SqlServerTestStore(string name)
        {
            _name = name;
        }

        private static string GetScratchDbName()
        {
            string name;
            do
            {
                name = "Scratch_" + Guid.NewGuid();
            } while (DatabaseExists(name)
                     || DatabaseFilesExist(name));

            return name;
        }

        private static void WaitForExists(SqlConnection connection)
        {
            var retryCount = 0;
            while (true)
            {
                try
                {
                    connection.Open();

                    connection.Close();

                    return;
                }
                catch (SqlException e)
                {
                    if (++retryCount >= 30
                        || (e.Number != 233 && e.Number != -2 && e.Number != 4060))
                    {
                        throw;
                    }

                    SqlConnection.ClearPool(connection);

                    Thread.Sleep(100);
                }
            }
        }

        private SqlServerTestStore CreateTransient(bool createDatabase)
        {
            _connection = new SqlConnection(CreateConnectionString(_name));

            if (createDatabase)
            {
                using (var master = new SqlConnection(CreateConnectionString("master")))
                {
                    master.Open();
                    using (var command = master.CreateCommand())
                    {
                        command.CommandTimeout = CommandTimeout;
                        command.CommandText = $"{Environment.NewLine}CREATE DATABASE [{_name}]";

                        command.ExecuteNonQuery();

                        WaitForExists(_connection);
                    }
                }
                _connection.Open();
            }

            _deleteDatabase = true;
            return this;
        }

        private static bool DatabaseExists(string name)
        {
            using (var master = new SqlConnection(CreateConnectionString("master")))
            {
                master.Open();

                using (var command = master.CreateCommand())
                {
                    command.CommandTimeout = CommandTimeout;
                    command.CommandText = $@"SELECT COUNT(*) FROM sys.databases WHERE name = N'{name}'";

                    return (int) command.ExecuteScalar() > 0;
                }
            }
        }

        private static bool DatabaseFilesExist(string name)
        {
            var userFolder = Environment.GetEnvironmentVariable("USERPROFILE") ??
                             Environment.GetEnvironmentVariable("HOME");
            return userFolder != null
                   && (File.Exists(Path.Combine(userFolder, name + ".mdf"))
                       || File.Exists(Path.Combine(userFolder, name + "_log.ldf")));
        }

        private void DeleteDatabase(string name)
        {
            using (var master = new SqlConnection(CreateConnectionString("master")))
            {
                master.Open();

                using (var command = master.CreateCommand())
                {
                    command.CommandTimeout = CommandTimeout;
                        // Query will take a few seconds if (and only if) there are active connections

                    // SET SINGLE_USER will close any open connections that would prevent the drop
                    command.CommandText
                        = string.Format(@"IF EXISTS (SELECT * FROM sys.databases WHERE name = N'{0}')
                                          BEGIN
                                              ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                                              DROP DATABASE [{0}];
                                          END", name);

                    command.ExecuteNonQuery();
                }
            }
        }

        public DbConnection Connection => _connection;

        public void Dispose()
        {
            _connection.Dispose();

            if (_deleteDatabase)
            {
                DeleteDatabase(_name);
            }
        }
    }
}
