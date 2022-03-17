// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthChecksSample;

public abstract class DbConnectionHealthCheck : IHealthCheck
{
    protected DbConnectionHealthCheck(string connectionString)
        : this(connectionString, testQuery: null)
    {
    }

    protected DbConnectionHealthCheck(string connectionString, string testQuery)
    {
        ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        TestQuery = testQuery;
    }

    protected string ConnectionString { get; }

    // This sample supports specifying a query to run as a boolean test of whether the database
    // is responding. It is important to choose a query that will return quickly or you risk
    // overloading the database.
    //
    // In most cases this is not necessary, but if you find it necessary, choose a simple query such as 'SELECT 1'.
    protected string TestQuery { get; }

    protected abstract DbConnection CreateConnection(string connectionString);

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default(CancellationToken))
    {
        using (var connection = CreateConnection(ConnectionString))
        {
            try
            {
                await connection.OpenAsync(cancellationToken);

                if (TestQuery != null)
                {
                    var command = connection.CreateCommand();
                    command.CommandText = TestQuery;

                    await command.ExecuteNonQueryAsync(cancellationToken);
                }
            }
            catch (DbException ex)
            {
                return new HealthCheckResult(status: context.Registration.FailureStatus, exception: ex);
            }
        }

        return HealthCheckResult.Healthy();
    }
}
