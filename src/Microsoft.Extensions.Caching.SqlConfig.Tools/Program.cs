// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Caching.SqlConfig.Tools
{
    public class Program
    {
        private string _connectionString = null;
        private string _schemaName = null;
        private string _tableName = null;

        private readonly ILogger _logger;

        public Program()
        {
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddConsole();
            _logger = loggerFactory.CreateLogger<Program>();
        }

        public static int Main(string[] args)
        {
            return new Program().Run(args);
        }

        public int Run(string[] args)
        {
            try
            {
                var description = "Creates table and indexes in Microsoft SQL Server database " +
                    "to be used for distributed caching";

                var app = new CommandLineApplication
                {
                    Name = "dotnet sql-cache",
                    FullName = "SQL Server Cache Command Line Tool",
                    Description = description,
                };
                app.HelpOption("-?|-h|--help");
                app.VersionOptionFromAssemblyAttributes(typeof(Program).GetTypeInfo().Assembly);

                app.Command("create", command =>
                {
                    command.Description = description;
                    var connectionStringArg = command.Argument(
                        "[connectionString]",
                        "The connection string to connect to the database.");
                    var schemaNameArg = command.Argument("[schemaName]", "Name of the table schema.");
                    var tableNameArg = command.Argument("[tableName]", "Name of the table to be created.");
                    command.HelpOption("-?|-h|--help");

                    command.OnExecute(() =>
                    {
                        if (string.IsNullOrEmpty(connectionStringArg.Value)
                        || string.IsNullOrEmpty(schemaNameArg.Value)
                        || string.IsNullOrEmpty(tableNameArg.Value))
                        {
                            _logger.LogWarning("Invalid input");
                            app.ShowHelp();
                            return 2;
                        }

                        _connectionString = connectionStringArg.Value;
                        _schemaName = schemaNameArg.Value;
                        _tableName = tableNameArg.Value;

                        return CreateTableAndIndexes();
                    });
                });

                // Show help information if no subcommand/option was specified.
                app.OnExecute(() =>
                {
                    app.ShowHelp();
                    return 2;
                });

                return app.Execute(args);
            }
            catch (Exception exception)
            {
                _logger.LogCritical("An error occurred. {ErrorMessage}", exception.Message);
                return 1;
            }
        }

        private int CreateTableAndIndexes()
        {
            ValidateConnectionString();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var sqlQueries = new SqlQueries(_schemaName, _tableName);
                var command = new SqlCommand(sqlQueries.TableInfo, connection);

                using (var reader = command.ExecuteReader(CommandBehavior.SingleRow))
                {
                    if (reader.Read())
                    {
                        _logger.LogWarning(
                            $"Table with schema '{_schemaName}' and name '{_tableName}' already exists. " +
                            "Provide a different table name and try again.");
                        return 1;
                    }
                }

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        command = new SqlCommand(sqlQueries.CreateTable, connection, transaction);
                        command.ExecuteNonQuery();

                        command = new SqlCommand(
                            sqlQueries.CreateNonClusteredIndexOnExpirationTime,
                            connection,
                            transaction);
                        command.ExecuteNonQuery();

                        transaction.Commit();

                        _logger.LogInformation("Table and index were created successfully.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            "An error occurred while trying to create the table and index. {ErrorMessage}",
                            ex.Message);
                        transaction.Rollback();

                        return 1;
                    }
                }
            }

            return 0;
        }

        private void ValidateConnectionString()
        {
            try
            {
                new SqlConnectionStringBuilder(_connectionString);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    $"Invalid Sql server connection string '{_connectionString}'. {ex.Message}", ex);
            }
        }
    }
}