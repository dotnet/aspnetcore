using System;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;

namespace E2ETests
{
    /// <summary>
    /// Summary description for DtUtils
    /// </summary>
    public class DbUtils
    {
        private const string BaseConnString = @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;MultipleActiveResultSets=true;Connect Timeout=30;";

        public static string CreateConnectionString(string dbName)
            => new SqlConnectionStringBuilder(BaseConnString)
            {
                InitialCatalog = dbName
            }.ToString();

        public static string GetUniqueName()
            => "MusicStore_Test_" + Guid.NewGuid().ToString().Replace("-", string.Empty);

        public static void DropDatabase(string databaseName, ILogger logger)
        {
            if (!TestPlatformHelper.IsWindows)
            {
                return;
            }
            try
            {
                logger.LogInformation("Trying to drop database '{0}'", databaseName);
                using (var conn = new SqlConnection(CreateConnectionString("master")))
                {
                    conn.Open();

                    var cmd = conn.CreateCommand();
                    cmd.CommandText = string.Format(@"IF EXISTS (SELECT * FROM sys.databases WHERE name = N'{0}')
                                          BEGIN
                                               ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                                               DROP DATABASE [{0}];
                                          END", databaseName);
                    cmd.ExecuteNonQuery();

                    logger.LogInformation("Successfully dropped database {0}", databaseName);
                }
            }
            catch (Exception exception)
            {
                //Ignore if there is failure in cleanup.
                logger.LogWarning("Error occurred while dropping database {0}. Exception : {1}", databaseName, exception.ToString());
            }
        }
    }
}