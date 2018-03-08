// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Identity.EntityFrameworkCore.Test
{
    public static class DbUtil
    {
        public static IServiceCollection ConfigureDbServices<TContext>(string connectionString, IServiceCollection services = null) where TContext : DbContext
        {
            if (services == null)
            {
                services = new ServiceCollection();
            }
            services.AddHttpContextAccessor();
            services.AddDbContext<TContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });
            return services;
        }

        public static TContext Create<TContext>(string connectionString, IServiceCollection services = null) where TContext : DbContext
        {
            var serviceProvider = ConfigureDbServices<TContext>(connectionString, services).BuildServiceProvider();
            return serviceProvider.GetRequiredService<TContext>();
        }

        public static bool VerifyMaxLength(SqlConnection conn, string table, int maxLength, params string[] columns)
        {
            var count = 0;
            using (
                var command =
                    new SqlCommand("SELECT COLUMN_NAME, CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS where TABLE_NAME=@Table", conn))
            {
                command.Parameters.Add(new SqlParameter("Table", table));
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!columns.Contains(reader.GetString(0)))
                        {
                            continue;
                        }
                        if (reader.GetInt32(1) != maxLength)
                        {
                            return false;
                        }
                        count++;
                    }
                    return count == columns.Length;
                }
            }
        }

        public static bool VerifyColumns(SqlConnection conn, string table, params string[] columns)
        {
            var count = 0;
            using (
                var command =
                    new SqlCommand("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS where TABLE_NAME=@Table", conn))
            {
                command.Parameters.Add(new SqlParameter("Table", table));
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

        public static void VerifyIndex(SqlConnection conn, string table, string index, bool isUnique = false)
        {
            using (
                var command =
                    new SqlCommand(
                        "SELECT COUNT(*) FROM sys.indexes where NAME=@Index AND object_id = OBJECT_ID(@Table) AND is_unique = @Unique", conn))
            {
                command.Parameters.Add(new SqlParameter("Index", index));
                command.Parameters.Add(new SqlParameter("Table", table));
                command.Parameters.Add(new SqlParameter("Unique", isUnique));
                using (var reader = command.ExecuteReader())
                {
                    Assert.True(reader.Read());
                    Assert.True(reader.GetInt32(0) > 0);
                }
            }
        }

    }
}