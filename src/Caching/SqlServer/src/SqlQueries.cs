// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.Extensions.Caching.SqlServer;

internal sealed class SqlQueries
{
    private const string TableInfoFormat =
        "SELECT TABLE_CATALOG, TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE " +
        "FROM INFORMATION_SCHEMA.TABLES " +
        "WHERE TABLE_SCHEMA = '{0}' " +
        "AND TABLE_NAME = '{1}'";

    private const string UpdateCacheItemFormat =
    "UPDATE {0} " +
    "SET ExpiresAtTime = " +
        "(CASE " +
            "WHEN DATEDIFF(SECOND, @UtcNow, AbsoluteExpiration) <= SlidingExpirationInSeconds " +
            "THEN AbsoluteExpiration " +
            "ELSE " +
            "DATEADD(SECOND, SlidingExpirationInSeconds, @UtcNow) " +
        "END) " +
    "WHERE Id = @Id " +
    "AND @UtcNow <= ExpiresAtTime " +
    "AND SlidingExpirationInSeconds IS NOT NULL " +
    "AND (AbsoluteExpiration IS NULL OR AbsoluteExpiration <> ExpiresAtTime) ;";

    private const string GetCacheItemFormat =
        "SELECT Value " +
        "FROM {0} WHERE Id = @Id AND @UtcNow <= ExpiresAtTime;";

    private const string SetCacheItemFormat =
        "DECLARE @ExpiresAtTime DATETIMEOFFSET; " +
        "SET @ExpiresAtTime = " +
        "(CASE " +
                "WHEN (@SlidingExpirationInSeconds IS NUll) " +
                "THEN @AbsoluteExpiration " +
                "ELSE " +
                "DATEADD(SECOND, Convert(bigint, @SlidingExpirationInSeconds), @UtcNow) " +
        "END);" +
        "UPDATE {0} SET Value = @Value, ExpiresAtTime = @ExpiresAtTime," +
        "SlidingExpirationInSeconds = @SlidingExpirationInSeconds, AbsoluteExpiration = @AbsoluteExpiration " +
        "WHERE Id = @Id " +
        "IF (@@ROWCOUNT = 0) " +
        "BEGIN " +
            "INSERT INTO {0} " +
            "(Id, Value, ExpiresAtTime, SlidingExpirationInSeconds, AbsoluteExpiration) " +
            "VALUES (@Id, @Value, @ExpiresAtTime, @SlidingExpirationInSeconds, @AbsoluteExpiration); " +
        "END ";

    private const string DeleteCacheItemFormat = "DELETE FROM {0} WHERE Id = @Id";

    public const string DeleteExpiredCacheItemsFormat = "DELETE FROM {0} WHERE @UtcNow > ExpiresAtTime";

    public SqlQueries(string schemaName, string tableName)
    {
        var tableNameWithSchema = string.Format(
            CultureInfo.InvariantCulture,
            "{0}.{1}", DelimitIdentifier(schemaName), DelimitIdentifier(tableName));

        // when retrieving an item, we do an UPDATE first and then a SELECT
        GetCacheItem = string.Format(CultureInfo.InvariantCulture, UpdateCacheItemFormat + GetCacheItemFormat, tableNameWithSchema);
        GetCacheItemWithoutValue = string.Format(CultureInfo.InvariantCulture, UpdateCacheItemFormat, tableNameWithSchema);
        DeleteCacheItem = string.Format(CultureInfo.InvariantCulture, DeleteCacheItemFormat, tableNameWithSchema);
        DeleteExpiredCacheItems = string.Format(CultureInfo.InvariantCulture, DeleteExpiredCacheItemsFormat, tableNameWithSchema);
        SetCacheItem = string.Format(CultureInfo.InvariantCulture, SetCacheItemFormat, tableNameWithSchema);
        TableInfo = string.Format(CultureInfo.InvariantCulture, TableInfoFormat, EscapeLiteral(schemaName), EscapeLiteral(tableName));
    }

    public string TableInfo { get; }

    public string GetCacheItem { get; }

    public string GetCacheItemWithoutValue { get; }

    public string SetCacheItem { get; }

    public string DeleteCacheItem { get; }

    public string DeleteExpiredCacheItems { get; }

    // From EF's SqlServerQuerySqlGenerator
    private static string DelimitIdentifier(string identifier)
    {
        return "[" + identifier.Replace("]", "]]") + "]";
    }

    private static string EscapeLiteral(string literal)
    {
        return literal.Replace("'", "''");
    }
}
