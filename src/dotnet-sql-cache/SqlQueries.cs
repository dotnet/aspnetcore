// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Caching.SqlConfig.Tools
{
    internal class SqlQueries
    {
        private const string CreateTableFormat = "CREATE TABLE {0}(" +
            // Maximum size of primary key column is 900 bytes (898 bytes from key + 2 additional bytes used by the
            // Sql Server). In the case where the key is greater than 898 bytes, then it gets truncated.
            // - Add collation to the key column to make it case-sensitive
            "Id nvarchar(449) COLLATE SQL_Latin1_General_CP1_CS_AS NOT NULL, " +
            "Value varbinary(MAX) NOT NULL, " +
            "ExpiresAtTime datetimeoffset NOT NULL, " +
            "SlidingExpirationInSeconds bigint NULL," +
            "AbsoluteExpiration datetimeoffset NULL, " +
            "PRIMARY KEY (Id))";

        private const string CreateNonClusteredIndexOnExpirationTimeFormat
            = "CREATE NONCLUSTERED INDEX Index_ExpiresAtTime ON {0}(ExpiresAtTime)";

        private const string TableInfoFormat =
             "SELECT TABLE_CATALOG, TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE " +
             "FROM INFORMATION_SCHEMA.TABLES " +
             "WHERE TABLE_SCHEMA = '{0}' " +
             "AND TABLE_NAME = '{1}'";

        public SqlQueries(string schemaName, string tableName)
        {
            if (string.IsNullOrEmpty(schemaName))
            {
                throw new ArgumentException("Schema name cannot be empty or null");
            }
            if (string.IsNullOrEmpty(tableName))
            {
                throw new ArgumentException("Table name cannot be empty or null");
            }

            var tableNameWithSchema = string.Format(
                "{0}.{1}", DelimitIdentifier(schemaName), DelimitIdentifier(tableName));
            CreateTable = string.Format(CreateTableFormat, tableNameWithSchema);
            CreateNonClusteredIndexOnExpirationTime = string.Format(
                CreateNonClusteredIndexOnExpirationTimeFormat,
                tableNameWithSchema);
            TableInfo = string.Format(TableInfoFormat, EscapeLiteral(schemaName), EscapeLiteral(tableName));
        }

        public string CreateTable { get; }

        public string CreateNonClusteredIndexOnExpirationTime { get; }

        public string TableInfo { get; }

        // From EF's SqlServerQuerySqlGenerator
        private string DelimitIdentifier(string identifier)
        {
            return "[" + identifier.Replace("]", "]]") + "]";
        }

        private string EscapeLiteral(string literal)
        {
            return literal.Replace("'", "''");
        }
    }
}
