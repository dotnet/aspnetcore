using System;
using System.Data.SqlClient;

namespace E2ETests
{
    /// <summary>
    /// Summary description for DtUtils
    /// </summary>
    public class DbUtils
    {
        public static void DropDatabase(string databaseName)
        {
            try
            {
                Console.WriteLine("Trying to drop database '{0}'", databaseName);
                using (var conn = new SqlConnection(@"Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;"))
                {
                    conn.Open();

                    var cmd = conn.CreateCommand();
                    cmd.CommandText = string.Format(@"IF EXISTS (SELECT * FROM sys.databases WHERE name = N'{0}')
                                          BEGIN
                                               ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                                               DROP DATABASE [{0}];
                                          END", databaseName);
                    cmd.ExecuteNonQuery();

                    Console.WriteLine("Successfully dropped database {0}", databaseName);
                }
            }
            catch (Exception exception)
            {
                //Ignore if there is failure in cleanup.
                Console.WriteLine("Error occured while dropping database {0}. Exception : {1}", databaseName, exception);
            }
        }
    }
}