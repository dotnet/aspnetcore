// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;

public class SqlServerTests : DistributedCacheTests
{
    public SqlServerTests(ITestOutputHelper log) : base(log) { }

    protected override bool CustomClockSupported => true;

    protected override async ValueTask ConfigureAsync(IServiceCollection services)
    {
        // create a local DB named CacheBench, then
        // dotnet tool install --global dotnet-sql-cache
        // dotnet sql-cache create "Data Source=.;Initial Catalog=CacheBench;Integrated Security=True;Trust Server Certificate=True" dbo BenchmarkCache

        const string ConnectionString = "Data Source=.;Initial Catalog=CacheBench;Integrated Security=True;Trust Server Certificate=True";

        try
        {
            using var conn = new SqlConnection(ConnectionString);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "truncate table dbo.BenchmarkCache";
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            // if that worked: we should be fine
            services.AddDistributedSqlServerCache(options =>
            {
                options.SchemaName = "dbo";
                options.TableName = "BenchmarkCache";
                options.ConnectionString = ConnectionString;
                options.SystemClock = Clock;
            });
        }
        catch (Exception ex)
        {
            Log.WriteLine(ex.Message);
        }
    }
}
