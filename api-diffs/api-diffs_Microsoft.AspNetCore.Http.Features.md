# Microsoft.AspNetCore.Http.Features

``` diff
 namespace Microsoft.AspNetCore.Http.Features {
     public class DefaultSessionFeature : ISessionFeature {
         public DefaultSessionFeature();
         public ISession Session { get; set; }
     }
     public class FeatureCollection : IEnumerable, IEnumerable<KeyValuePair<Type, object>>, IFeatureCollection {
         public FeatureCollection();
         public FeatureCollection(IFeatureCollection defaults);
         public bool IsReadOnly { get; }
         public virtual int Revision { get; }
         public object this[Type key] { get; set; }
         public TFeature Get<TFeature>();
         public IEnumerator<KeyValuePair<Type, object>> GetEnumerator();
         public void Set<TFeature>(TFeature instance);
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
     }
     public struct FeatureReference<T> {
         public static readonly FeatureReference<T> Default;
         public T Fetch(IFeatureCollection features);
         public T Update(IFeatureCollection features, T feature);
     }
     public struct FeatureReferences<TCache> {
         public TCache Cache;
         public FeatureReferences(IFeatureCollection collection);
         public IFeatureCollection Collection { get; private set; }
         public int Revision { get; private set; }
         public TFeature Fetch<TFeature, TState>(ref TFeature cached, TState state, Func<TState, TFeature> factory) where TFeature : class;
         public TFeature Fetch<TFeature>(ref TFeature cached, Func<IFeatureCollection, TFeature> factory) where TFeature : class;
+        public void Initalize(IFeatureCollection collection);
+        public void Initalize(IFeatureCollection collection, int revision);
     }
     public class FormFeature : IFormFeature {
         public FormFeature(HttpRequest request);
         public FormFeature(HttpRequest request, FormOptions options);
         public FormFeature(IFormCollection form);
         public IFormCollection Form { get; set; }
         public bool HasFormContentType { get; }
         public IFormCollection ReadForm();
         public Task<IFormCollection> ReadFormAsync();
         public Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken);
     }
     public class FormOptions {
         public const int DefaultBufferBodyLengthLimit = 134217728;
         public const int DefaultMemoryBufferThreshold = 65536;
         public const int DefaultMultipartBoundaryLengthLimit = 128;
         public const long DefaultMultipartBodyLengthLimit = (long)134217728;
         public FormOptions();
         public bool BufferBody { get; set; }
         public long BufferBodyLengthLimit { get; set; }
         public int KeyLengthLimit { get; set; }
         public int MemoryBufferThreshold { get; set; }
         public long MultipartBodyLengthLimit { get; set; }
         public int MultipartBoundaryLengthLimit { get; set; }
         public int MultipartHeadersCountLimit { get; set; }
         public int MultipartHeadersLengthLimit { get; set; }
         public int ValueCountLimit { get; set; }
         public int ValueLengthLimit { get; set; }
     }
     public class HttpConnectionFeature : IHttpConnectionFeature {
         public HttpConnectionFeature();
         public string ConnectionId { get; set; }
         public IPAddress LocalIpAddress { get; set; }
         public int LocalPort { get; set; }
         public IPAddress RemoteIpAddress { get; set; }
         public int RemotePort { get; set; }
     }
     public class HttpRequestFeature : IHttpRequestFeature {
         public HttpRequestFeature();
         public Stream Body { get; set; }
         public IHeaderDictionary Headers { get; set; }
         public string Method { get; set; }
         public string Path { get; set; }
         public string PathBase { get; set; }
         public string Protocol { get; set; }
         public string QueryString { get; set; }
         public string RawTarget { get; set; }
         public string Scheme { get; set; }
     }
     public class HttpRequestIdentifierFeature : IHttpRequestIdentifierFeature {
         public HttpRequestIdentifierFeature();
         public string TraceIdentifier { get; set; }
     }
     public class HttpRequestLifetimeFeature : IHttpRequestLifetimeFeature {
         public HttpRequestLifetimeFeature();
         public CancellationToken RequestAborted { get; set; }
         public void Abort();
     }
     public class HttpResponseFeature : IHttpResponseFeature {
         public HttpResponseFeature();
         public Stream Body { get; set; }
         public virtual bool HasStarted { get; }
         public IHeaderDictionary Headers { get; set; }
         public string ReasonPhrase { get; set; }
         public int StatusCode { get; set; }
         public virtual void OnCompleted(Func<object, Task> callback, object state);
         public virtual void OnStarting(Func<object, Task> callback, object state);
     }
+    public enum HttpsCompressionMode {
+        Compress = 2,
+        Default = 0,
+        DoNotCompress = 1,
+    }
     public interface IEndpointFeature {
         Endpoint Endpoint { get; set; }
     }
     public interface IFeatureCollection : IEnumerable, IEnumerable<KeyValuePair<Type, object>> {
         bool IsReadOnly { get; }
         int Revision { get; }
         object this[Type key] { get; set; }
         TFeature Get<TFeature>();
         void Set<TFeature>(TFeature instance);
     }
     public interface IFormFeature {
         IFormCollection Form { get; set; }
         bool HasFormContentType { get; }
         IFormCollection ReadForm();
         Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken);
     }
     public interface IHttpBodyControlFeature {
         bool AllowSynchronousIO { get; set; }
     }
     public interface IHttpBufferingFeature {
         void DisableRequestBuffering();
         void DisableResponseBuffering();
     }
     public interface IHttpConnectionFeature {
         string ConnectionId { get; set; }
         IPAddress LocalIpAddress { get; set; }
         int LocalPort { get; set; }
         IPAddress RemoteIpAddress { get; set; }
         int RemotePort { get; set; }
     }
     public interface IHttpMaxRequestBodySizeFeature {
         bool IsReadOnly { get; }
         Nullable<long> MaxRequestBodySize { get; set; }
     }
     public interface IHttpRequestFeature {
         Stream Body { get; set; }
         IHeaderDictionary Headers { get; set; }
         string Method { get; set; }
         string Path { get; set; }
         string PathBase { get; set; }
         string Protocol { get; set; }
         string QueryString { get; set; }
         string RawTarget { get; set; }
         string Scheme { get; set; }
     }
     public interface IHttpRequestIdentifierFeature {
         string TraceIdentifier { get; set; }
     }
     public interface IHttpRequestLifetimeFeature {
         CancellationToken RequestAborted { get; set; }
         void Abort();
     }
+    public interface IHttpRequestTrailersFeature {
+        bool Available { get; }
+        IHeaderDictionary Trailers { get; }
+    }
     public interface IHttpResponseFeature {
         Stream Body { get; set; }
         bool HasStarted { get; }
         IHeaderDictionary Headers { get; set; }
         string ReasonPhrase { get; set; }
         int StatusCode { get; set; }
         void OnCompleted(Func<object, Task> callback, object state);
         void OnStarting(Func<object, Task> callback, object state);
     }
+    public interface IHttpResponseStartFeature {
+        Task StartAsync(CancellationToken token = default(CancellationToken));
+    }
     public interface IHttpResponseTrailersFeature {
         IHeaderDictionary Trailers { get; set; }
     }
+    public interface IHttpsCompressionFeature {
+        HttpsCompressionMode Mode { get; set; }
+    }
     public interface IHttpSendFileFeature {
         Task SendFileAsync(string path, long offset, Nullable<long> count, CancellationToken cancellation);
     }
     public interface IHttpUpgradeFeature {
         bool IsUpgradableRequest { get; }
         Task<Stream> UpgradeAsync();
     }
     public interface IHttpWebSocketFeature {
         bool IsWebSocketRequest { get; }
         Task<WebSocket> AcceptAsync(WebSocketAcceptContext context);
     }
     public interface IItemsFeature {
         IDictionary<object, object> Items { get; set; }
     }
     public interface IQueryFeature {
         IQueryCollection Query { get; set; }
     }
+    public interface IRequestBodyPipeFeature {
+        PipeReader Reader { get; }
+    }
     public interface IRequestCookiesFeature {
         IRequestCookieCollection Cookies { get; set; }
     }
+    public interface IResponseBodyPipeFeature {
+        PipeWriter Writer { get; }
+    }
     public interface IResponseCookiesFeature {
         IResponseCookies Cookies { get; }
     }
     public interface IRouteValuesFeature {
         RouteValueDictionary RouteValues { get; set; }
     }
     public interface IServerVariablesFeature {
         string this[string variableName] { get; set; }
     }
     public interface IServiceProvidersFeature {
         IServiceProvider RequestServices { get; set; }
     }
     public interface ISessionFeature {
         ISession Session { get; set; }
     }
     public class ItemsFeature : IItemsFeature {
         public ItemsFeature();
         public IDictionary<object, object> Items { get; set; }
     }
     public interface ITlsConnectionFeature {
         X509Certificate2 ClientCertificate { get; set; }
         Task<X509Certificate2> GetClientCertificateAsync(CancellationToken cancellationToken);
     }
     public interface ITlsTokenBindingFeature {
         byte[] GetProvidedTokenBindingId();
         byte[] GetReferredTokenBindingId();
     }
     public interface ITrackingConsentFeature {
         bool CanTrack { get; }
         bool HasConsent { get; }
         bool IsConsentNeeded { get; }
         string CreateConsentCookie();
         void GrantConsent();
         void WithdrawConsent();
     }
     public class QueryFeature : IQueryFeature {
         public QueryFeature(IFeatureCollection features);
         public QueryFeature(IQueryCollection query);
         public IQueryCollection Query { get; set; }
     }
+    public class RequestBodyPipeFeature : IRequestBodyPipeFeature {
+        public RequestBodyPipeFeature(HttpContext context);
+        public PipeReader Reader { get; }
+    }
     public class RequestCookiesFeature : IRequestCookiesFeature {
         public RequestCookiesFeature(IFeatureCollection features);
         public RequestCookiesFeature(IRequestCookieCollection cookies);
         public IRequestCookieCollection Cookies { get; set; }
     }
+    public class RequestServicesFeature : IAsyncDisposable, IDisposable, IServiceProvidersFeature {
+        public RequestServicesFeature(HttpContext context, IServiceScopeFactory scopeFactory);
+        public IServiceProvider RequestServices { get; set; }
+        public void Dispose();
+        public ValueTask DisposeAsync();
+    }
+    public class ResponseBodyPipeFeature : IResponseBodyPipeFeature {
+        public ResponseBodyPipeFeature(HttpContext context);
+        public PipeWriter Writer { get; }
+    }
     public class ResponseCookiesFeature : IResponseCookiesFeature {
         public ResponseCookiesFeature(IFeatureCollection features);
         public ResponseCookiesFeature(IFeatureCollection features, ObjectPool<StringBuilder> builderPool);
         public IResponseCookies Cookies { get; }
     }
+    public class RouteValuesFeature : IRouteValuesFeature {
+        public RouteValuesFeature();
+        public RouteValueDictionary RouteValues { get; set; }
+    }
     public class ServiceProvidersFeature : IServiceProvidersFeature {
         public ServiceProvidersFeature();
         public IServiceProvider RequestServices { get; set; }
     }
     public class TlsConnectionFeature : ITlsConnectionFeature {
         public TlsConnectionFeature();
         public X509Certificate2 ClientCertificate { get; set; }
         public Task<X509Certificate2> GetClientCertificateAsync(CancellationToken cancellationToken);
     }
 }
```

