// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace HealthChecksSample;

public class SqlConnectionHealthCheck : DbConnectionHealthCheck
{
    private static readonly string DefaultTestQuery = "Select 1";

    public SqlConnectionHealthCheck(string connectionString)
        : this(connectionString, testQuery: DefaultTestQuery)
    {
    }

    public SqlConnectionHealthCheck(string connectionString, string testQuery)
        : base(connectionString, testQuery ?? DefaultTestQuery)
    {
    }

    protected override DbConnection CreateConnection(string connectionString)
    {
        return new SqlConnection(connectionString);
    }
}
