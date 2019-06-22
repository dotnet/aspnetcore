# Microsoft.AspNetCore.Http

``` diff
 namespace Microsoft.AspNetCore.Http {
+    public class BindingAddress {
+        public BindingAddress();
+        public string Host { get; }
+        public bool IsUnixPipe { get; }
+        public string PathBase { get; }
+        public int Port { get; }
+        public string Scheme { get; }
+        public string UnixPipePath { get; }
+        public override bool Equals(object obj);
+        public override int GetHashCode();
+        public static BindingAddress Parse(string address);
+        public override string ToString();
+    }
     public abstract class ConnectionInfo {
         protected ConnectionInfo();
         public abstract X509Certificate2 ClientCertificate { get; set; }
         public abstract string Id { get; set; }
         public abstract IPAddress LocalIpAddress { get; set; }
         public abstract int LocalPort { get; set; }
         public abstract IPAddress RemoteIpAddress { get; set; }
         public abstract int RemotePort { get; set; }
         public abstract Task<X509Certificate2> GetClientCertificateAsync(CancellationToken cancellationToken = default(CancellationToken));
     }
     public class CookieBuilder {
         public CookieBuilder();
         public virtual string Domain { get; set; }
         public virtual Nullable<TimeSpan> Expiration { get; set; }
         public virtual bool HttpOnly { get; set; }
         public virtual bool IsEssential { get; set; }
         public virtual Nullable<TimeSpan> MaxAge { get; set; }
         public virtual string Name { get; set; }
         public virtual string Path { get; set; }
         public virtual SameSiteMode SameSite { get; set; }
         public virtual CookieSecurePolicy SecurePolicy { get; set; }
         public CookieOptions Build(HttpContext context);
         public virtual CookieOptions Build(HttpContext context, DateTimeOffset expiresFrom);
     }
     public class CookieOptions {
         public CookieOptions();
         public string Domain { get; set; }
         public Nullable<DateTimeOffset> Expires { get; set; }
         public bool HttpOnly { get; set; }
         public bool IsEssential { get; set; }
         public Nullable<TimeSpan> MaxAge { get; set; }
         public string Path { get; set; }
         public SameSiteMode SameSite { get; set; }
         public bool Secure { get; set; }
     }
     public enum CookieSecurePolicy {
         Always = 1,
         None = 2,
         SameAsRequest = 0,
     }
-    public class DefaultHttpContext : HttpContext {
+    public sealed class DefaultHttpContext : HttpContext {
         public DefaultHttpContext();
         public DefaultHttpContext(IFeatureCollection features);
-        public override AuthenticationManager Authentication { get; }

         public override ConnectionInfo Connection { get; }
         public override IFeatureCollection Features { get; }
+        public FormOptions FormOptions { get; set; }
+        public HttpContext HttpContext { get; }
         public override IDictionary<object, object> Items { get; set; }
         public override HttpRequest Request { get; }
         public override CancellationToken RequestAborted { get; set; }
         public override IServiceProvider RequestServices { get; set; }
         public override HttpResponse Response { get; }
+        public IServiceScopeFactory ServiceScopeFactory { get; set; }
         public override ISession Session { get; set; }
         public override string TraceIdentifier { get; set; }
         public override ClaimsPrincipal User { get; set; }
         public override WebSocketManager WebSockets { get; }
         public override void Abort();
-        public virtual void Initialize(IFeatureCollection features);
+        public void Initialize(IFeatureCollection features);
-        protected virtual AuthenticationManager InitializeAuthenticationManager();

-        protected virtual ConnectionInfo InitializeConnectionInfo();

-        protected virtual HttpRequest InitializeHttpRequest();

-        protected virtual HttpResponse InitializeHttpResponse();

-        protected virtual WebSocketManager InitializeWebSocketManager();

-        public virtual void Uninitialize();
+        public void Uninitialize();
-        protected virtual void UninitializeAuthenticationManager(AuthenticationManager instance);

-        protected virtual void UninitializeConnectionInfo(ConnectionInfo instance);

-        protected virtual void UninitializeHttpRequest(HttpRequest instance);

-        protected virtual void UninitializeHttpResponse(HttpResponse instance);

-        protected virtual void UninitializeWebSocketManager(WebSocketManager instance);

     }
+    public class DefaultHttpContextFactory : IHttpContextFactory {
+        public DefaultHttpContextFactory(IServiceProvider serviceProvider);
+        public HttpContext Create(IFeatureCollection featureCollection);
+        public void Dispose(HttpContext httpContext);
+    }
     public class Endpoint {
         public Endpoint(RequestDelegate requestDelegate, EndpointMetadataCollection metadata, string displayName);
         public string DisplayName { get; }
         public EndpointMetadataCollection Metadata { get; }
         public RequestDelegate RequestDelegate { get; }
         public override string ToString();
     }
+    public static class EndpointHttpContextExtensions {
+        public static Endpoint GetEndpoint(this HttpContext context);
+        public static void SetEndpoint(this HttpContext context, Endpoint endpoint);
+    }
     public sealed class EndpointMetadataCollection : IEnumerable, IEnumerable<object>, IReadOnlyCollection<object>, IReadOnlyList<object> {
         public static readonly EndpointMetadataCollection Empty;
         public EndpointMetadataCollection(IEnumerable<object> items);
         public EndpointMetadataCollection(params object[] items);
         public int Count { get; }
         public object this[int index] { get; }
         public EndpointMetadataCollection.Enumerator GetEnumerator();
         public T GetMetadata<T>() where T : class;
-        public IEnumerable<T> GetOrderedMetadata<T>() where T : class;

+        public IReadOnlyList<T> GetOrderedMetadata<T>() where T : class;
         IEnumerator<object> System.Collections.Generic.IEnumerable<System.Object>.GetEnumerator();
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
         public struct Enumerator : IDisposable, IEnumerator, IEnumerator<object> {
             public object Current { get; private set; }
             public void Dispose();
             public bool MoveNext();
             public void Reset();
         }
     }
     public class FormCollection : IEnumerable, IEnumerable<KeyValuePair<string, StringValues>>, IFormCollection {
         public static readonly FormCollection Empty;
         public FormCollection(Dictionary<string, StringValues> fields, IFormFileCollection files = null);
         public int Count { get; }
         public IFormFileCollection Files { get; private set; }
         public ICollection<string> Keys { get; }
         public StringValues this[string key] { get; }
         public bool ContainsKey(string key);
         public FormCollection.Enumerator GetEnumerator();
         IEnumerator<KeyValuePair<string, StringValues>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.GetEnumerator();
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
         public bool TryGetValue(string key, out StringValues value);
         public struct Enumerator : IDisposable, IEnumerator, IEnumerator<KeyValuePair<string, StringValues>> {
             public KeyValuePair<string, StringValues> Current { get; }
             object System.Collections.IEnumerator.Current { get; }
             public void Dispose();
             public bool MoveNext();
             void System.Collections.IEnumerator.Reset();
         }
     }
+    public class FormFile : IFormFile {
+        public FormFile(Stream baseStream, long baseStreamOffset, long length, string name, string fileName);
+        public string ContentDisposition { get; set; }
+        public string ContentType { get; set; }
+        public string FileName { get; }
+        public IHeaderDictionary Headers { get; set; }
+        public long Length { get; }
+        public string Name { get; }
+        public void CopyTo(Stream target);
+        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default(CancellationToken));
+        public Stream OpenReadStream();
+    }
+    public class FormFileCollection : List<IFormFile>, IEnumerable, IEnumerable<IFormFile>, IFormFileCollection, IReadOnlyCollection<IFormFile>, IReadOnlyList<IFormFile> {
+        public FormFileCollection();
+        public IFormFile this[string name] { get; }
+        public IFormFile GetFile(string name);
+        public IReadOnlyList<IFormFile> GetFiles(string name);
+    }
-    public struct FragmentString : IEquatable<FragmentString> {
+    public readonly struct FragmentString : IEquatable<FragmentString> {
         public static readonly FragmentString Empty;
         public FragmentString(string value);
         public bool HasValue { get; }
         public string Value { get; }
         public bool Equals(FragmentString other);
         public override bool Equals(object obj);
         public static FragmentString FromUriComponent(string uriComponent);
         public static FragmentString FromUriComponent(Uri uri);
         public override int GetHashCode();
         public static bool operator ==(FragmentString left, FragmentString right);
         public static bool operator !=(FragmentString left, FragmentString right);
         public override string ToString();
         public string ToUriComponent();
     }
     public class HeaderDictionary : ICollection<KeyValuePair<string, StringValues>>, IDictionary<string, StringValues>, IEnumerable, IEnumerable<KeyValuePair<string, StringValues>>, IHeaderDictionary {
         public HeaderDictionary();
         public HeaderDictionary(Dictionary<string, StringValues> store);
         public HeaderDictionary(int capacity);
         public Nullable<long> ContentLength { get; set; }
         public int Count { get; }
         public bool IsReadOnly { get; set; }
         public ICollection<string> Keys { get; }
         StringValues System.Collections.Generic.IDictionary<System.String,Microsoft.Extensions.Primitives.StringValues>.this[string key] { get; set; }
         public StringValues this[string key] { get; set; }
         public ICollection<StringValues> Values { get; }
         public void Add(KeyValuePair<string, StringValues> item);
         public void Add(string key, StringValues value);
         public void Clear();
         public bool Contains(KeyValuePair<string, StringValues> item);
         public bool ContainsKey(string key);
         public void CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex);
         public HeaderDictionary.Enumerator GetEnumerator();
         public bool Remove(KeyValuePair<string, StringValues> item);
         public bool Remove(string key);
         IEnumerator<KeyValuePair<string, StringValues>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.GetEnumerator();
         IEnumerator System.Collections.IEnumerable.GetEnumerator();
         public bool TryGetValue(string key, out StringValues value);
         public struct Enumerator : IDisposable, IEnumerator, IEnumerator<KeyValuePair<string, StringValues>> {
             public KeyValuePair<string, StringValues> Current { get; }
             object System.Collections.IEnumerator.Current { get; }
             public void Dispose();
             public bool MoveNext();
             void System.Collections.IEnumerator.Reset();
         }
     }
     public static class HeaderDictionaryExtensions {
         public static void Append(this IHeaderDictionary headers, string key, StringValues value);
         public static void AppendCommaSeparatedValues(this IHeaderDictionary headers, string key, params string[] values);
         public static string[] GetCommaSeparatedValues(this IHeaderDictionary headers, string key);
         public static void SetCommaSeparatedValues(this IHeaderDictionary headers, string key, params string[] values);
     }
     public static class HeaderDictionaryTypeExtensions {
         public static void AppendList<T>(this IHeaderDictionary Headers, string name, IList<T> values);
         public static RequestHeaders GetTypedHeaders(this HttpRequest request);
         public static ResponseHeaders GetTypedHeaders(this HttpResponse response);
     }
-    public struct HostString : IEquatable<HostString> {
+    public readonly struct HostString : IEquatable<HostString> {
         public HostString(string value);
         public HostString(string host, int port);
         public bool HasValue { get; }
         public string Host { get; }
         public Nullable<int> Port { get; }
         public string Value { get; }
         public bool Equals(HostString other);
         public override bool Equals(object obj);
         public static HostString FromUriComponent(string uriComponent);
         public static HostString FromUriComponent(Uri uri);
         public override int GetHashCode();
         public static bool MatchesAny(StringSegment value, IList<StringSegment> patterns);
         public static bool operator ==(HostString left, HostString right);
         public static bool operator !=(HostString left, HostString right);
         public override string ToString();
         public string ToUriComponent();
     }
     public abstract class HttpContext {
         protected HttpContext();
-        public abstract AuthenticationManager Authentication { get; }

         public abstract ConnectionInfo Connection { get; }
         public abstract IFeatureCollection Features { get; }
         public abstract IDictionary<object, object> Items { get; set; }
         public abstract HttpRequest Request { get; }
         public abstract CancellationToken RequestAborted { get; set; }
         public abstract IServiceProvider RequestServices { get; set; }
         public abstract HttpResponse Response { get; }
         public abstract ISession Session { get; set; }
         public abstract string TraceIdentifier { get; set; }
         public abstract ClaimsPrincipal User { get; set; }
         public abstract WebSocketManager WebSockets { get; }
         public abstract void Abort();
     }
     public class HttpContextAccessor : IHttpContextAccessor {
         public HttpContextAccessor();
         public HttpContext HttpContext { get; set; }
     }
     public class HttpContextFactory : IHttpContextFactory {
         public HttpContextFactory(IOptions<FormOptions> formOptions);
         public HttpContextFactory(IOptions<FormOptions> formOptions, IHttpContextAccessor httpContextAccessor);
+        public HttpContextFactory(IOptions<FormOptions> formOptions, IServiceScopeFactory serviceScopeFactory);
+        public HttpContextFactory(IOptions<FormOptions> formOptions, IServiceScopeFactory serviceScopeFactory, IHttpContextAccessor httpContextAccessor);
         public HttpContext Create(IFeatureCollection featureCollection);
         public void Dispose(HttpContext httpContext);
     }
+    public static class HttpContextServerVariableExtensions {
+        public static string GetServerVariable(this HttpContext context, string variableName);
+    }
     public static class HttpMethods {
         public static readonly string Connect;
         public static readonly string Delete;
         public static readonly string Get;
         public static readonly string Head;
         public static readonly string Options;
         public static readonly string Patch;
         public static readonly string Post;
         public static readonly string Put;
         public static readonly string Trace;
         public static bool IsConnect(string method);
         public static bool IsDelete(string method);
         public static bool IsGet(string method);
         public static bool IsHead(string method);
         public static bool IsOptions(string method);
         public static bool IsPatch(string method);
         public static bool IsPost(string method);
         public static bool IsPut(string method);
         public static bool IsTrace(string method);
     }
     public abstract class HttpRequest {
         protected HttpRequest();
         public abstract Stream Body { get; set; }
+        public virtual PipeReader BodyReader { get; }
         public abstract Nullable<long> ContentLength { get; set; }
         public abstract string ContentType { get; set; }
         public abstract IRequestCookieCollection Cookies { get; set; }
         public abstract IFormCollection Form { get; set; }
         public abstract bool HasFormContentType { get; }
         public abstract IHeaderDictionary Headers { get; }
         public abstract HostString Host { get; set; }
         public abstract HttpContext HttpContext { get; }
         public abstract bool IsHttps { get; set; }
         public abstract string Method { get; set; }
         public abstract PathString Path { get; set; }
         public abstract PathString PathBase { get; set; }
         public abstract string Protocol { get; set; }
         public abstract IQueryCollection Query { get; set; }
         public abstract QueryString QueryString { get; set; }
+        public virtual RouteValueDictionary RouteValues { get; set; }
         public abstract string Scheme { get; set; }
         public abstract Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken = default(CancellationToken));
     }
     public static class HttpRequestRewindExtensions {
         public static void EnableBuffering(this HttpRequest request);
         public static void EnableBuffering(this HttpRequest request, int bufferThreshold);
         public static void EnableBuffering(this HttpRequest request, int bufferThreshold, long bufferLimit);
         public static void EnableBuffering(this HttpRequest request, long bufferLimit);
     }
     public abstract class HttpResponse {
         protected HttpResponse();
         public abstract Stream Body { get; set; }
+        public virtual PipeWriter BodyWriter { get; }
         public abstract Nullable<long> ContentLength { get; set; }
         public abstract string ContentType { get; set; }
         public abstract IResponseCookies Cookies { get; }
         public abstract bool HasStarted { get; }
         public abstract IHeaderDictionary Headers { get; }
         public abstract HttpContext HttpContext { get; }
         public abstract int StatusCode { get; set; }
         public abstract void OnCompleted(Func<object, Task> callback, object state);
         public virtual void OnCompleted(Func<Task> callback);
         public abstract void OnStarting(Func<object, Task> callback, object state);
         public virtual void OnStarting(Func<Task> callback);
         public virtual void Redirect(string location);
         public abstract void Redirect(string location, bool permanent);
         public virtual void RegisterForDispose(IDisposable disposable);
+        public virtual void RegisterForDisposeAsync(IAsyncDisposable disposable);
+        public virtual Task StartAsync(CancellationToken cancellationToken = default(CancellationToken));
     }
     public static class HttpResponseWritingExtensions {
         public static Task WriteAsync(this HttpResponse response, string text, Encoding encoding, CancellationToken cancellationToken = default(CancellationToken));
         public static Task WriteAsync(this HttpResponse response, string text, CancellationToken cancellationToken = default(CancellationToken));
     }
+    public interface IDefaultHttpContextContainer {
+        DefaultHttpContext HttpContext { get; }
+    }
     public interface IFormCollection : IEnumerable, IEnumerable<KeyValuePair<string, StringValues>> {
         int Count { get; }
         IFormFileCollection Files { get; }
         ICollection<string> Keys { get; }
         StringValues this[string key] { get; }
         bool ContainsKey(string key);
         bool TryGetValue(string key, out StringValues value);
     }
     public interface IFormFile {
         string ContentDisposition { get; }
         string ContentType { get; }
         string FileName { get; }
         IHeaderDictionary Headers { get; }
         long Length { get; }
         string Name { get; }
         void CopyTo(Stream target);
         Task CopyToAsync(Stream target, CancellationToken cancellationToken = default(CancellationToken));
         Stream OpenReadStream();
     }
     public interface IFormFileCollection : IEnumerable, IEnumerable<IFormFile>, IReadOnlyCollection<IFormFile>, IReadOnlyList<IFormFile> {
         IFormFile this[string name] { get; }
         IFormFile GetFile(string name);
         IReadOnlyList<IFormFile> GetFiles(string name);
     }
     public interface IHeaderDictionary : ICollection<KeyValuePair<string, StringValues>>, IDictionary<string, StringValues>, IEnumerable, IEnumerable<KeyValuePair<string, StringValues>> {
         Nullable<long> ContentLength { get; set; }
         StringValues this[string key] { get; set; }
     }
     public interface IHttpContextAccessor {
         HttpContext HttpContext { get; set; }
     }
     public interface IHttpContextFactory {
         HttpContext Create(IFeatureCollection featureCollection);
         void Dispose(HttpContext httpContext);
     }
     public interface IMiddleware {
         Task InvokeAsync(HttpContext context, RequestDelegate next);
     }
     public interface IMiddlewareFactory {
         IMiddleware Create(Type middlewareType);
         void Release(IMiddleware middleware);
     }
     public interface IQueryCollection : IEnumerable, IEnumerable<KeyValuePair<string, StringValues>> {
         int Count { get; }
         ICollection<string> Keys { get; }
         StringValues this[string key] { get; }
         bool ContainsKey(string key);
         bool TryGetValue(string key, out StringValues value);
     }
     public interface IRequestCookieCollection : IEnumerable, IEnumerable<KeyValuePair<string, string>> {
         int Count { get; }
         ICollection<string> Keys { get; }
         string this[string key] { get; }
         bool ContainsKey(string key);
         bool TryGetValue(string key, out string value);
     }
     public interface IResponseCookies {
         void Append(string key, string value);
         void Append(string key, string value, CookieOptions options);
         void Delete(string key);
         void Delete(string key, CookieOptions options);
     }
     public interface ISession {
         string Id { get; }
         bool IsAvailable { get; }
         IEnumerable<string> Keys { get; }
         void Clear();
         Task CommitAsync(CancellationToken cancellationToken = default(CancellationToken));
         Task LoadAsync(CancellationToken cancellationToken = default(CancellationToken));
         void Remove(string key);
         void Set(string key, byte[] value);
         bool TryGetValue(string key, out byte[] value);
     }
     public class MiddlewareFactory : IMiddlewareFactory {
         public MiddlewareFactory(IServiceProvider serviceProvider);
         public IMiddleware Create(Type middlewareType);
         public void Release(IMiddleware middleware);
     }
-    public struct PathString : IEquatable<PathString> {
+    public readonly struct PathString : IEquatable<PathString> {
         public static readonly PathString Empty;
         public PathString(string value);
         public bool HasValue { get; }
         public string Value { get; }
         public PathString Add(PathString other);
         public string Add(QueryString other);
         public bool Equals(PathString other);
         public bool Equals(PathString other, StringComparison comparisonType);
         public override bool Equals(object obj);
         public static PathString FromUriComponent(string uriComponent);
         public static PathString FromUriComponent(Uri uri);
         public override int GetHashCode();
         public static PathString operator +(PathString left, PathString right);
         public static string operator +(PathString left, QueryString right);
         public static string operator +(PathString left, string right);
         public static string operator +(string left, PathString right);
         public static bool operator ==(PathString left, PathString right);
         public static implicit operator string (PathString path);
         public static implicit operator PathString (string s);
         public static bool operator !=(PathString left, PathString right);
         public bool StartsWithSegments(PathString other);
         public bool StartsWithSegments(PathString other, out PathString remaining);
         public bool StartsWithSegments(PathString other, out PathString matched, out PathString remaining);
         public bool StartsWithSegments(PathString other, StringComparison comparisonType);
         public bool StartsWithSegments(PathString other, StringComparison comparisonType, out PathString remaining);
         public bool StartsWithSegments(PathString other, StringComparison comparisonType, out PathString matched, out PathString remaining);
         public override string ToString();
         public string ToUriComponent();
     }
+    public class QueryCollection : IEnumerable, IEnumerable<KeyValuePair<string, StringValues>>, IQueryCollection {
+        public static readonly QueryCollection Empty;
+        public QueryCollection();
+        public QueryCollection(QueryCollection store);
+        public QueryCollection(Dictionary<string, StringValues> store);
+        public QueryCollection(int capacity);
+        public int Count { get; }
+        public ICollection<string> Keys { get; }
+        public StringValues this[string key] { get; }
+        public bool ContainsKey(string key);
+        public QueryCollection.Enumerator GetEnumerator();
+        IEnumerator<KeyValuePair<string, StringValues>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.GetEnumerator();
+        IEnumerator System.Collections.IEnumerable.GetEnumerator();
+        public bool TryGetValue(string key, out StringValues value);
+        public struct Enumerator : IDisposable, IEnumerator, IEnumerator<KeyValuePair<string, StringValues>> {
+            public KeyValuePair<string, StringValues> Current { get; }
+            object System.Collections.IEnumerator.Current { get; }
+            public void Dispose();
+            public bool MoveNext();
+            void System.Collections.IEnumerator.Reset();
+        }
+    }
-    public struct QueryString : IEquatable<QueryString> {
+    public readonly struct QueryString : IEquatable<QueryString> {
         public static readonly QueryString Empty;
         public QueryString(string value);
         public bool HasValue { get; }
         public string Value { get; }
         public QueryString Add(QueryString other);
         public QueryString Add(string name, string value);
         public static QueryString Create(IEnumerable<KeyValuePair<string, StringValues>> parameters);
         public static QueryString Create(IEnumerable<KeyValuePair<string, string>> parameters);
         public static QueryString Create(string name, string value);
         public bool Equals(QueryString other);
         public override bool Equals(object obj);
         public static QueryString FromUriComponent(string uriComponent);
         public static QueryString FromUriComponent(Uri uri);
         public override int GetHashCode();
         public static QueryString operator +(QueryString left, QueryString right);
         public static bool operator ==(QueryString left, QueryString right);
         public static bool operator !=(QueryString left, QueryString right);
         public override string ToString();
         public string ToUriComponent();
     }
     public delegate Task RequestDelegate(HttpContext context);
     public static class RequestFormReaderExtensions {
         public static Task<IFormCollection> ReadFormAsync(this HttpRequest request, FormOptions options, CancellationToken cancellationToken = default(CancellationToken));
     }
+    public static class RequestTrailerExtensions {
+        public static bool CheckTrailersAvailable(this HttpRequest request);
+        public static StringValues GetDeclaredTrailers(this HttpRequest request);
+        public static StringValues GetTrailer(this HttpRequest request, string trailerName);
+        public static bool SupportsTrailers(this HttpRequest request);
+    }
     public static class ResponseExtensions {
         public static void Clear(this HttpResponse response);
+        public static void Redirect(this HttpResponse response, string location, bool permanent, bool preserveMethod);
     }
     public static class ResponseTrailerExtensions {
         public static void AppendTrailer(this HttpResponse response, string trailerName, StringValues trailerValues);
         public static void DeclareTrailer(this HttpResponse response, string trailerName);
         public static bool SupportsTrailers(this HttpResponse response);
     }
     public enum SameSiteMode {
         Lax = 1,
         None = 0,
         Strict = 2,
     }
     public static class SendFileResponseExtensions {
         public static Task SendFileAsync(this HttpResponse response, IFileInfo file, long offset, Nullable<long> count, CancellationToken cancellationToken = default(CancellationToken));
         public static Task SendFileAsync(this HttpResponse response, IFileInfo file, CancellationToken cancellationToken = default(CancellationToken));
         public static Task SendFileAsync(this HttpResponse response, string fileName, long offset, Nullable<long> count, CancellationToken cancellationToken = default(CancellationToken));
         public static Task SendFileAsync(this HttpResponse response, string fileName, CancellationToken cancellationToken = default(CancellationToken));
     }
     public static class SessionExtensions {
         public static byte[] Get(this ISession session, string key);
         public static Nullable<int> GetInt32(this ISession session, string key);
         public static string GetString(this ISession session, string key);
         public static void SetInt32(this ISession session, string key, int value);
         public static void SetString(this ISession session, string key, string value);
     }
     public static class StatusCodes {
         public const int Status100Continue = 100;
         public const int Status101SwitchingProtocols = 101;
         public const int Status102Processing = 102;
         public const int Status200OK = 200;
         public const int Status201Created = 201;
         public const int Status202Accepted = 202;
         public const int Status203NonAuthoritative = 203;
         public const int Status204NoContent = 204;
         public const int Status205ResetContent = 205;
         public const int Status206PartialContent = 206;
         public const int Status207MultiStatus = 207;
         public const int Status208AlreadyReported = 208;
         public const int Status226IMUsed = 226;
         public const int Status300MultipleChoices = 300;
         public const int Status301MovedPermanently = 301;
         public const int Status302Found = 302;
         public const int Status303SeeOther = 303;
         public const int Status304NotModified = 304;
         public const int Status305UseProxy = 305;
         public const int Status306SwitchProxy = 306;
         public const int Status307TemporaryRedirect = 307;
         public const int Status308PermanentRedirect = 308;
         public const int Status400BadRequest = 400;
         public const int Status401Unauthorized = 401;
         public const int Status402PaymentRequired = 402;
         public const int Status403Forbidden = 403;
         public const int Status404NotFound = 404;
         public const int Status405MethodNotAllowed = 405;
         public const int Status406NotAcceptable = 406;
         public const int Status407ProxyAuthenticationRequired = 407;
         public const int Status408RequestTimeout = 408;
         public const int Status409Conflict = 409;
         public const int Status410Gone = 410;
         public const int Status411LengthRequired = 411;
         public const int Status412PreconditionFailed = 412;
         public const int Status413PayloadTooLarge = 413;
         public const int Status413RequestEntityTooLarge = 413;
         public const int Status414RequestUriTooLong = 414;
         public const int Status414UriTooLong = 414;
         public const int Status415UnsupportedMediaType = 415;
         public const int Status416RangeNotSatisfiable = 416;
         public const int Status416RequestedRangeNotSatisfiable = 416;
         public const int Status417ExpectationFailed = 417;
         public const int Status418ImATeapot = 418;
         public const int Status419AuthenticationTimeout = 419;
         public const int Status421MisdirectedRequest = 421;
         public const int Status422UnprocessableEntity = 422;
         public const int Status423Locked = 423;
         public const int Status424FailedDependency = 424;
         public const int Status426UpgradeRequired = 426;
         public const int Status428PreconditionRequired = 428;
         public const int Status429TooManyRequests = 429;
         public const int Status431RequestHeaderFieldsTooLarge = 431;
         public const int Status451UnavailableForLegalReasons = 451;
         public const int Status500InternalServerError = 500;
         public const int Status501NotImplemented = 501;
         public const int Status502BadGateway = 502;
         public const int Status503ServiceUnavailable = 503;
         public const int Status504GatewayTimeout = 504;
         public const int Status505HttpVersionNotsupported = 505;
         public const int Status506VariantAlsoNegotiates = 506;
         public const int Status507InsufficientStorage = 507;
         public const int Status508LoopDetected = 508;
         public const int Status510NotExtended = 510;
         public const int Status511NetworkAuthenticationRequired = 511;
     }
     public class WebSocketAcceptContext {
         public WebSocketAcceptContext();
         public virtual string SubProtocol { get; set; }
     }
     public abstract class WebSocketManager {
         protected WebSocketManager();
         public abstract bool IsWebSocketRequest { get; }
         public abstract IList<string> WebSocketRequestedProtocols { get; }
         public virtual Task<WebSocket> AcceptWebSocketAsync();
         public abstract Task<WebSocket> AcceptWebSocketAsync(string subProtocol);
     }
 }
```

