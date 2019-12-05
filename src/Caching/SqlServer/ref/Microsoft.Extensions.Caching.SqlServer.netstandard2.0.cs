// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Caching.SqlServer
{
    public partial class SqlServerCache : Microsoft.Extensions.Caching.Distributed.IDistributedCache
    {
        public SqlServerCache(Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Caching.SqlServer.SqlServerCacheOptions> options) { }
        public byte[] Get(string key) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<byte[]> GetAsync(string key, System.Threading.CancellationToken token = default(System.Threading.CancellationToken)) { throw null; }
        public void Refresh(string key) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task RefreshAsync(string key, System.Threading.CancellationToken token = default(System.Threading.CancellationToken)) { throw null; }
        public void Remove(string key) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task RemoveAsync(string key, System.Threading.CancellationToken token = default(System.Threading.CancellationToken)) { throw null; }
        public void Set(string key, byte[] value, Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions options) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task SetAsync(string key, byte[] value, Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions options, System.Threading.CancellationToken token = default(System.Threading.CancellationToken)) { throw null; }
    }
    public partial class SqlServerCacheOptions : Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Caching.SqlServer.SqlServerCacheOptions>
    {
        public SqlServerCacheOptions() { }
        public string ConnectionString { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.TimeSpan DefaultSlidingExpiration { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.TimeSpan? ExpiredItemsDeletionInterval { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        Microsoft.Extensions.Caching.SqlServer.SqlServerCacheOptions Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Caching.SqlServer.SqlServerCacheOptions>.Value { get { throw null; } }
        public string SchemaName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.Extensions.Internal.ISystemClock SystemClock { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string TableName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class SqlServerCachingServicesExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddDistributedSqlServerCache(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.Extensions.Caching.SqlServer.SqlServerCacheOptions> setupAction) { throw null; }
    }
}
