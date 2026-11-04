// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Data;
using System.IO;
using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.Extensions.Caching.SqlConfig.Tools;

public class Program
{
    private string _connectionString;
    private string _schemaName;
    private string _tableName;
    private string _outputPath;
    private bool _idempotent;

    private readonly IConsole _console;

    public Program(IConsole console)
    {
        Ensure.NotNull(console, nameof(console));

        _console = console;
    }

    public static int Main(string[] args)
    {
        return new Program(PhysicalConsole.Singleton).Run(args);
    }

    public int Run(string[] args)
    {
        DebugHelper.HandleDebugSwitch(ref args);

        try
        {
            var app = new CommandLineApplication
            {
                Name = "dotnet sql-cache",
                FullName = "SQL Server Cache Command Line Tool",
                Description =
                    "Creates table and indexes in Microsoft SQL Server database to be used for distributed caching",
            };

            app.HelpOption();
            app.VersionOptionFromAssemblyAttributes(typeof(Program).Assembly);
            var verbose = app.VerboseOption();

            app.Command("create", command =>
            {
                command.Description = "Adds table and indexes to the database.";

                var connectionStringArg = command.Argument(
                    "[connectionString]", "The connection string to connect to the database.");

                var schemaNameArg = command.Argument(
                    "[schemaName]", "Name of the table schema.");

                var tableNameArg = command.Argument(
                    "[tableName]", "Name of the table to be created.");

                command.HelpOption();

                command.OnExecute(() =>
                {
                    var reporter = CreateReporter(verbose.HasValue());
                    if (string.IsNullOrEmpty(connectionStringArg.Value)
                        || string.IsNullOrEmpty(schemaNameArg.Value)
                        || string.IsNullOrEmpty(tableNameArg.Value))
                    {
                        reporter.Error("Invalid input");
                        command.ShowHelp();
                        return 2;
                    }

                    _connectionString = connectionStringArg.Value;
                    _schemaName = schemaNameArg.Value;
                    _tableName = tableNameArg.Value;

                    return CreateTableAndIndexes(reporter);
                });
            });

            app.Command("script", command =>
            {
                command.Description = "Generates a SQL script for the table and indexes.";

                var schemaNameArg = command.Argument(
                    "[schemaName]", "Name of the table schema.");

                var tableNameArg = command.Argument(
                    "[tableName]", "Name of the table to be created.");

                var outputOption = command.Option(
                    "-o|--output",
                    "The file to write the result to.",
                    CommandOptionType.SingleValue);

                var idempotentOption = command.Option(
                    "-i|--idempotent",
                    "Generates a script that can be used on a database that already has the table.",
                    CommandOptionType.NoValue);

                command.HelpOption();

                command.OnExecute(() =>
                {
                    var reporter = CreateReporter(verbose.HasValue());
                    if (string.IsNullOrEmpty(schemaNameArg.Value)
                        || string.IsNullOrEmpty(tableNameArg.Value))
                    {
                        reporter.Error("Invalid input");
                        command.ShowHelp();
                        return 2;
                    }

                    _schemaName = schemaNameArg.Value;
                    _tableName = tableNameArg.Value;
                    _idempotent = idempotentOption.HasValue();
                    if (outputOption.HasValue())
                    {
                        _outputPath = outputOption.Value();
                    }

                    return ScriptTableAndIndexes(reporter);
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
            CreateReporter(verbose: false).Error($"An error occurred. {exception.Message}");
            return 1;
        }
    }

    private IReporter CreateReporter(bool verbose)
        => new ConsoleReporter(_console, verbose, quiet: false);

    private SqlQueries CreateSqlQueries()
        => new SqlQueries(_schemaName, _tableName);

    private int ScriptTableAndIndexes(IReporter reporter)
    {
        Action<string> writer = reporter.Output;
        StreamWriter streamWriter = default;

        try
        {
            if (_outputPath is not null)
            {
                streamWriter = new StreamWriter(_outputPath);
                writer = streamWriter.WriteLine;
            }

            var sqlQueries = CreateSqlQueries();

            if (_idempotent)
            {
                writer("IF NOT EXISTS (");
                writer("\t" + sqlQueries.TableInfo);
                writer(")");
                writer("BEGIN");
            }

            var prefix = _idempotent ? "\t" : "";
            writer(prefix + sqlQueries.CreateTable);
            writer(prefix + sqlQueries.CreateNonClusteredIndexOnExpirationTime);

            if (_idempotent)
            {
                writer("END");
            }

            return 0;
        }
        catch (Exception ex)
        {
            reporter.Error(
                $"An error occurred while trying to script the table and index. {ex.Message}");

            return 1;
        }
        finally
        {
            streamWriter?.Dispose();
        }
    }

    private int CreateTableAndIndexes(IReporter reporter)
    {
        ValidateConnectionString();

        using (var connection = new SqlConnection(_connectionString))
        {
            connection.Open();

            var sqlQueries = CreateSqlQueries();
            var command = new SqlCommand(sqlQueries.TableInfo, connection);

            using (var reader = command.ExecuteReader(CommandBehavior.SingleRow))
            {
                if (reader.Read())
                {
                    reporter.Warn(
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

                    reporter.Verbose($"Executing {command.CommandText}");
                    command.ExecuteNonQuery();

                    command = new SqlCommand(
                        sqlQueries.CreateNonClusteredIndexOnExpirationTime,
                        connection,
                        transaction);

                    reporter.Verbose($"Executing {command.CommandText}");
                    command.ExecuteNonQuery();

                    transaction.Commit();

                    reporter.Output("Table and index were created successfully.");
                }
                catch (Exception ex)
                {
                    reporter.Error(
                        $"An error occurred while trying to create the table and index. {ex.Message}");
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
                $"Invalid SQL Server connection string '{_connectionString}'. {ex.Message}", ex);
        }
    }
}
