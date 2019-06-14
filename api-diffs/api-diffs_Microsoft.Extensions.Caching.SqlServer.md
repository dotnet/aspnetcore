# Microsoft.Extensions.Caching.SqlServer

``` diff
-namespace Microsoft.Extensions.Caching.SqlServer {
 {
-    public class SqlServerCache : IDistributedCache {
 {
-        public SqlServerCache(IOptions<SqlServerCacheOptions> options);

-        public byte[] Get(string key);

-        public Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken));

-        public void Refresh(string key);

-        public Task RefreshAsync(string key, CancellationToken token = default(CancellationToken));

-        public void Remove(string key);

-        public Task RemoveAsync(string key, CancellationToken token = default(CancellationToken));

-        public void Set(string key, byte[] value, DistributedCacheEntryOptions options);

-        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken));

-    }
-    public class SqlServerCacheOptions : IOptions<SqlServerCacheOptions> {
 {
-        public SqlServerCacheOptions();

-        public string ConnectionString { get; set; }

-        public TimeSpan DefaultSlidingExpiration { get; set; }

-        public Nullable<TimeSpan> ExpiredItemsDeletionInterval { get; set; }

-        SqlServerCacheOptions Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Caching.SqlServer.SqlServerCacheOptions>.Value { get; }

-        public string SchemaName { get; set; }

-        public ISystemClock SystemClock { get; set; }

-        public string TableName { get; set; }

-    }
-}
```

