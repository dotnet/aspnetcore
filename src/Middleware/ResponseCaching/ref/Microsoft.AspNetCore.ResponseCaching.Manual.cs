// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.ResponseCaching
{
    internal partial class CachedResponse : Microsoft.AspNetCore.ResponseCaching.IResponseCacheEntry
    {
        public CachedResponse() { }
        public System.IO.Stream Body { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.DateTimeOffset Created { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.IHeaderDictionary Headers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int StatusCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    internal partial class CachedVaryByRules : Microsoft.AspNetCore.ResponseCaching.IResponseCacheEntry
    {
        public CachedVaryByRules() { }
        public Microsoft.Extensions.Primitives.StringValues Headers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.Extensions.Primitives.StringValues QueryKeys { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string VaryByKeyPrefix { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    internal partial class FastGuid
    {
        internal FastGuid(long id) { }
        internal string IdString { get { throw null; } }
        internal long IdValue { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        internal static Microsoft.AspNetCore.ResponseCaching.FastGuid NewGuid() { throw null; }
    }
    internal partial interface IResponseCache
    {
        Microsoft.AspNetCore.ResponseCaching.IResponseCacheEntry Get(string key);
        System.Threading.Tasks.Task<Microsoft.AspNetCore.ResponseCaching.IResponseCacheEntry> GetAsync(string key);
        void Set(string key, Microsoft.AspNetCore.ResponseCaching.IResponseCacheEntry entry, System.TimeSpan validFor);
        System.Threading.Tasks.Task SetAsync(string key, Microsoft.AspNetCore.ResponseCaching.IResponseCacheEntry entry, System.TimeSpan validFor);
    }
    internal partial interface IResponseCacheEntry
    {
    }
    internal partial interface IResponseCachingKeyProvider
    {
        string CreateBaseKey(Microsoft.AspNetCore.ResponseCaching.ResponseCachingContext context);
        System.Collections.Generic.IEnumerable<string> CreateLookupVaryByKeys(Microsoft.AspNetCore.ResponseCaching.ResponseCachingContext context);
        string CreateStorageVaryByKey(Microsoft.AspNetCore.ResponseCaching.ResponseCachingContext context);
    }
    internal partial interface IResponseCachingPolicyProvider
    {
        bool AllowCacheLookup(Microsoft.AspNetCore.ResponseCaching.ResponseCachingContext context);
        bool AllowCacheStorage(Microsoft.AspNetCore.ResponseCaching.ResponseCachingContext context);
        bool AttemptResponseCaching(Microsoft.AspNetCore.ResponseCaching.ResponseCachingContext context);
        bool IsCachedEntryFresh(Microsoft.AspNetCore.ResponseCaching.ResponseCachingContext context);
        bool IsResponseCacheable(Microsoft.AspNetCore.ResponseCaching.ResponseCachingContext context);
    }
    internal partial interface ISystemClock
    {
        System.DateTimeOffset UtcNow { get; }
    }
    internal partial class MemoryResponseCache : Microsoft.AspNetCore.ResponseCaching.IResponseCache
    {
        internal MemoryResponseCache(Microsoft.Extensions.Caching.Memory.IMemoryCache cache) { }
        public Microsoft.AspNetCore.ResponseCaching.IResponseCacheEntry Get(string key) { throw null; }
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.ResponseCaching.IResponseCacheEntry> GetAsync(string key) { throw null; }
        public void Set(string key, Microsoft.AspNetCore.ResponseCaching.IResponseCacheEntry entry, System.TimeSpan validFor) { }
        public System.Threading.Tasks.Task SetAsync(string key, Microsoft.AspNetCore.ResponseCaching.IResponseCacheEntry entry, System.TimeSpan validFor) { throw null; }
    }
    internal partial class ResponseCachingContext
    {
        internal ResponseCachingContext(Microsoft.AspNetCore.Http.HttpContext httpContext, Microsoft.Extensions.Logging.ILogger logger) { }
        internal string BaseKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.TimeSpan? CachedEntryAge { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]internal set { } }
        internal Microsoft.AspNetCore.ResponseCaching.CachedResponse CachedResponse { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal Microsoft.AspNetCore.Http.IHeaderDictionary CachedResponseHeaders { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal System.TimeSpan CachedResponseValidFor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.ResponseCaching.CachedVaryByRules CachedVaryByRules { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.HttpContext HttpContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        internal Microsoft.Extensions.Logging.ILogger Logger { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        internal System.IO.Stream OriginalResponseStream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal Microsoft.AspNetCore.ResponseCaching.ResponseCachingStream ResponseCachingStream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal System.DateTimeOffset? ResponseDate { get { throw null; } set { } }
        internal System.DateTimeOffset? ResponseExpires { get { throw null; } }
        internal System.TimeSpan? ResponseMaxAge { get { throw null; } }
        internal System.TimeSpan? ResponseSharedMaxAge { get { throw null; } }
        internal bool ResponseStarted { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.DateTimeOffset? ResponseTime { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]internal set { } }
        internal bool ShouldCacheResponse { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal string StorageVaryKey { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    internal partial class ResponseCachingKeyProvider : Microsoft.AspNetCore.ResponseCaching.IResponseCachingKeyProvider
    {
        internal ResponseCachingKeyProvider(Microsoft.Extensions.ObjectPool.ObjectPoolProvider poolProvider, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.ResponseCaching.ResponseCachingOptions> options) { }
        public string CreateBaseKey(Microsoft.AspNetCore.ResponseCaching.ResponseCachingContext context) { throw null; }
        public System.Collections.Generic.IEnumerable<string> CreateLookupVaryByKeys(Microsoft.AspNetCore.ResponseCaching.ResponseCachingContext context) { throw null; }
        public string CreateStorageVaryByKey(Microsoft.AspNetCore.ResponseCaching.ResponseCachingContext context) { throw null; }
    }
    public partial class ResponseCachingMiddleware
    {
        internal ResponseCachingMiddleware(Microsoft.AspNetCore.Http.RequestDelegate next, Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.ResponseCaching.ResponseCachingOptions> options, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.AspNetCore.ResponseCaching.IResponseCachingPolicyProvider policyProvider, Microsoft.AspNetCore.ResponseCaching.IResponseCache cache, Microsoft.AspNetCore.ResponseCaching.IResponseCachingKeyProvider keyProvider) { }
        internal static void AddResponseCachingFeature(Microsoft.AspNetCore.Http.HttpContext context) { }
        internal static bool ContentIsNotModified(Microsoft.AspNetCore.ResponseCaching.ResponseCachingContext context) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        internal System.Threading.Tasks.Task FinalizeCacheBodyAsync(Microsoft.AspNetCore.ResponseCaching.ResponseCachingContext context) { throw null; }
        internal System.Threading.Tasks.Task FinalizeCacheHeadersAsync(Microsoft.AspNetCore.ResponseCaching.ResponseCachingContext context) { throw null; }
        internal static Microsoft.Extensions.Primitives.StringValues GetOrderCasingNormalizedStringValues(Microsoft.Extensions.Primitives.StringValues stringValues) { throw null; }
        internal void ShimResponseStream(Microsoft.AspNetCore.ResponseCaching.ResponseCachingContext context) { }
        internal System.Threading.Tasks.Task StartResponseAsync(Microsoft.AspNetCore.ResponseCaching.ResponseCachingContext context) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        internal System.Threading.Tasks.Task<bool> TryServeFromCacheAsync(Microsoft.AspNetCore.ResponseCaching.ResponseCachingContext context) { throw null; }
    }
    public partial class ResponseCachingOptions
    {
        internal Microsoft.AspNetCore.ResponseCaching.ISystemClock SystemClock { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    internal partial class ResponseCachingPolicyProvider : Microsoft.AspNetCore.ResponseCaching.IResponseCachingPolicyProvider
    {
        public ResponseCachingPolicyProvider() { }
        public virtual bool AllowCacheLookup(Microsoft.AspNetCore.ResponseCaching.ResponseCachingContext context) { throw null; }
        public virtual bool AllowCacheStorage(Microsoft.AspNetCore.ResponseCaching.ResponseCachingContext context) { throw null; }
        public virtual bool AttemptResponseCaching(Microsoft.AspNetCore.ResponseCaching.ResponseCachingContext context) { throw null; }
        public virtual bool IsCachedEntryFresh(Microsoft.AspNetCore.ResponseCaching.ResponseCachingContext context) { throw null; }
        public virtual bool IsResponseCacheable(Microsoft.AspNetCore.ResponseCaching.ResponseCachingContext context) { throw null; }
    }
    internal partial class ResponseCachingStream : System.IO.Stream
    {
        internal ResponseCachingStream(System.IO.Stream innerStream, long maxBufferSize, int segmentSize, System.Action startResponseCallback, System.Func<System.Threading.Tasks.Task> startResponseCallbackAsync) { }
        internal bool BufferingEnabled { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public override System.IAsyncResult BeginWrite(byte[] buffer, int offset, int count, System.AsyncCallback callback, object state) { throw null; }
        internal void DisableBuffering() { }
        public override void EndWrite(System.IAsyncResult asyncResult) { }
        public override void Flush() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        internal System.IO.Stream GetBufferStream() { throw null; }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override void WriteByte(byte value) { }
    }
    internal partial class SegmentReadStream : System.IO.Stream
    {
        internal SegmentReadStream(System.Collections.Generic.List<byte[]> segments, long length) { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public override System.IAsyncResult BeginRead(byte[] buffer, int offset, int count, System.AsyncCallback callback, object state) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.Task CopyToAsync(System.IO.Stream destination, int bufferSize, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override int EndRead(System.IAsyncResult asyncResult) { throw null; }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override int ReadByte() { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
    }
    internal partial class SegmentWriteStream : System.IO.Stream
    {
        internal SegmentWriteStream(int segmentSize) { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public override System.IAsyncResult BeginWrite(byte[] buffer, int offset, int count, System.AsyncCallback callback, object state) { throw null; }
        protected override void Dispose(bool disposing) { }
        public override void EndWrite(System.IAsyncResult asyncResult) { }
        public override void Flush() { }
        internal System.Collections.Generic.List<byte[]> GetSegments() { throw null; }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override void WriteByte(byte value) { }
    }
    internal static partial class StreamUtilities
    {
        internal static int BodySegmentSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal static System.IAsyncResult ToIAsyncResult(System.Threading.Tasks.Task task, System.AsyncCallback callback, object state) { throw null; }
    }
}
