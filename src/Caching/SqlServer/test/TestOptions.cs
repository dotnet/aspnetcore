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
