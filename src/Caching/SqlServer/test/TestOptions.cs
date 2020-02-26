// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Caching.SqlServer
{
    internal class TestSqlServerCacheOptions : IOptions<SqlServerCacheOptions>
    {
        private readonly SqlServerCacheOptions _innerOptions;

        public TestSqlServerCacheOptions(SqlServerCacheOptions innerOptions)
        {
            _innerOptions = innerOptions;
        }

        public SqlServerCacheOptions Value
        {
            get
            {
                return _innerOptions;
            }
        }
    }
}
