# Microsoft.AspNetCore.Http.Internal

``` diff
-namespace Microsoft.AspNetCore.Http.Internal {
 {
-    public class BindingAddress {
 {
-        public BindingAddress();

-        public string Host { get; private set; }

-        public bool IsUnixPipe { get; }

-        public string PathBase { get; private set; }

-        public int Port { get; internal set; }

-        public string Scheme { get; private set; }

-        public string UnixPipePath { get; }

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public static BindingAddress Parse(string address);

-        public override string ToString();

-    }
-    public static class BufferingHelper {
 {
-        public static string TempDirectory { get; }

-        public static HttpRequest EnableRewind(this HttpRequest request, int bufferThreshold = 30720, Nullable<long> bufferLimit = default(Nullable<long>));

-        public static MultipartSection EnableRewind(this MultipartSection section, Action<IDisposable> registerForDispose, int bufferThreshold = 30720, Nullable<long> bufferLimit = default(Nullable<long>));

-    }
-    public class DefaultConnectionInfo : ConnectionInfo {
 {
-        public DefaultConnectionInfo(IFeatureCollection features);

-        public override X509Certificate2 ClientCertificate { get; set; }

-        public override string Id { get; set; }

-        public override IPAddress LocalIpAddress { get; set; }

-        public override int LocalPort { get; set; }

-        public override IPAddress RemoteIpAddress { get; set; }

-        public override int RemotePort { get; set; }

-        public override Task<X509Certificate2> GetClientCertificateAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public virtual void Initialize(IFeatureCollection features);

-        public virtual void Uninitialize();

-    }
-    public class DefaultHttpRequest : HttpRequest {
 {
-        public DefaultHttpRequest(HttpContext context);

-        public override Stream Body { get; set; }

-        public override Nullable<long> ContentLength { get; set; }

-        public override string ContentType { get; set; }

-        public override IRequestCookieCollection Cookies { get; set; }

-        public override IFormCollection Form { get; set; }

-        public override bool HasFormContentType { get; }

-        public override IHeaderDictionary Headers { get; }

-        public override HostString Host { get; set; }

-        public override HttpContext HttpContext { get; }

-        public override bool IsHttps { get; set; }

-        public override string Method { get; set; }

-        public override PathString Path { get; set; }

-        public override PathString PathBase { get; set; }

-        public override string Protocol { get; set; }

-        public override IQueryCollection Query { get; set; }

-        public override QueryString QueryString { get; set; }

-        public override string Scheme { get; set; }

-        public virtual void Initialize(HttpContext context);

-        public override Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken);

-        public virtual void Uninitialize();

-    }
-    public class DefaultHttpResponse : HttpResponse {
 {
-        public DefaultHttpResponse(HttpContext context);

-        public override Stream Body { get; set; }

-        public override Nullable<long> ContentLength { get; set; }

-        public override string ContentType { get; set; }

-        public override IResponseCookies Cookies { get; }

-        public override bool HasStarted { get; }

-        public override IHeaderDictionary Headers { get; }

-        public override HttpContext HttpContext { get; }

-        public override int StatusCode { get; set; }

-        public virtual void Initialize(HttpContext context);

-        public override void OnCompleted(Func<object, Task> callback, object state);

-        public override void OnStarting(Func<object, Task> callback, object state);

-        public override void Redirect(string location, bool permanent);

-        public virtual void Uninitialize();

-    }
-    public class DefaultWebSocketManager : WebSocketManager {
 {
-        public DefaultWebSocketManager(IFeatureCollection features);

-        public override bool IsWebSocketRequest { get; }

-        public override IList<string> WebSocketRequestedProtocols { get; }

-        public override Task<WebSocket> AcceptWebSocketAsync(string subProtocol);

-        public virtual void Initialize(IFeatureCollection features);

-        public virtual void Uninitialize();

-    }
-    public class FormFile : IFormFile {
 {
-        public FormFile(Stream baseStream, long baseStreamOffset, long length, string name, string fileName);

-        public string ContentDisposition { get; set; }

-        public string ContentType { get; set; }

-        public string FileName { get; }

-        public IHeaderDictionary Headers { get; set; }

-        public long Length { get; }

-        public string Name { get; }

-        public void CopyTo(Stream target);

-        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default(CancellationToken));

-        public Stream OpenReadStream();

-    }
-    public class FormFileCollection : List<IFormFile>, IEnumerable, IEnumerable<IFormFile>, IFormFileCollection, IReadOnlyCollection<IFormFile>, IReadOnlyList<IFormFile> {
 {
-        public FormFileCollection();

-        public IFormFile this[string name] { get; }

-        public IFormFile GetFile(string name);

-        public IReadOnlyList<IFormFile> GetFiles(string name);

-    }
-    public struct HeaderSegment : IEquatable<HeaderSegment> {
 {
-        public HeaderSegment(StringSegment formatting, StringSegment data);

-        public StringSegment Data { get; }

-        public StringSegment Formatting { get; }

-        public bool Equals(HeaderSegment other);

-        public override bool Equals(object obj);

-        public override int GetHashCode();

-        public static bool operator ==(HeaderSegment left, HeaderSegment right);

-        public static bool operator !=(HeaderSegment left, HeaderSegment right);

-    }
-    public struct HeaderSegmentCollection : IEnumerable, IEnumerable<HeaderSegment>, IEquatable<HeaderSegmentCollection> {
 {
-        public HeaderSegmentCollection(StringValues headers);

-        public bool Equals(HeaderSegmentCollection other);

-        public override bool Equals(object obj);

-        public HeaderSegmentCollection.Enumerator GetEnumerator();

-        public override int GetHashCode();

-        public static bool operator ==(HeaderSegmentCollection left, HeaderSegmentCollection right);

-        public static bool operator !=(HeaderSegmentCollection left, HeaderSegmentCollection right);

-        IEnumerator<HeaderSegment> System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Http.Internal.HeaderSegment>.GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-        public struct Enumerator : IDisposable, IEnumerator, IEnumerator<HeaderSegment> {
 {
-            public Enumerator(StringValues headers);

-            public HeaderSegment Current { get; }

-            object System.Collections.IEnumerator.Current { get; }

-            public void Dispose();

-            public bool MoveNext();

-            public void Reset();

-        }
-    }
-    public class ItemsDictionary : ICollection<KeyValuePair<object, object>>, IDictionary<object, object>, IEnumerable, IEnumerable<KeyValuePair<object, object>> {
 {
-        public ItemsDictionary();

-        public ItemsDictionary(IDictionary<object, object> items);

-        public IDictionary<object, object> Items { get; }

-        int System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.Count { get; }

-        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.IsReadOnly { get; }

-        object System.Collections.Generic.IDictionary<System.Object,System.Object>.this[object key] { get; set; }

-        ICollection<object> System.Collections.Generic.IDictionary<System.Object,System.Object>.Keys { get; }

-        ICollection<object> System.Collections.Generic.IDictionary<System.Object,System.Object>.Values { get; }

-        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.Add(KeyValuePair<object, object> item);

-        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.Clear();

-        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.Contains(KeyValuePair<object, object> item);

-        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.CopyTo(KeyValuePair<object, object>[] array, int arrayIndex);

-        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.Remove(KeyValuePair<object, object> item);

-        void System.Collections.Generic.IDictionary<System.Object,System.Object>.Add(object key, object value);

-        bool System.Collections.Generic.IDictionary<System.Object,System.Object>.ContainsKey(object key);

-        bool System.Collections.Generic.IDictionary<System.Object,System.Object>.Remove(object key);

-        bool System.Collections.Generic.IDictionary<System.Object,System.Object>.TryGetValue(object key, out object value);

-        IEnumerator<KeyValuePair<object, object>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.Object,System.Object>>.GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-    }
-    public static class ParsingHelpers {
 {
-        public static void AppendHeaderJoined(IHeaderDictionary headers, string key, params string[] values);

-        public static void AppendHeaderUnmodified(IHeaderDictionary headers, string key, StringValues values);

-        public static StringValues GetHeader(IHeaderDictionary headers, string key);

-        public static StringValues GetHeaderSplit(IHeaderDictionary headers, string key);

-        public static StringValues GetHeaderUnmodified(IHeaderDictionary headers, string key);

-        public static void SetHeaderJoined(IHeaderDictionary headers, string key, StringValues value);

-        public static void SetHeaderUnmodified(IHeaderDictionary headers, string key, Nullable<StringValues> values);

-    }
-    public class QueryCollection : IEnumerable, IEnumerable<KeyValuePair<string, StringValues>>, IQueryCollection {
 {
-        public static readonly QueryCollection Empty;

-        public QueryCollection();

-        public QueryCollection(QueryCollection store);

-        public QueryCollection(Dictionary<string, StringValues> store);

-        public QueryCollection(int capacity);

-        public int Count { get; }

-        public ICollection<string> Keys { get; }

-        public StringValues this[string key] { get; }

-        public bool ContainsKey(string key);

-        public QueryCollection.Enumerator GetEnumerator();

-        IEnumerator<KeyValuePair<string, StringValues>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-        public bool TryGetValue(string key, out StringValues value);

-        public struct Enumerator : IDisposable, IEnumerator, IEnumerator<KeyValuePair<string, StringValues>> {
 {
-            public KeyValuePair<string, StringValues> Current { get; }

-            object System.Collections.IEnumerator.Current { get; }

-            public void Dispose();

-            public bool MoveNext();

-            void System.Collections.IEnumerator.Reset();

-        }
-    }
-    public class RequestCookieCollection : IEnumerable, IEnumerable<KeyValuePair<string, string>>, IRequestCookieCollection {
 {
-        public static readonly RequestCookieCollection Empty;

-        public RequestCookieCollection();

-        public RequestCookieCollection(Dictionary<string, string> store);

-        public RequestCookieCollection(int capacity);

-        public int Count { get; }

-        public ICollection<string> Keys { get; }

-        public string this[string key] { get; }

-        public bool ContainsKey(string key);

-        public RequestCookieCollection.Enumerator GetEnumerator();

-        public static RequestCookieCollection Parse(IList<string> values);

-        IEnumerator<KeyValuePair<string, string>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String,System.String>>.GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-        public bool TryGetValue(string key, out string value);

-        public struct Enumerator : IDisposable, IEnumerator, IEnumerator<KeyValuePair<string, string>> {
 {
-            public KeyValuePair<string, string> Current { get; }

-            object System.Collections.IEnumerator.Current { get; }

-            public void Dispose();

-            public bool MoveNext();

-            public void Reset();

-        }
-    }
-    public class ResponseCookies : IResponseCookies {
 {
-        public ResponseCookies(IHeaderDictionary headers, ObjectPool<StringBuilder> builderPool);

-        public void Append(string key, string value);

-        public void Append(string key, string value, CookieOptions options);

-        public void Delete(string key);

-        public void Delete(string key, CookieOptions options);

-    }
-}
```

