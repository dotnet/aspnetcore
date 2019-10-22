// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder
{
    public partial class ApplicationBuilder : Microsoft.AspNetCore.Builder.IApplicationBuilder
    {
        public ApplicationBuilder(System.IServiceProvider serviceProvider) { }
        public ApplicationBuilder(System.IServiceProvider serviceProvider, object server) { }
        public System.IServiceProvider ApplicationServices { get { throw null; } set { } }
        public System.Collections.Generic.IDictionary<string, object> Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Http.Features.IFeatureCollection ServerFeatures { get { throw null; } }
        public Microsoft.AspNetCore.Http.RequestDelegate Build() { throw null; }
        public Microsoft.AspNetCore.Builder.IApplicationBuilder New() { throw null; }
        public Microsoft.AspNetCore.Builder.IApplicationBuilder Use(System.Func<Microsoft.AspNetCore.Http.RequestDelegate, Microsoft.AspNetCore.Http.RequestDelegate> middleware) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Http
{
    public partial class BindingAddress
    {
        public BindingAddress() { }
        public string Host { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool IsUnixPipe { get { throw null; } }
        public string PathBase { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public int Port { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Scheme { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string UnixPipePath { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static Microsoft.AspNetCore.Http.BindingAddress Parse(string address) { throw null; }
        public override string ToString() { throw null; }
    }
    public sealed partial class DefaultHttpContext : Microsoft.AspNetCore.Http.HttpContext
    {
        public DefaultHttpContext() { }
        public DefaultHttpContext(Microsoft.AspNetCore.Http.Features.IFeatureCollection features) { }
        public override Microsoft.AspNetCore.Http.ConnectionInfo Connection { get { throw null; } }
        public override Microsoft.AspNetCore.Http.Features.IFeatureCollection Features { get { throw null; } }
        public Microsoft.AspNetCore.Http.Features.FormOptions FormOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public Microsoft.AspNetCore.Http.HttpContext HttpContext { get { throw null; } }
        public override System.Collections.Generic.IDictionary<object, object> Items { get { throw null; } set { } }
        public override Microsoft.AspNetCore.Http.HttpRequest Request { get { throw null; } }
        public override System.Threading.CancellationToken RequestAborted { get { throw null; } set { } }
        public override System.IServiceProvider RequestServices { get { throw null; } set { } }
        public override Microsoft.AspNetCore.Http.HttpResponse Response { get { throw null; } }
        public Microsoft.Extensions.DependencyInjection.IServiceScopeFactory ServiceScopeFactory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public override Microsoft.AspNetCore.Http.ISession Session { get { throw null; } set { } }
        public override string TraceIdentifier { get { throw null; } set { } }
        public override System.Security.Claims.ClaimsPrincipal User { get { throw null; } set { } }
        public override Microsoft.AspNetCore.Http.WebSocketManager WebSockets { get { throw null; } }
        public override void Abort() { }
        public void Initialize(Microsoft.AspNetCore.Http.Features.IFeatureCollection features) { }
        public void Uninitialize() { }
    }
    public partial class FormCollection : Microsoft.AspNetCore.Http.IFormCollection, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.IEnumerable
    {
        public static readonly Microsoft.AspNetCore.Http.FormCollection Empty;
        public FormCollection(System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues> fields, Microsoft.AspNetCore.Http.IFormFileCollection files = null) { }
        public int Count { get { throw null; } }
        public Microsoft.AspNetCore.Http.IFormFileCollection Files { get { throw null; } }
        public Microsoft.Extensions.Primitives.StringValues this[string key] { get { throw null; } }
        public System.Collections.Generic.ICollection<string> Keys { get { throw null; } }
        public bool ContainsKey(string key) { throw null; }
        public Microsoft.AspNetCore.Http.FormCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public bool TryGetValue(string key, out Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
        }
    }
    public partial class FormFile : Microsoft.AspNetCore.Http.IFormFile
    {
        public FormFile(System.IO.Stream baseStream, long baseStreamOffset, long length, string name, string fileName) { }
        public string ContentDisposition { get { throw null; } set { } }
        public string ContentType { get { throw null; } set { } }
        public string FileName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Microsoft.AspNetCore.Http.IHeaderDictionary Headers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public long Length { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public void CopyTo(System.IO.Stream target) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task CopyToAsync(System.IO.Stream target, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.IO.Stream OpenReadStream() { throw null; }
    }
    public partial class FormFileCollection : System.Collections.Generic.List<Microsoft.AspNetCore.Http.IFormFile>, Microsoft.AspNetCore.Http.IFormFileCollection, System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Http.IFormFile>, System.Collections.Generic.IReadOnlyCollection<Microsoft.AspNetCore.Http.IFormFile>, System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.IFormFile>, System.Collections.IEnumerable
    {
        public FormFileCollection() { }
        public Microsoft.AspNetCore.Http.IFormFile this[string name] { get { throw null; } }
        public Microsoft.AspNetCore.Http.IFormFile GetFile(string name) { throw null; }
        public System.Collections.Generic.IReadOnlyList<Microsoft.AspNetCore.Http.IFormFile> GetFiles(string name) { throw null; }
    }
    public partial class HeaderDictionary : Microsoft.AspNetCore.Http.IHeaderDictionary, System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.Generic.IDictionary<string, Microsoft.Extensions.Primitives.StringValues>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.IEnumerable
    {
        public HeaderDictionary() { }
        public HeaderDictionary(System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues> store) { }
        public HeaderDictionary(int capacity) { }
        public long? ContentLength { get { throw null; } set { } }
        public int Count { get { throw null; } }
        public bool IsReadOnly { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.Extensions.Primitives.StringValues this[string key] { get { throw null; } set { } }
        public System.Collections.Generic.ICollection<string> Keys { get { throw null; } }
        Microsoft.Extensions.Primitives.StringValues System.Collections.Generic.IDictionary<System.String,Microsoft.Extensions.Primitives.StringValues>.this[string key] { get { throw null; } set { } }
        public System.Collections.Generic.ICollection<Microsoft.Extensions.Primitives.StringValues> Values { get { throw null; } }
        public void Add(System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> item) { }
        public void Add(string key, Microsoft.Extensions.Primitives.StringValues value) { }
        public void Clear() { }
        public bool Contains(System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> item) { throw null; }
        public bool ContainsKey(string key) { throw null; }
        public void CopyTo(System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>[] array, int arrayIndex) { }
        public Microsoft.AspNetCore.Http.HeaderDictionary.Enumerator GetEnumerator() { throw null; }
        public bool Remove(System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> item) { throw null; }
        public bool Remove(string key) { throw null; }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public bool TryGetValue(string key, out Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
        }
    }
    public partial class HttpContextAccessor : Microsoft.AspNetCore.Http.IHttpContextAccessor
    {
        public HttpContextAccessor() { }
        public Microsoft.AspNetCore.Http.HttpContext HttpContext { get { throw null; } set { } }
    }
    [System.ObsoleteAttribute("This is obsolete and will be removed in a future version. Use DefaultHttpContextFactory instead.")]
    public partial class HttpContextFactory : Microsoft.AspNetCore.Http.IHttpContextFactory
    {
        public HttpContextFactory(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Http.Features.FormOptions> formOptions) { }
        public HttpContextFactory(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Http.Features.FormOptions> formOptions, Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor) { }
        public HttpContextFactory(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Http.Features.FormOptions> formOptions, Microsoft.Extensions.DependencyInjection.IServiceScopeFactory serviceScopeFactory) { }
        public HttpContextFactory(Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Http.Features.FormOptions> formOptions, Microsoft.Extensions.DependencyInjection.IServiceScopeFactory serviceScopeFactory, Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor) { }
        public Microsoft.AspNetCore.Http.HttpContext Create(Microsoft.AspNetCore.Http.Features.IFeatureCollection featureCollection) { throw null; }
        public void Dispose(Microsoft.AspNetCore.Http.HttpContext httpContext) { }
    }
    public static partial class HttpRequestRewindExtensions
    {
        public static void EnableBuffering(this Microsoft.AspNetCore.Http.HttpRequest request) { }
        public static void EnableBuffering(this Microsoft.AspNetCore.Http.HttpRequest request, int bufferThreshold) { }
        public static void EnableBuffering(this Microsoft.AspNetCore.Http.HttpRequest request, int bufferThreshold, long bufferLimit) { }
        public static void EnableBuffering(this Microsoft.AspNetCore.Http.HttpRequest request, long bufferLimit) { }
    }
    public partial class MiddlewareFactory : Microsoft.AspNetCore.Http.IMiddlewareFactory
    {
        public MiddlewareFactory(System.IServiceProvider serviceProvider) { }
        public Microsoft.AspNetCore.Http.IMiddleware Create(System.Type middlewareType) { throw null; }
        public void Release(Microsoft.AspNetCore.Http.IMiddleware middleware) { }
    }
    public partial class QueryCollection : Microsoft.AspNetCore.Http.IQueryCollection, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.IEnumerable
    {
        public static readonly Microsoft.AspNetCore.Http.QueryCollection Empty;
        public QueryCollection() { }
        public QueryCollection(Microsoft.AspNetCore.Http.QueryCollection store) { }
        public QueryCollection(System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues> store) { }
        public QueryCollection(int capacity) { }
        public int Count { get { throw null; } }
        public Microsoft.Extensions.Primitives.StringValues this[string key] { get { throw null; } }
        public System.Collections.Generic.ICollection<string> Keys { get { throw null; } }
        public bool ContainsKey(string key) { throw null; }
        public Microsoft.AspNetCore.Http.QueryCollection.Enumerator GetEnumerator() { throw null; }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public bool TryGetValue(string key, out Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            void System.Collections.IEnumerator.Reset() { }
        }
    }
    public static partial class RequestFormReaderExtensions
    {
        public static System.Threading.Tasks.Task<Microsoft.AspNetCore.Http.IFormCollection> ReadFormAsync(this Microsoft.AspNetCore.Http.HttpRequest request, Microsoft.AspNetCore.Http.Features.FormOptions options, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    public static partial class SendFileFallback
    {
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.Task SendFileAsync(System.IO.Stream destination, string filePath, long offset, long? count, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public partial class StreamResponseBodyFeature : Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature
    {
        public StreamResponseBodyFeature(System.IO.Stream stream) { }
        public StreamResponseBodyFeature(System.IO.Stream stream, Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature priorFeature) { }
        public Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature PriorFeature { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.IO.Stream Stream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.IO.Pipelines.PipeWriter Writer { get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task CompleteAsync() { throw null; }
        public virtual void DisableBuffering() { }
        public void Dispose() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public virtual System.Threading.Tasks.Task SendFileAsync(string path, long offset, long? count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public virtual System.Threading.Tasks.Task StartAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Http.Features
{
    public partial class DefaultSessionFeature : Microsoft.AspNetCore.Http.Features.ISessionFeature
    {
        public DefaultSessionFeature() { }
        public Microsoft.AspNetCore.Http.ISession Session { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class FormFeature : Microsoft.AspNetCore.Http.Features.IFormFeature
    {
        public FormFeature(Microsoft.AspNetCore.Http.HttpRequest request) { }
        public FormFeature(Microsoft.AspNetCore.Http.HttpRequest request, Microsoft.AspNetCore.Http.Features.FormOptions options) { }
        public FormFeature(Microsoft.AspNetCore.Http.IFormCollection form) { }
        public Microsoft.AspNetCore.Http.IFormCollection Form { get { throw null; } set { } }
        public bool HasFormContentType { get { throw null; } }
        public Microsoft.AspNetCore.Http.IFormCollection ReadForm() { throw null; }
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Http.IFormCollection> ReadFormAsync() { throw null; }
        public System.Threading.Tasks.Task<Microsoft.AspNetCore.Http.IFormCollection> ReadFormAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public partial class FormOptions
    {
        public const int DefaultBufferBodyLengthLimit = 134217728;
        public const int DefaultMemoryBufferThreshold = 65536;
        public const long DefaultMultipartBodyLengthLimit = (long)134217728;
        public const int DefaultMultipartBoundaryLengthLimit = 128;
        public FormOptions() { }
        public bool BufferBody { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public long BufferBodyLengthLimit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int KeyLengthLimit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int MemoryBufferThreshold { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public long MultipartBodyLengthLimit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int MultipartBoundaryLengthLimit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int MultipartHeadersCountLimit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int MultipartHeadersLengthLimit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int ValueCountLimit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int ValueLengthLimit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class HttpConnectionFeature : Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature
    {
        public HttpConnectionFeature() { }
        public string ConnectionId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Net.IPAddress LocalIpAddress { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int LocalPort { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Net.IPAddress RemoteIpAddress { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int RemotePort { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class HttpRequestFeature : Microsoft.AspNetCore.Http.Features.IHttpRequestFeature
    {
        public HttpRequestFeature() { }
        public System.IO.Stream Body { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Microsoft.AspNetCore.Http.IHeaderDictionary Headers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Method { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Path { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string PathBase { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Protocol { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string QueryString { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string RawTarget { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Scheme { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class HttpRequestIdentifierFeature : Microsoft.AspNetCore.Http.Features.IHttpRequestIdentifierFeature
    {
        public HttpRequestIdentifierFeature() { }
        public string TraceIdentifier { get { throw null; } set { } }
    }
    public partial class HttpRequestLifetimeFeature : Microsoft.AspNetCore.Http.Features.IHttpRequestLifetimeFeature
    {
        public HttpRequestLifetimeFeature() { }
        public System.Threading.CancellationToken RequestAborted { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public void Abort() { }
    }
    public partial class HttpResponseFeature : Microsoft.AspNetCore.Http.Features.IHttpResponseFeature
    {
        public HttpResponseFeature() { }
        public System.IO.Stream Body { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual bool HasStarted { get { throw null; } }
        public Microsoft.AspNetCore.Http.IHeaderDictionary Headers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string ReasonPhrase { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int StatusCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public virtual void OnCompleted(System.Func<object, System.Threading.Tasks.Task> callback, object state) { }
        public virtual void OnStarting(System.Func<object, System.Threading.Tasks.Task> callback, object state) { }
    }
    public partial class ItemsFeature : Microsoft.AspNetCore.Http.Features.IItemsFeature
    {
        public ItemsFeature() { }
        public System.Collections.Generic.IDictionary<object, object> Items { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class QueryFeature : Microsoft.AspNetCore.Http.Features.IQueryFeature
    {
        public QueryFeature(Microsoft.AspNetCore.Http.Features.IFeatureCollection features) { }
        public QueryFeature(Microsoft.AspNetCore.Http.IQueryCollection query) { }
        public Microsoft.AspNetCore.Http.IQueryCollection Query { get { throw null; } set { } }
    }
    public partial class RequestBodyPipeFeature : Microsoft.AspNetCore.Http.Features.IRequestBodyPipeFeature
    {
        public RequestBodyPipeFeature(Microsoft.AspNetCore.Http.HttpContext context) { }
        public System.IO.Pipelines.PipeReader Reader { get { throw null; } }
    }
    public partial class RequestCookiesFeature : Microsoft.AspNetCore.Http.Features.IRequestCookiesFeature
    {
        public RequestCookiesFeature(Microsoft.AspNetCore.Http.Features.IFeatureCollection features) { }
        public RequestCookiesFeature(Microsoft.AspNetCore.Http.IRequestCookieCollection cookies) { }
        public Microsoft.AspNetCore.Http.IRequestCookieCollection Cookies { get { throw null; } set { } }
    }
    public partial class RequestServicesFeature : Microsoft.AspNetCore.Http.Features.IServiceProvidersFeature, System.IAsyncDisposable, System.IDisposable
    {
        public RequestServicesFeature(Microsoft.AspNetCore.Http.HttpContext context, Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory) { }
        public System.IServiceProvider RequestServices { get { throw null; } set { } }
        public void Dispose() { }
        public System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
    }
    public partial class ResponseCookiesFeature : Microsoft.AspNetCore.Http.Features.IResponseCookiesFeature
    {
        public ResponseCookiesFeature(Microsoft.AspNetCore.Http.Features.IFeatureCollection features) { }
        public ResponseCookiesFeature(Microsoft.AspNetCore.Http.Features.IFeatureCollection features, Microsoft.Extensions.ObjectPool.ObjectPool<System.Text.StringBuilder> builderPool) { }
        public Microsoft.AspNetCore.Http.IResponseCookies Cookies { get { throw null; } }
    }
    public partial class RouteValuesFeature : Microsoft.AspNetCore.Http.Features.IRouteValuesFeature
    {
        public RouteValuesFeature() { }
        public Microsoft.AspNetCore.Routing.RouteValueDictionary RouteValues { get { throw null; } set { } }
    }
    public partial class ServiceProvidersFeature : Microsoft.AspNetCore.Http.Features.IServiceProvidersFeature
    {
        public ServiceProvidersFeature() { }
        public System.IServiceProvider RequestServices { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
    public partial class TlsConnectionFeature : Microsoft.AspNetCore.Http.Features.ITlsConnectionFeature
    {
        public TlsConnectionFeature() { }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 ClientCertificate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.Threading.Tasks.Task<System.Security.Cryptography.X509Certificates.X509Certificate2> GetClientCertificateAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Http.Features.Authentication
{
    public partial class HttpAuthenticationFeature : Microsoft.AspNetCore.Http.Features.Authentication.IHttpAuthenticationFeature
    {
        public HttpAuthenticationFeature() { }
        public System.Security.Claims.ClaimsPrincipal User { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class HttpServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddHttpContextAccessor(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
    }
}
