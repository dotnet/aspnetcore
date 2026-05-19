param(
    [Parameter(Mandatory = $false)][string]$ConnectionString = "Server=(localdb)\MSSQLLocalDB;Database=CacheTestDb;Trusted_Connection=True;",
    [Parameter(Mandatory = $false)][string]$SchemaName = "dbo",
    [Parameter(Mandatory = $false)][string]$TableName = "CacheTest")

function ExecuteScalar($Connection, $Script) {
    $cmd = New-Object System.Data.SqlClient.SqlCommand
    $cmd.Connection = $Connection
    $cmd.CommandText = $Script
    $cmd.ExecuteScalar()
}

# Check if the database exists
Write-Host "Checking for database..."
$ServerConnectionBuilder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $ConnectionString

if (!$ServerConnectionBuilder.InitialCatalog) {
    throw "An 'Initial Catalog' or 'Database' value must be provided in the connection string!"
}
$DatabaseName = $ServerConnectionBuilder.InitialCatalog

if (!$ServerConnectionBuilder.DataSource -eq "(localdb)\MSSQLLocalDB") {
    Write-Warning "This script is really only designed for running against your local instance of SQL Local DB. Continue at your own risk!"
}

$ServerConnectionBuilder.Remove("Initial Catalog") | Out-Null;
$ServerConnection = New-Object System.Data.SqlClient.SqlConnection $ServerConnectionBuilder.ConnectionString
$ServerConnection.Open();

# Yes, this is SQL Injectable, but you're using it on your local machine with the intent of connecting to your local db.
$dbid = ExecuteScalar $ServerConnection "SELECT database_id FROM sys.databases WHERE Name = '$DatabaseName'"
if (!$dbid) {
    Write-Host "Database not found, creating..."

    # Create the database
    ExecuteScalar $ServerConnection "CREATE DATABASE $DatabaseName"
}

# Close the server connection
$ServerConnection.Close()

# Check for the table
$DbConnection = New-Object System.Data.SqlClient.SqlConnection $ConnectionString
$DbConnection.Open();
$tableid = ExecuteScalar $DbConnection "SELECT object_id FROM sys.objects WHERE type = 'U' AND name = '$TableName'"
if ($tableid) {
    Write-Host "Table exists, dropping it..."
    ExecuteScalar $DbConnection "DROP TABLE $TableName"
}

$DbConnection.Close()

# Fill the database with sql cache goodies
dotnet sql-cache create $ConnectionString $SchemaName $TableName

# Set environment variables and launch tests
$oldConnectionString = $env:SQLCACHETESTS_ConnectionString
$oldSchemaName = $env:SQLCACHETESTS_SchemaName
$oldTableName = $env:SQLCACHETESTS_TableName
$oldEnabled = $env:SQLCACHETESTS_ENABLED
try {
    $env:SQLCACHETESTS_ConnectionString = $ConnectionString
    $env:SQLCACHETESTS_SchemaName = $SchemaName
    $env:SQLCACHETESTS_TableName = $TableName
    $env:SQLCACHETESTS_ENABLED = "1"

    Write-Host "Launching Tests..."
    dotnet test "$PSScriptRoot/Microsoft.Extensions.Caching.SqlServer.Tests.csproj"
}
finally {
    if ($oldConnectionString) {
        $env:SQLCACHETESTS_ConnectionString = $oldConnectionString
    }
    else {
        Remove-Item env:\SQLCACHETESTS_ConnectionString
    }

    if ($oldSchemaName) {
        $env:SQLCACHETESTS_SchemaName = $oldSchemaName
    }
    else {
        Remove-Item env:\SQLCACHETESTS_SchemaName
    }

    if ($oldTableName) {
        $env:SQLCACHETESTS_TableName = $oldTableName
    }
    else {
        Remove-Item env:\SQLCACHETESTS_TableName
    }

    if ($oldEnabled) {
        $env:SQLCACHETESTS_ENABLED = $oldEnabled
    }
    else {
        Remove-Item env:\SQLCACHETESTS_ENABLED
    }
}
