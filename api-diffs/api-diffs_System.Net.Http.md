# System.Net.Http

``` diff
 namespace System.Net.Http {
-    public class ByteRangeStreamContent : HttpContent {
 {
-        public ByteRangeStreamContent(Stream content, RangeHeaderValue range, MediaTypeHeaderValue mediaType);

-        public ByteRangeStreamContent(Stream content, RangeHeaderValue range, MediaTypeHeaderValue mediaType, int bufferSize);

-        public ByteRangeStreamContent(Stream content, RangeHeaderValue range, string mediaType);

-        public ByteRangeStreamContent(Stream content, RangeHeaderValue range, string mediaType, int bufferSize);

-        protected override void Dispose(bool disposing);

-        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context);

-        protected override bool TryComputeLength(out long length);

-    }
-    public static class HttpClientExtensions {
 {
-        public static Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient client, string requestUri, T value);

-        public static Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient client, string requestUri, T value, CancellationToken cancellationToken);

-        public static Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient client, Uri requestUri, T value);

-        public static Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient client, Uri requestUri, T value, CancellationToken cancellationToken);

-        public static Task<HttpResponseMessage> PostAsXmlAsync<T>(this HttpClient client, string requestUri, T value);

-        public static Task<HttpResponseMessage> PostAsXmlAsync<T>(this HttpClient client, string requestUri, T value, CancellationToken cancellationToken);

-        public static Task<HttpResponseMessage> PostAsXmlAsync<T>(this HttpClient client, Uri requestUri, T value);

-        public static Task<HttpResponseMessage> PostAsXmlAsync<T>(this HttpClient client, Uri requestUri, T value, CancellationToken cancellationToken);

-        public static Task<HttpResponseMessage> PostAsync<T>(this HttpClient client, string requestUri, T value, MediaTypeFormatter formatter);

-        public static Task<HttpResponseMessage> PostAsync<T>(this HttpClient client, string requestUri, T value, MediaTypeFormatter formatter, MediaTypeHeaderValue mediaType, CancellationToken cancellationToken);

-        public static Task<HttpResponseMessage> PostAsync<T>(this HttpClient client, string requestUri, T value, MediaTypeFormatter formatter, string mediaType);

-        public static Task<HttpResponseMessage> PostAsync<T>(this HttpClient client, string requestUri, T value, MediaTypeFormatter formatter, string mediaType, CancellationToken cancellationToken);

-        public static Task<HttpResponseMessage> PostAsync<T>(this HttpClient client, string requestUri, T value, MediaTypeFormatter formatter, CancellationToken cancellationToken);

-        public static Task<HttpResponseMessage> PostAsync<T>(this HttpClient client, Uri requestUri, T value, MediaTypeFormatter formatter);

-        public static Task<HttpResponseMessage> PostAsync<T>(this HttpClient client, Uri requestUri, T value, MediaTypeFormatter formatter, MediaTypeHeaderValue mediaType, CancellationToken cancellationToken);

-        public static Task<HttpResponseMessage> PostAsync<T>(this HttpClient client, Uri requestUri, T value, MediaTypeFormatter formatter, string mediaType);

-        public static Task<HttpResponseMessage> PostAsync<T>(this HttpClient client, Uri requestUri, T value, MediaTypeFormatter formatter, string mediaType, CancellationToken cancellationToken);

-        public static Task<HttpResponseMessage> PostAsync<T>(this HttpClient client, Uri requestUri, T value, MediaTypeFormatter formatter, CancellationToken cancellationToken);

-        public static Task<HttpResponseMessage> PutAsJsonAsync<T>(this HttpClient client, string requestUri, T value);

-        public static Task<HttpResponseMessage> PutAsJsonAsync<T>(this HttpClient client, string requestUri, T value, CancellationToken cancellationToken);

-        public static Task<HttpResponseMessage> PutAsJsonAsync<T>(this HttpClient client, Uri requestUri, T value);

-        public static Task<HttpResponseMessage> PutAsJsonAsync<T>(this HttpClient client, Uri requestUri, T value, CancellationToken cancellationToken);

-        public static Task<HttpResponseMessage> PutAsXmlAsync<T>(this HttpClient client, string requestUri, T value);

-        public static Task<HttpResponseMessage> PutAsXmlAsync<T>(this HttpClient client, string requestUri, T value, CancellationToken cancellationToken);

-        public static Task<HttpResponseMessage> PutAsXmlAsync<T>(this HttpClient client, Uri requestUri, T value);

-        public static Task<HttpResponseMessage> PutAsXmlAsync<T>(this HttpClient client, Uri requestUri, T value, CancellationToken cancellationToken);

-        public static Task<HttpResponseMessage> PutAsync<T>(this HttpClient client, string requestUri, T value, MediaTypeFormatter formatter);

-        public static Task<HttpResponseMessage> PutAsync<T>(this HttpClient client, string requestUri, T value, MediaTypeFormatter formatter, MediaTypeHeaderValue mediaType, CancellationToken cancellationToken);

-        public static Task<HttpResponseMessage> PutAsync<T>(this HttpClient client, string requestUri, T value, MediaTypeFormatter formatter, string mediaType);

-        public static Task<HttpResponseMessage> PutAsync<T>(this HttpClient client, string requestUri, T value, MediaTypeFormatter formatter, string mediaType, CancellationToken cancellationToken);

-        public static Task<HttpResponseMessage> PutAsync<T>(this HttpClient client, string requestUri, T value, MediaTypeFormatter formatter, CancellationToken cancellationToken);

-        public static Task<HttpResponseMessage> PutAsync<T>(this HttpClient client, Uri requestUri, T value, MediaTypeFormatter formatter);

-        public static Task<HttpResponseMessage> PutAsync<T>(this HttpClient client, Uri requestUri, T value, MediaTypeFormatter formatter, MediaTypeHeaderValue mediaType, CancellationToken cancellationToken);

-        public static Task<HttpResponseMessage> PutAsync<T>(this HttpClient client, Uri requestUri, T value, MediaTypeFormatter formatter, string mediaType);

-        public static Task<HttpResponseMessage> PutAsync<T>(this HttpClient client, Uri requestUri, T value, MediaTypeFormatter formatter, string mediaType, CancellationToken cancellationToken);

-        public static Task<HttpResponseMessage> PutAsync<T>(this HttpClient client, Uri requestUri, T value, MediaTypeFormatter formatter, CancellationToken cancellationToken);

-    }
-    public static class HttpClientFactory {
 {
-        public static HttpClient Create(params DelegatingHandler[] handlers);

-        public static HttpClient Create(HttpMessageHandler innerHandler, params DelegatingHandler[] handlers);

-        public static HttpMessageHandler CreatePipeline(HttpMessageHandler innerHandler, IEnumerable<DelegatingHandler> handlers);

-    }
     public static class HttpClientFactoryExtensions {
         public static HttpClient CreateClient(this IHttpClientFactory factory);
     }
-    public static class HttpContentExtensions {
 {
-        public static Task<object> ReadAsAsync(this HttpContent content, Type type);

-        public static Task<object> ReadAsAsync(this HttpContent content, Type type, IEnumerable<MediaTypeFormatter> formatters);

-        public static Task<object> ReadAsAsync(this HttpContent content, Type type, IEnumerable<MediaTypeFormatter> formatters, IFormatterLogger formatterLogger);

-        public static Task<object> ReadAsAsync(this HttpContent content, Type type, IEnumerable<MediaTypeFormatter> formatters, IFormatterLogger formatterLogger, CancellationToken cancellationToken);

-        public static Task<object> ReadAsAsync(this HttpContent content, Type type, IEnumerable<MediaTypeFormatter> formatters, CancellationToken cancellationToken);

-        public static Task<object> ReadAsAsync(this HttpContent content, Type type, CancellationToken cancellationToken);

-        public static Task<T> ReadAsAsync<T>(this HttpContent content);

-        public static Task<T> ReadAsAsync<T>(this HttpContent content, IEnumerable<MediaTypeFormatter> formatters);

-        public static Task<T> ReadAsAsync<T>(this HttpContent content, IEnumerable<MediaTypeFormatter> formatters, IFormatterLogger formatterLogger);

-        public static Task<T> ReadAsAsync<T>(this HttpContent content, IEnumerable<MediaTypeFormatter> formatters, IFormatterLogger formatterLogger, CancellationToken cancellationToken);

-        public static Task<T> ReadAsAsync<T>(this HttpContent content, IEnumerable<MediaTypeFormatter> formatters, CancellationToken cancellationToken);

-        public static Task<T> ReadAsAsync<T>(this HttpContent content, CancellationToken cancellationToken);

-    }
-    public static class HttpContentFormDataExtensions {
 {
-        public static bool IsFormData(this HttpContent content);

-        public static Task<NameValueCollection> ReadAsFormDataAsync(this HttpContent content);

-        public static Task<NameValueCollection> ReadAsFormDataAsync(this HttpContent content, CancellationToken cancellationToken);

-    }
-    public static class HttpContentMessageExtensions {
 {
-        public static bool IsHttpRequestMessageContent(this HttpContent content);

-        public static bool IsHttpResponseMessageContent(this HttpContent content);

-        public static Task<HttpRequestMessage> ReadAsHttpRequestMessageAsync(this HttpContent content);

-        public static Task<HttpRequestMessage> ReadAsHttpRequestMessageAsync(this HttpContent content, string uriScheme);

-        public static Task<HttpRequestMessage> ReadAsHttpRequestMessageAsync(this HttpContent content, string uriScheme, int bufferSize);

-        public static Task<HttpRequestMessage> ReadAsHttpRequestMessageAsync(this HttpContent content, string uriScheme, int bufferSize, int maxHeaderSize);

-        public static Task<HttpRequestMessage> ReadAsHttpRequestMessageAsync(this HttpContent content, string uriScheme, int bufferSize, int maxHeaderSize, CancellationToken cancellationToken);

-        public static Task<HttpRequestMessage> ReadAsHttpRequestMessageAsync(this HttpContent content, string uriScheme, int bufferSize, CancellationToken cancellationToken);

-        public static Task<HttpRequestMessage> ReadAsHttpRequestMessageAsync(this HttpContent content, string uriScheme, CancellationToken cancellationToken);

-        public static Task<HttpRequestMessage> ReadAsHttpRequestMessageAsync(this HttpContent content, CancellationToken cancellationToken);

-        public static Task<HttpResponseMessage> ReadAsHttpResponseMessageAsync(this HttpContent content);

-        public static Task<HttpResponseMessage> ReadAsHttpResponseMessageAsync(this HttpContent content, int bufferSize);

-        public static Task<HttpResponseMessage> ReadAsHttpResponseMessageAsync(this HttpContent content, int bufferSize, int maxHeaderSize);

-        public static Task<HttpResponseMessage> ReadAsHttpResponseMessageAsync(this HttpContent content, int bufferSize, int maxHeaderSize, CancellationToken cancellationToken);

-        public static Task<HttpResponseMessage> ReadAsHttpResponseMessageAsync(this HttpContent content, int bufferSize, CancellationToken cancellationToken);

-        public static Task<HttpResponseMessage> ReadAsHttpResponseMessageAsync(this HttpContent content, CancellationToken cancellationToken);

-    }
-    public static class HttpContentMultipartExtensions {
 {
-        public static bool IsMimeMultipartContent(this HttpContent content);

-        public static bool IsMimeMultipartContent(this HttpContent content, string subtype);

-        public static Task<MultipartMemoryStreamProvider> ReadAsMultipartAsync(this HttpContent content);

-        public static Task<MultipartMemoryStreamProvider> ReadAsMultipartAsync(this HttpContent content, CancellationToken cancellationToken);

-        public static Task<T> ReadAsMultipartAsync<T>(this HttpContent content, T streamProvider) where T : MultipartStreamProvider;

-        public static Task<T> ReadAsMultipartAsync<T>(this HttpContent content, T streamProvider, int bufferSize) where T : MultipartStreamProvider;

-        public static Task<T> ReadAsMultipartAsync<T>(this HttpContent content, T streamProvider, int bufferSize, CancellationToken cancellationToken) where T : MultipartStreamProvider;

-        public static Task<T> ReadAsMultipartAsync<T>(this HttpContent content, T streamProvider, CancellationToken cancellationToken) where T : MultipartStreamProvider;

-    }
-    public class HttpMessageContent : HttpContent {
 {
-        public HttpMessageContent(HttpRequestMessage httpRequest);

-        public HttpMessageContent(HttpResponseMessage httpResponse);

-        public HttpRequestMessage HttpRequestMessage { get; private set; }

-        public HttpResponseMessage HttpResponseMessage { get; private set; }

-        protected override void Dispose(bool disposing);

-        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context);

-        protected override bool TryComputeLength(out long length);

-    }
     public static class HttpMessageHandlerFactoryExtensions {
         public static HttpMessageHandler CreateHandler(this IHttpMessageHandlerFactory factory);
     }
-    public static class HttpRequestHeadersExtensions {
 {
-        public static Collection<CookieHeaderValue> GetCookies(this HttpRequestHeaders headers);

-        public static Collection<CookieHeaderValue> GetCookies(this HttpRequestHeaders headers, string name);

-    }
-    public static class HttpRequestMessageExtensions {
 {
-        public static HttpResponseMessage CreateResponse(this HttpRequestMessage request);

-        public static HttpResponseMessage CreateResponse(this HttpRequestMessage request, HttpStatusCode statusCode);

-    }
-    public static class HttpResponseHeadersExtensions {
 {
-        public static void AddCookies(this HttpResponseHeaders headers, IEnumerable<CookieHeaderValue> cookies);

-    }
     public interface IHttpClientFactory {
         HttpClient CreateClient(string name);
     }
     public interface IHttpMessageHandlerFactory {
         HttpMessageHandler CreateHandler(string name);
     }
-    public class InvalidByteRangeException : Exception {
 {
-        public InvalidByteRangeException(ContentRangeHeaderValue contentRange);

-        public InvalidByteRangeException(ContentRangeHeaderValue contentRange, SerializationInfo info, StreamingContext context);

-        public InvalidByteRangeException(ContentRangeHeaderValue contentRange, string message);

-        public InvalidByteRangeException(ContentRangeHeaderValue contentRange, string message, Exception innerException);

-        public ContentRangeHeaderValue ContentRange { get; private set; }

-    }
-    public class MultipartFileData {
 {
-        public MultipartFileData(HttpContentHeaders headers, string localFileName);

-        public HttpContentHeaders Headers { get; private set; }

-        public string LocalFileName { get; private set; }

-    }
-    public class MultipartFileStreamProvider : MultipartStreamProvider {
 {
-        public MultipartFileStreamProvider(string rootPath);

-        public MultipartFileStreamProvider(string rootPath, int bufferSize);

-        protected int BufferSize { get; }

-        public Collection<MultipartFileData> FileData { get; }

-        protected string RootPath { get; }

-        public virtual string GetLocalFileName(HttpContentHeaders headers);

-        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers);

-    }
-    public abstract class MultipartFormDataRemoteStreamProvider : MultipartStreamProvider {
 {
-        protected MultipartFormDataRemoteStreamProvider();

-        public Collection<MultipartRemoteFileData> FileData { get; private set; }

-        public NameValueCollection FormData { get; private set; }

-        public override Task ExecutePostProcessingAsync();

-        public override Task ExecutePostProcessingAsync(CancellationToken cancellationToken);

-        public abstract RemoteStreamInfo GetRemoteStream(HttpContent parent, HttpContentHeaders headers);

-        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers);

-    }
-    public class MultipartFormDataStreamProvider : MultipartFileStreamProvider {
 {
-        public MultipartFormDataStreamProvider(string rootPath);

-        public MultipartFormDataStreamProvider(string rootPath, int bufferSize);

-        public NameValueCollection FormData { get; private set; }

-        public override Task ExecutePostProcessingAsync();

-        public override Task ExecutePostProcessingAsync(CancellationToken cancellationToken);

-        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers);

-    }
-    public class MultipartMemoryStreamProvider : MultipartStreamProvider {
 {
-        public MultipartMemoryStreamProvider();

-        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers);

-    }
-    public class MultipartRelatedStreamProvider : MultipartStreamProvider {
 {
-        public MultipartRelatedStreamProvider();

-        public HttpContent RootContent { get; }

-        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers);

-    }
-    public class MultipartRemoteFileData {
 {
-        public MultipartRemoteFileData(HttpContentHeaders headers, string location, string fileName);

-        public string FileName { get; private set; }

-        public HttpContentHeaders Headers { get; private set; }

-        public string Location { get; private set; }

-    }
-    public abstract class MultipartStreamProvider {
 {
-        protected MultipartStreamProvider();

-        public Collection<HttpContent> Contents { get; }

-        public virtual Task ExecutePostProcessingAsync();

-        public virtual Task ExecutePostProcessingAsync(CancellationToken cancellationToken);

-        public abstract Stream GetStream(HttpContent parent, HttpContentHeaders headers);

-    }
-    public class ObjectContent : HttpContent {
 {
-        public ObjectContent(Type type, object value, MediaTypeFormatter formatter);

-        public ObjectContent(Type type, object value, MediaTypeFormatter formatter, MediaTypeHeaderValue mediaType);

-        public ObjectContent(Type type, object value, MediaTypeFormatter formatter, string mediaType);

-        public MediaTypeFormatter Formatter { get; }

-        public Type ObjectType { get; private set; }

-        public object Value { get; set; }

-        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context);

-        protected override bool TryComputeLength(out long length);

-    }
-    public class ObjectContent<T> : ObjectContent {
 {
-        public ObjectContent(T value, MediaTypeFormatter formatter);

-        public ObjectContent(T value, MediaTypeFormatter formatter, MediaTypeHeaderValue mediaType);

-        public ObjectContent(T value, MediaTypeFormatter formatter, string mediaType);

-    }
-    public class PushStreamContent : HttpContent {
 {
-        public PushStreamContent(Action<Stream, HttpContent, TransportContext> onStreamAvailable);

-        public PushStreamContent(Action<Stream, HttpContent, TransportContext> onStreamAvailable, MediaTypeHeaderValue mediaType);

-        public PushStreamContent(Action<Stream, HttpContent, TransportContext> onStreamAvailable, string mediaType);

-        public PushStreamContent(Func<Stream, HttpContent, TransportContext, Task> onStreamAvailable);

-        public PushStreamContent(Func<Stream, HttpContent, TransportContext, Task> onStreamAvailable, MediaTypeHeaderValue mediaType);

-        public PushStreamContent(Func<Stream, HttpContent, TransportContext, Task> onStreamAvailable, string mediaType);

-        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context);

-        protected override bool TryComputeLength(out long length);

-    }
-    public class RemoteStreamInfo {
 {
-        public RemoteStreamInfo(Stream remoteStream, string location, string fileName);

-        public string FileName { get; private set; }

-        public string Location { get; private set; }

-        public Stream RemoteStream { get; private set; }

-    }
-    public class UnsupportedMediaTypeException : Exception {
 {
-        public UnsupportedMediaTypeException(string message, MediaTypeHeaderValue mediaType);

-        public MediaTypeHeaderValue MediaType { get; private set; }

-    }
-    public static class UriExtensions {
 {
-        public static NameValueCollection ParseQueryString(this Uri address);

-        public static bool TryReadQueryAs(this Uri address, Type type, out object value);

-        public static bool TryReadQueryAs<T>(this Uri address, out T value);

-        public static bool TryReadQueryAsJson(this Uri address, out JObject value);

-    }
 }
```

