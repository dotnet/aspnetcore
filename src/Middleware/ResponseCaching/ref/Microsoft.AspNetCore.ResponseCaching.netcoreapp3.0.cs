// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public static partial class ResponseCachingExtensions
    {
        public static Microsoft.AspNetCore.Builder.IApplicationBuilder UseResponseCaching(this Microsoft.AspNetCore.Builder.IApplicationBuilder app) { throw null; }
    }
}
namespace Microsoft.AspNetCore.ResponseCaching
{
    public partial class ResponseCachingFeature : Microsoft.AspNetCore.ResponseCaching.IResponseCachingFeature
    {
        public ResponseCachingFeature() { }
        public string[] VaryByQueryKeys { get { throw null; } set { } }
    }
    public partial class ResponseCachingMiddleware
    {
        public ResponseCachingMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.ResponseCaching.ResponseCachingOptions> options, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.ResponseCaching.Internal.IResponseCachingPolicyProvider policyProvider, Microsoft.AspNetCore.ResponseCaching.Internal.IResponseCachingKeyProvider keyProvider) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task Invoke(Microsoft.AspNetCore.Http.HttpContext httpContext) { throw null; }
    }
    public partial class ResponseCachingOptions
    {
        public ResponseCachingOptions() { }
        public long MaximumBodySize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public long SizeLimit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool UseCaseSensitivePaths { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace Microsoft.AspNetCore.ResponseCaching.Internal
{
    public partial class CachedResponse : Microsoft.AspNetCore.ResponseCaching.Internal.IResponseCacheEntry
    {
        public CachedResponse() { }
        public System.IO.Stream Body { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.DateTimeOffset Created { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.IHeaderDictionary Headers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int StatusCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class CachedVaryByRules : Microsoft.AspNetCore.ResponseCaching.Internal.IResponseCacheEntry
    {
        public CachedVaryByRules() { }
        public Microsoft.Extensions.Primitives.StringValues Headers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.Extensions.Primitives.StringValues QueryKeys { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string VaryByKeyPrefix { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial interface IResponseCache
    {
        Microsoft.AspNetCore.ResponseCaching.Internal.IResponseCacheEntry Get(string key);
        System.Threading.Tasks.Task<Microsoft.AspNetCore.ResponseCaching.Internal.IResponseCacheEntry> GetAsync(string key);
        void Set(string key, Microsoft.AspNetCore.ResponseCaching.Internal.IResponseCacheEntry entry, System.TimeSpan validFor);
        System.Threading.Tasks.Task SetAsync(string key, Microsoft.AspNetCore.ResponseCaching.Internal.IResponseCacheEntry entry, System.TimeSpan validFor);
    }
    public partial interface IResponseCacheEntry
    {
    }
    public partial interface IResponseCachingKeyProvider
    {
        string CreateBaseKey(Microsoft.AspNetCore.ResponseCaching.Internal.ResponseCachingContext context);
        System.Collections.Generic.IEnumerable<string> CreateLookupVaryByKeys(Microsoft.AspNetCore.ResponseCaching.Internal.ResponseCachingContext context);
        string CreateStorageVaryByKey(Microsoft.AspNetCore.ResponseCaching.Internal.ResponseCachingContext context);
    }
    public partial interface IResponseCachingPolicyProvider
    {
        bool AllowCacheLookup(Microsoft.AspNetCore.ResponseCaching.Internal.ResponseCachingContext context);
        bool AllowCacheStorage(Microsoft.AspNetCore.ResponseCaching.Internal.ResponseCachingContext context);
        bool AttemptResponseCaching(Microsoft.AspNetCore.ResponseCaching.Internal.ResponseCachingContext context);
        bool IsCachedEntryFresh(Microsoft.AspNetCore.ResponseCaching.Internal.ResponseCachingContext context);
        bool IsResponseCacheable(Microsoft.AspNetCore.ResponseCaching.Internal.ResponseCachingContext context);
    }
    public partial class MemoryResponseCache : Microsoft.AspNetCore.ResponseCaching.Internal.IResponseCache
    {
        public MemoryResponseCache(Microsoft.Extensions.Caching.Memory.IMemoryCache cache) { }
        public Microsoft.AspNetCore.ResponseCaching.Internal.IResponseCacheEntry Get(string key) { throw null; }
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.ResponseCaching.Internal.IResponseCacheEntry> GetAsync(string key) { throw null; }
        public void Set(string key, Microsoft.AspNetCore.ResponseCaching.Internal.IResponseCacheEntry entry, System.TimeSpan validFor) { }
        public System.Threading.Tasks.Task SetAsync(string key, Microsoft.AspNetCore.ResponseCaching.Internal.IResponseCacheEntry entry, System.TimeSpan validFor) { throw null; }
    }
    public partial class ResponseCachingContext
    {
        internal ResponseCachingContext() { }
        public System.TimeSpan? CachedEntryAge { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.ResponseCaching.Internal.CachedVaryByRules CachedVaryByRules { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Http.HttpContext HttpContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.DateTimeOffset? ResponseTime { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    public partial class ResponseCachingKeyProvider : Microsoft.AspNetCore.ResponseCaching.Internal.IResponseCachingKeyProvider
    {
        public ResponseCachingKeyProvider(Microsoft.Extensions.ObjectPool.ObjectPoolProvider poolProvider, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.ResponseCaching.ResponseCachingOptions> options) { }
        public string CreateBaseKey(Microsoft.AspNetCore.ResponseCaching.Internal.ResponseCachingContext context) { throw null; }
        public System.Collections.Generic.IEnumerable<string> CreateLookupVaryByKeys(Microsoft.AspNetCore.ResponseCaching.Internal.ResponseCachingContext context) { throw null; }
        public string CreateStorageVaryByKey(Microsoft.AspNetCore.ResponseCaching.Internal.ResponseCachingContext context) { throw null; }
    }
    public partial class ResponseCachingPolicyProvider : Microsoft.AspNetCore.ResponseCaching.Internal.IResponseCachingPolicyProvider
    {
        public ResponseCachingPolicyProvider() { }
        public virtual bool AllowCacheLookup(Microsoft.AspNetCore.ResponseCaching.Internal.ResponseCachingContext context) { throw null; }
        public virtual bool AllowCacheStorage(Microsoft.AspNetCore.ResponseCaching.Internal.ResponseCachingContext context) { throw null; }
        public virtual bool AttemptResponseCaching(Microsoft.AspNetCore.ResponseCaching.Internal.ResponseCachingContext context) { throw null; }
        public virtual bool IsCachedEntryFresh(Microsoft.AspNetCore.ResponseCaching.Internal.ResponseCachingContext context) { throw null; }
        public virtual bool IsResponseCacheable(Microsoft.AspNetCore.ResponseCaching.Internal.ResponseCachingContext context) { throw null; }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ResponseCachingServicesExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddResponseCaching(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddResponseCaching(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Action<Microsoft.AspNetCore.ResponseCaching.ResponseCachingOptions> configureOptions) { throw null; }
    }
}
