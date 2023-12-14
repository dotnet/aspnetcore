// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test;

public static class DbUtil
{
    public static IServiceCollection ConfigureDbServices<TContext>(
        DbConnection connection,
        IServiceCollection services = null) where TContext : DbContext
    {
        if (services == null)
        {
            services = new ServiceCollection();
        }

        services.AddHttpContextAccessor();

        services.AddDbContext<TContext>(options =>
        {
            options
                .ConfigureWarnings(b => b.Log(CoreEventId.ManyServiceProvidersCreatedWarning))
                .UseSqlite(connection);
        });

        return services;
    }

    public static TContext Create<TContext>(DbConnection connection, IServiceCollection services = null) where TContext : DbContext
    {
        var serviceProvider = ConfigureDbServices<TContext>(connection, services).BuildServiceProvider();
        return serviceProvider.GetRequiredService<TContext>();
    }

    public static bool VerifyMaxLength(DbContext context, string table, int maxLength, params string[] columns)
    {
        var count = 0;

        foreach (var property in context.Model.GetEntityTypes().Single(e => e.GetTableName() == table).GetProperties())
        {
            if (!columns.Contains(property.GetColumnName(StoreObjectIdentifier.Table(table, property.DeclaringType.GetSchema()))))
            {
                continue;
            }
            if (property.GetMaxLength() != maxLength)
            {
                return false;
            }
            count++;
        }

        return count == columns.Length;
    }

    public static bool VerifyColumns(SqliteConnection conn, string table, params string[] columns)
    {
        var count = 0;
        using (var command = new SqliteCommand("SELECT \"name\" FROM pragma_table_info(@table)", conn))
        {
            command.Parameters.Add(new SqliteParameter("table", table));
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    count++;
                    if (!columns.Contains(reader.GetString(0)))
                    {
                        return false;
                    }
                }
                return count == columns.Length;
            }
        }
    }

    public static void VerifyIndex(SqliteConnection conn, string table, string index, bool isUnique = false)
    {
        using (var command =
            new SqliteCommand(
                "SELECT COUNT(*) FROM pragma_index_list(@table) WHERE \"name\" = @index AND \"unique\" = @unique", conn))
        {
            command.Parameters.Add(new SqliteParameter("index", index));
            command.Parameters.Add(new SqliteParameter("table", table));
            command.Parameters.Add(new SqliteParameter("unique", isUnique));
            using (var reader = command.ExecuteReader())
            {
                Assert.True(reader.Read());
                Assert.True(reader.GetInt32(0) > 0);
            }
        }
    }
}
