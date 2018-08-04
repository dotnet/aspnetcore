using System.Data.Common;
using System.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthChecksSample
{
    public class SqlConnectionHealthCheck : DbConnectionHealthCheck
    {
        private static readonly string DefaultTestQuery = "Select 1";

        public SqlConnectionHealthCheck(string name, string connectionString)
            : this(name, connectionString, testQuery: DefaultTestQuery)
        {
        }

        public SqlConnectionHealthCheck(string name, string connectionString, string testQuery)
            : base(name, connectionString, testQuery ?? DefaultTestQuery)
        {
        }

        protected override DbConnection CreateConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }
    }
}
