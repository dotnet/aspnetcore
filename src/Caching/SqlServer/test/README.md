# Microsoft.Extensions.Caching.SqlServer Tests

These tests include functional tests that run against a real SQL Server. Since these are flaky on CI, they should be run manually when changing this code.

## Pre-requisites

1. A functional SQL Server, Local DB included with VS is sufficient
1. An empty database named `CacheTestDb` in that SQL Server

## Running the tests

1. Install the latest version of the `dotnet-sql-cache` too: `dotnet tool install --global dotnet-sql-cache` (make sure to specify a version if it's a pre-release tool you want!)
1. Run `dotnet sql-cache [connectionstring] dbo CacheTest`
    * `[connectionstring]` must be a SQL Connection String **for an empty database named `CacheTestDb` that already exists**
    * If using Local DB, this string should work: `"Server=(localdb)\MSSQLLocalDB;Database=CacheTestDb;Trusted_Connection=True;"`
1. Unskip the tests by changing the `SqlServerCacheWithDatabaseTest.SkipReason` field to `null`.
1. Run the tests.