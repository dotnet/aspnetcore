// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Builder.Internal
{
    public partial class ApplicationBuilder : Microsoft.AspNetCore.Builder.IApplicationBuilder
    {
        public ApplicationBuilder(System.IServiceProvider serviceProvider) { }
        public ApplicationBuilder(System.IServiceProvider serviceProvider, object server) { }
        public System.IServiceProvider ApplicationServices { get { throw null; } set { } }
        public System.Collections.Generic.IDictionary<string, object> Properties { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Http.Features.IFeatureCollection ServerFeatures { get { throw null; } }
        public Microsoft.AspNetCore.Http.RequestDelegate Build() { throw null; }
        public Microsoft.AspNetCore.Builder.IApplicationBuilder New() { throw null; }
        public Microsoft.AspNetCore.Builder.IApplicationBuilder Use(System.Func<Microsoft.AspNetCore.Http.RequestDelegate, Microsoft.AspNetCore.Http.RequestDelegate> middleware) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Http
{
    public sealed partial class DefaultHttpContext : Microsoft.AspNetCore.Http.HttpContext
    {
        public DefaultHttpContext() { }
        public DefaultHttpContext(Microsoft.AspNetCore.Http.Features.IFeatureCollection features) { }
        public override Microsoft.AspNetCore.Http.ConnectionInfo Connection { get { throw null; } }
        public override Microsoft.AspNetCore.Http.Features.IFeatureCollection Features { get { throw null; } }
        public Microsoft.AspNetCore.Http.Features.FormOptions FormOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override System.Collections.Generic.IDictionary<object, object> Items { get { throw null; } set { } }
        public override Microsoft.AspNetCore.Http.HttpRequest Request { get { throw null; } }
        public override System.Threading.CancellationToken RequestAborted { get { throw null; } set { } }
        public override System.IServiceProvider RequestServices { get { throw null; } set { } }
        public override Microsoft.AspNetCore.Http.HttpResponse Response { get { throw null; } }
        public Microsoft.Extensions.DependencyInjection.IServiceScopeFactory ServiceScopeFactory { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public override Microsoft.AspNetCore.Http.ISession Session { get { throw null; } set { } }
        public override string TraceIdentifier { get { throw null; } set { } }
        public override System.Security.Claims.ClaimsPrincipal User { get { throw null; } set { } }
        public override Microsoft.AspNetCore.Http.WebSocketManager WebSockets { get { throw null; } }
        public override void Abort() { }
        public void Initialize(Microsoft.AspNetCore.Http.Features.IFeatureCollection features) { }
        public void Uninitialize() { }
    }
    public partial class DefaultHttpContextFactory : Microsoft.AspNetCore.Http.IHttpContextFactory
    {
        public DefaultHttpContextFactory(System.IServiceProvider serviceProvider) { }
        public Microsoft.AspNetCore.Http.HttpContext Create(Microsoft.AspNetCore.Http.Features.IFeatureCollection featureCollection) { throw null; }
        public void Dispose(Microsoft.AspNetCore.Http.HttpContext httpContext) { }
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
    public partial class HeaderDictionary : Microsoft.AspNetCore.Http.IHeaderDictionary, System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.Generic.IDictionary<string, Microsoft.Extensions.Primitives.StringValues>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.IEnumerable
    {
        public HeaderDictionary() { }
        public HeaderDictionary(System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues> store) { }
        public HeaderDictionary(int capacity) { }
        public long? ContentLength { get { throw null; } set { } }
        public int Count { get { throw null; } }
        public bool IsReadOnly { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
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
    public partial interface IDefaultHttpContextContainer
    {
        Microsoft.AspNetCore.Http.DefaultHttpContext HttpContext { get; }
    }
    public partial class MiddlewareFactory : Microsoft.AspNetCore.Http.IMiddlewareFactory
    {
        public MiddlewareFactory(System.IServiceProvider serviceProvider) { }
        public Microsoft.AspNetCore.Http.IMiddleware Create(System.Type middlewareType) { throw null; }
        public void Release(Microsoft.AspNetCore.Http.IMiddleware middleware) { }
    }
    public static partial class RequestFormReaderExtensions
    {
        public static System.Threading.Tasks.Task<Microsoft.AspNetCore.Http.IFormCollection> ReadFormAsync(this Microsoft.AspNetCore.Http.HttpRequest request, Microsoft.AspNetCore.Http.Features.FormOptions options, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Http.Features
{
    public partial class DefaultSessionFeature : Microsoft.AspNetCore.Http.Features.ISessionFeature
    {
        public DefaultSessionFeature() { }
        public Microsoft.AspNetCore.Http.ISession Session { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
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
        public bool BufferBody { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public long BufferBodyLengthLimit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int KeyLengthLimit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int MemoryBufferThreshold { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public long MultipartBodyLengthLimit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int MultipartBoundaryLengthLimit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int MultipartHeadersCountLimit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int MultipartHeadersLengthLimit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int ValueCountLimit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int ValueLengthLimit { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class HttpConnectionFeature : Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature
    {
        public HttpConnectionFeature() { }
        public string ConnectionId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Net.IPAddress LocalIpAddress { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int LocalPort { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Net.IPAddress RemoteIpAddress { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int RemotePort { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class HttpRequestFeature : Microsoft.AspNetCore.Http.Features.IHttpRequestFeature
    {
        public HttpRequestFeature() { }
        public System.IO.Stream Body { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.IHeaderDictionary Headers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Method { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Path { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string PathBase { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Protocol { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string QueryString { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string RawTarget { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Scheme { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class HttpRequestIdentifierFeature : Microsoft.AspNetCore.Http.Features.IHttpRequestIdentifierFeature
    {
        public HttpRequestIdentifierFeature() { }
        public string TraceIdentifier { get { throw null; } set { } }
    }
    public partial class HttpRequestLifetimeFeature : Microsoft.AspNetCore.Http.Features.IHttpRequestLifetimeFeature
    {
        public HttpRequestLifetimeFeature() { }
        public System.Threading.CancellationToken RequestAborted { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public void Abort() { }
    }
    public partial class HttpResponseFeature : Microsoft.AspNetCore.Http.Features.IHttpResponseFeature
    {
        public HttpResponseFeature() { }
        public System.IO.Stream Body { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual bool HasStarted { get { throw null; } }
        public Microsoft.AspNetCore.Http.IHeaderDictionary Headers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ReasonPhrase { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int StatusCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public virtual void OnCompleted(System.Func<object, System.Threading.Tasks.Task> callback, object state) { }
        public virtual void OnStarting(System.Func<object, System.Threading.Tasks.Task> callback, object state) { }
    }
    public partial class ItemsFeature : Microsoft.AspNetCore.Http.Features.IItemsFeature
    {
        public ItemsFeature() { }
        public System.Collections.Generic.IDictionary<object, object> Items { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
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
        public System.IO.Pipelines.PipeReader Reader { get { throw null; } set { } }
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
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
    }
    public partial class ResponseBodyPipeFeature : Microsoft.AspNetCore.Http.Features.IResponseBodyPipeFeature
    {
        public ResponseBodyPipeFeature(Microsoft.AspNetCore.Http.HttpContext context) { }
        public System.IO.Pipelines.PipeWriter Writer { get { throw null; } set { } }
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
        public System.IServiceProvider RequestServices { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class TlsConnectionFeature : Microsoft.AspNetCore.Http.Features.ITlsConnectionFeature
    {
        public TlsConnectionFeature() { }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 ClientCertificate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Threading.Tasks.Task<System.Security.Cryptography.X509Certificates.X509Certificate2> GetClientCertificateAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Http.Features.Authentication
{
    public partial class HttpAuthenticationFeature : Microsoft.AspNetCore.Http.Features.Authentication.IHttpAuthenticationFeature
    {
        public HttpAuthenticationFeature() { }
        public System.Security.Claims.ClaimsPrincipal User { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}
namespace Microsoft.AspNetCore.Http.Internal
{
    public partial class BindingAddress
    {
        public BindingAddress() { }
        public string Host { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool IsUnixPipe { get { throw null; } }
        public string PathBase { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public int Port { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Scheme { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string UnixPipePath { get { throw null; } }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static Microsoft.AspNetCore.Http.Internal.BindingAddress Parse(string address) { throw null; }
        public override string ToString() { throw null; }
    }
    public static partial class BufferingHelper
    {
        public static string TempDirectory { get { throw null; } }
        public static Microsoft.AspNetCore.Http.HttpRequest EnableRewind(this Microsoft.AspNetCore.Http.HttpRequest request, int bufferThreshold = 30720, long? bufferLimit = default(long?)) { throw null; }
        public static Microsoft.AspNetCore.WebUtilities.MultipartSection EnableRewind(this Microsoft.AspNetCore.WebUtilities.MultipartSection section, System.Action<System.IDisposable> registerForDispose, int bufferThreshold = 30720, long? bufferLimit = default(long?)) { throw null; }
    }
    public sealed partial class DefaultConnectionInfo : Microsoft.AspNetCore.Http.ConnectionInfo
    {
        public DefaultConnectionInfo(Microsoft.AspNetCore.Http.Features.IFeatureCollection features) { }
        public override System.Security.Cryptography.X509Certificates.X509Certificate2 ClientCertificate { get { throw null; } set { } }
        public override string Id { get { throw null; } set { } }
        public override System.Net.IPAddress LocalIpAddress { get { throw null; } set { } }
        public override int LocalPort { get { throw null; } set { } }
        public override System.Net.IPAddress RemoteIpAddress { get { throw null; } set { } }
        public override int RemotePort { get { throw null; } set { } }
        public override System.Threading.Tasks.Task<System.Security.Cryptography.X509Certificates.X509Certificate2> GetClientCertificateAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public void Initialize(Microsoft.AspNetCore.Http.Features.IFeatureCollection features) { }
        public void Initialize(Microsoft.AspNetCore.Http.Features.IFeatureCollection features, int revision) { }
        public void Uninitialize() { }
    }
    public sealed partial class DefaultHttpRequest : Microsoft.AspNetCore.Http.HttpRequest
    {
        public DefaultHttpRequest(Microsoft.AspNetCore.Http.DefaultHttpContext context) { }
        public override System.IO.Stream Body { get { throw null; } set { } }
        public override System.IO.Pipelines.PipeReader BodyReader { get { throw null; } set { } }
        public override long? ContentLength { get { throw null; } set { } }
        public override string ContentType { get { throw null; } set { } }
        public override Microsoft.AspNetCore.Http.IRequestCookieCollection Cookies { get { throw null; } set { } }
        public override Microsoft.AspNetCore.Http.IFormCollection Form { get { throw null; } set { } }
        public override bool HasFormContentType { get { throw null; } }
        public override Microsoft.AspNetCore.Http.IHeaderDictionary Headers { get { throw null; } }
        public override Microsoft.AspNetCore.Http.HostString Host { get { throw null; } set { } }
        public override Microsoft.AspNetCore.Http.HttpContext HttpContext { get { throw null; } }
        public override bool IsHttps { get { throw null; } set { } }
        public override string Method { get { throw null; } set { } }
        public override Microsoft.AspNetCore.Http.PathString Path { get { throw null; } set { } }
        public override Microsoft.AspNetCore.Http.PathString PathBase { get { throw null; } set { } }
        public override string Protocol { get { throw null; } set { } }
        public override Microsoft.AspNetCore.Http.IQueryCollection Query { get { throw null; } set { } }
        public override Microsoft.AspNetCore.Http.QueryString QueryString { get { throw null; } set { } }
        public override Microsoft.AspNetCore.Routing.RouteValueDictionary RouteValues { get { throw null; } set { } }
        public override string Scheme { get { throw null; } set { } }
        public void Initialize() { }
        public void Initialize(int revision) { }
        public override System.Threading.Tasks.Task<Microsoft.AspNetCore.Http.IFormCollection> ReadFormAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public void Uninitialize() { }
    }
    public sealed partial class DefaultHttpResponse : Microsoft.AspNetCore.Http.HttpResponse
    {
        public DefaultHttpResponse(Microsoft.AspNetCore.Http.DefaultHttpContext context) { }
        public override System.IO.Stream Body { get { throw null; } set { } }
        public override System.IO.Pipelines.PipeWriter BodyWriter { get { throw null; } set { } }
        public override long? ContentLength { get { throw null; } set { } }
        public override string ContentType { get { throw null; } set { } }
        public override Microsoft.AspNetCore.Http.IResponseCookies Cookies { get { throw null; } }
        public override bool HasStarted { get { throw null; } }
        public override Microsoft.AspNetCore.Http.IHeaderDictionary Headers { get { throw null; } }
        public override Microsoft.AspNetCore.Http.HttpContext HttpContext { get { throw null; } }
        public override int StatusCode { get { throw null; } set { } }
        public void Initialize() { }
        public void Initialize(int revision) { }
        public override void OnCompleted(System.Func<object, System.Threading.Tasks.Task> callback, object state) { }
        public override void OnStarting(System.Func<object, System.Threading.Tasks.Task> callback, object state) { }
        public override void Redirect(string location, bool permanent) { }
        public override System.Threading.Tasks.Task StartAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public void Uninitialize() { }
    }
    public sealed partial class DefaultWebSocketManager : Microsoft.AspNetCore.Http.WebSocketManager
    {
        public DefaultWebSocketManager(Microsoft.AspNetCore.Http.Features.IFeatureCollection features) { }
        public override bool IsWebSocketRequest { get { throw null; } }
        public override System.Collections.Generic.IList<string> WebSocketRequestedProtocols { get { throw null; } }
        public override System.Threading.Tasks.Task<System.Net.WebSockets.WebSocket> AcceptWebSocketAsync(string subProtocol) { throw null; }
        public void Initialize(Microsoft.AspNetCore.Http.Features.IFeatureCollection features) { }
        public void Initialize(Microsoft.AspNetCore.Http.Features.IFeatureCollection features, int revision) { }
        public void Uninitialize() { }
    }
    public partial class FormFile : Microsoft.AspNetCore.Http.IFormFile
    {
        public FormFile(System.IO.Stream baseStream, long baseStreamOffset, long length, string name, string fileName) { }
        public string ContentDisposition { get { throw null; } set { } }
        public string ContentType { get { throw null; } set { } }
        public string FileName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Http.IHeaderDictionary Headers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public long Length { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
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
    public partial class ItemsDictionary : System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<object, object>>, System.Collections.Generic.IDictionary<object, object>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<object, object>>, System.Collections.IEnumerable
    {
        public ItemsDictionary() { }
        public ItemsDictionary(System.Collections.Generic.IDictionary<object, object> items) { }
        public System.Collections.Generic.IDictionary<object, object> Items { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.Count { get { throw null; } }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.IsReadOnly { get { throw null; } }
        object System.Collections.Generic.IDictionary<System.Object,System.Object>.this[object key] { get { throw null; } set { } }
        System.Collections.Generic.ICollection<object> System.Collections.Generic.IDictionary<System.Object,System.Object>.Keys { get { throw null; } }
        System.Collections.Generic.ICollection<object> System.Collections.Generic.IDictionary<System.Object,System.Object>.Values { get { throw null; } }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.Add(System.Collections.Generic.KeyValuePair<object, object> item) { }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.Clear() { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.Contains(System.Collections.Generic.KeyValuePair<object, object> item) { throw null; }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.CopyTo(System.Collections.Generic.KeyValuePair<object, object>[] array, int arrayIndex) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.Remove(System.Collections.Generic.KeyValuePair<object, object> item) { throw null; }
        void System.Collections.Generic.IDictionary<System.Object,System.Object>.Add(object key, object value) { }
        bool System.Collections.Generic.IDictionary<System.Object,System.Object>.ContainsKey(object key) { throw null; }
        bool System.Collections.Generic.IDictionary<System.Object,System.Object>.Remove(object key) { throw null; }
        bool System.Collections.Generic.IDictionary<System.Object,System.Object>.TryGetValue(object key, out object value) { throw null; }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<object, object>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public partial class QueryCollection : Microsoft.AspNetCore.Http.IQueryCollection, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.IEnumerable
    {
        public static readonly Microsoft.AspNetCore.Http.Internal.QueryCollection Empty;
        public QueryCollection() { }
        public QueryCollection(Microsoft.AspNetCore.Http.Internal.QueryCollection store) { }
        public QueryCollection(System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues> store) { }
        public QueryCollection(int capacity) { }
        public int Count { get { throw null; } }
        public Microsoft.Extensions.Primitives.StringValues this[string key] { get { throw null; } }
        public System.Collections.Generic.ICollection<string> Keys { get { throw null; } }
        public bool ContainsKey(string key) { throw null; }
        public Microsoft.AspNetCore.Http.Internal.QueryCollection.Enumerator GetEnumerator() { throw null; }
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
    public partial class RequestCookieCollection : Microsoft.AspNetCore.Http.IRequestCookieCollection, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>>, System.Collections.IEnumerable
    {
        public static readonly Microsoft.AspNetCore.Http.Internal.RequestCookieCollection Empty;
        public RequestCookieCollection() { }
        public RequestCookieCollection(System.Collections.Generic.Dictionary<string, string> store) { }
        public RequestCookieCollection(int capacity) { }
        public int Count { get { throw null; } }
        public string this[string key] { get { throw null; } }
        public System.Collections.Generic.ICollection<string> Keys { get { throw null; } }
        public bool ContainsKey(string key) { throw null; }
        public Microsoft.AspNetCore.Http.Internal.RequestCookieCollection.Enumerator GetEnumerator() { throw null; }
        public static Microsoft.AspNetCore.Http.Internal.RequestCookieCollection Parse(System.Collections.Generic.IList<string> values) { throw null; }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, string>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String,System.String>>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public bool TryGetValue(string key, out string value) { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, string>>, System.Collections.IEnumerator, System.IDisposable
        {
            private object _dummy;
            private int _dummyPrimitive;
            public System.Collections.Generic.KeyValuePair<string, string> Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            public void Reset() { }
        }
    }
    public partial class ResponseCookies : Microsoft.AspNetCore.Http.IResponseCookies
    {
        public ResponseCookies(Microsoft.AspNetCore.Http.IHeaderDictionary headers, Microsoft.Extensions.ObjectPool.ObjectPool<System.Text.StringBuilder> builderPool) { }
        public void Append(string key, string value) { }
        public void Append(string key, string value, Microsoft.AspNetCore.Http.CookieOptions options) { }
        public void Delete(string key) { }
        public void Delete(string key, Microsoft.AspNetCore.Http.CookieOptions options) { }
    }
}
namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class HttpServiceCollectionExtensions
    {
        public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddHttpContextAccessor(this Microsoft.Extensions.DependencyInjection.IServiceCollection services) { throw null; }
    }
}
namespace System.IO.Pipelines
{
    public partial class ReadOnlyPipeStream : System.IO.Stream
    {
        public ReadOnlyPipeStream(System.IO.Pipelines.PipeReader pipeReader) { }
        public ReadOnlyPipeStream(System.IO.Pipelines.PipeReader pipeReader, bool allowSynchronousIO) { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public System.IO.Pipelines.PipeReader InnerPipeReader { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public override int WriteTimeout { get { throw null; } set { } }
        public override System.IAsyncResult BeginRead(byte[] buffer, int offset, int count, System.AsyncCallback callback, object state) { throw null; }
        public override System.Threading.Tasks.Task CopyToAsync(System.IO.Stream destination, int bufferSize, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override int EndRead(System.IAsyncResult asyncResult) { throw null; }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.ValueTask<int> ReadAsync(System.Memory<byte> destination, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public partial class StreamPipeReader : System.IO.Pipelines.PipeReader, System.IDisposable
    {
        public StreamPipeReader(System.IO.Stream readingStream) { }
        public StreamPipeReader(System.IO.Stream readingStream, System.IO.Pipelines.StreamPipeReaderOptions options) { }
        public System.IO.Stream InnerStream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public override void AdvanceTo(System.SequencePosition consumed) { }
        public override void AdvanceTo(System.SequencePosition consumed, System.SequencePosition examined) { }
        public override void CancelPendingRead() { }
        public override void Complete(System.Exception exception = null) { }
        public void Dispose() { }
        public override void OnWriterCompleted(System.Action<System.Exception, object> callback, object state) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> ReadAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override bool TryRead(out System.IO.Pipelines.ReadResult result) { throw null; }
    }
    public partial class StreamPipeReaderOptions
    {
        public const int DefaultMinimumReadThreshold = 256;
        public const int DefaultMinimumSegmentSize = 4096;
        public static System.IO.Pipelines.StreamPipeReaderOptions DefaultOptions;
        public StreamPipeReaderOptions() { }
        public StreamPipeReaderOptions(int minimumSegmentSize, int minimumReadThreshold, System.Buffers.MemoryPool<byte> memoryPool) { }
        public System.Buffers.MemoryPool<byte> MemoryPool { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int MinimumReadThreshold { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int MinimumSegmentSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    public partial class StreamPipeWriter : System.IO.Pipelines.PipeWriter, System.IDisposable
    {
        public StreamPipeWriter(System.IO.Stream writingStream) { }
        public StreamPipeWriter(System.IO.Stream writingStream, int minimumSegmentSize, System.Buffers.MemoryPool<byte> pool = null) { }
        public System.IO.Stream InnerStream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public override void Advance(int count) { }
        public override void CancelPendingFlush() { }
        public override void Complete(System.Exception exception = null) { }
        public void Dispose() { }
        public override System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FlushAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Memory<byte> GetMemory(int sizeHint = 0) { throw null; }
        public override System.Span<byte> GetSpan(int sizeHint = 0) { throw null; }
        public override void OnReaderCompleted(System.Action<System.Exception, object> callback, object state) { }
    }
    public partial class WriteOnlyPipeStream : System.IO.Stream
    {
        public WriteOnlyPipeStream(System.IO.Pipelines.PipeWriter pipeWriter) { }
        public WriteOnlyPipeStream(System.IO.Pipelines.PipeWriter pipeWriter, bool allowSynchronousIO) { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public System.IO.Pipelines.PipeWriter InnerPipeWriter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public override int ReadTimeout { get { throw null; } set { } }
        public override System.IAsyncResult BeginWrite(byte[] buffer, int offset, int count, System.AsyncCallback callback, object state) { throw null; }
        public override void EndWrite(System.IAsyncResult asyncResult) { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.ValueTask WriteAsync(System.ReadOnlyMemory<byte> source, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
}
