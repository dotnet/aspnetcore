# Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http

``` diff
 namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http {
-    public enum ConnectionOptions {
 {
-        Close = 1,

-        KeepAlive = 2,

-        None = 0,

-        Upgrade = 4,

-    }
-    public class DateHeaderValueManager : IHeartbeatHandler {
 {
-        public DateHeaderValueManager();

-        public DateHeaderValueManager.DateHeaderValues GetDateHeaderValues();

-        public void OnHeartbeat(DateTimeOffset now);

-        public class DateHeaderValues {
 {
-            public byte[] Bytes;

-            public string String;

-            public DateHeaderValues();

-        }
-    }
-    public class Http1Connection : HttpProtocol, IHttpMinRequestBodyDataRateFeature, IHttpMinResponseDataRateFeature, IRequestProcessor {
 {
-        protected readonly long _keepAliveTicks;

-        public Http1Connection(HttpConnectionContext context);

-        public PipeReader Input { get; }

-        MinDataRate Microsoft.AspNetCore.Server.Kestrel.Core.Features.IHttpMinRequestBodyDataRateFeature.MinDataRate { get; set; }

-        MinDataRate Microsoft.AspNetCore.Server.Kestrel.Core.Features.IHttpMinResponseDataRateFeature.MinDataRate { get; set; }

-        public MinDataRate MinRequestBodyDataRate { get; set; }

-        public MinDataRate MinResponseDataRate { get; set; }

-        public bool RequestTimedOut { get; }

-        public void Abort(ConnectionAbortedException abortReason);

-        protected override void ApplicationAbort();

-        protected override bool BeginRead(out ValueTask<ReadResult> awaitable);

-        protected override void BeginRequestProcessing();

-        protected override MessageBody CreateMessageBody();

-        protected override string CreateRequestId();

-        public void HandleReadDataRateTimeout();

-        public void HandleRequestHeadersTimeout();

-        void Microsoft.AspNetCore.Server.Kestrel.Core.Internal.IRequestProcessor.Tick(DateTimeOffset now);

-        public void OnInputOrOutputCompleted();

-        protected override void OnRequestProcessingEnded();

-        protected override void OnRequestProcessingEnding();

-        protected override void OnReset();

-        public void OnStartLine(HttpMethod method, HttpVersion version, Span<byte> target, Span<byte> path, Span<byte> query, Span<byte> customMethod, bool pathEncoded);

-        public void ParseRequest(ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined);

-        public void SendTimeoutResponse();

-        public void StopProcessingNextRequest();

-        public bool TakeMessageHeaders(ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined);

-        public bool TakeStartLine(ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined);

-        protected override bool TryParseRequest(ReadResult result, out bool endConnection);

-    }
-    public abstract class Http1MessageBody : MessageBody {
 {
-        protected Http1MessageBody(Http1Connection context);

-        protected void Copy(ReadOnlySequence<byte> readableBuffer, PipeWriter writableBuffer);

-        public static MessageBody For(HttpVersion httpVersion, HttpRequestHeaders headers, Http1Connection context);

-        protected override Task OnConsumeAsync();

-        protected override void OnReadStarted();

-        protected override Task OnStopAsync();

-        protected virtual bool Read(ReadOnlySequence<byte> readableBuffer, PipeWriter writableBuffer, out SequencePosition consumed, out SequencePosition examined);

-    }
-    public class Http1OutputProducer : IDisposable, IHttpOutputAborter, IHttpOutputProducer {
 {
-        public Http1OutputProducer(PipeWriter pipeWriter, string connectionId, ConnectionContext connectionContext, IKestrelTrace log, ITimeoutControl timeoutControl, IHttpMinResponseDataRateFeature minResponseDataRateFeature);

-        public void Abort(ConnectionAbortedException error);

-        public void Dispose();

-        public Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public Task Write100ContinueAsync();

-        public Task WriteAsync<T>(Func<PipeWriter, T, long> callback, T state, CancellationToken cancellationToken);

-        public Task WriteDataAsync(ReadOnlySpan<byte> buffer, CancellationToken cancellationToken = default(CancellationToken));

-        public void WriteResponseHeaders(int statusCode, string reasonPhrase, HttpResponseHeaders responseHeaders);

-        public Task WriteStreamSuffixAsync();

-    }
-    public struct Http1ParsingHandler : IHttpHeadersHandler, IHttpRequestLineHandler {
 {
-        public Http1Connection Connection;

-        public Http1ParsingHandler(Http1Connection connection);

-        public void OnHeader(Span<byte> name, Span<byte> value);

-        public void OnStartLine(HttpMethod method, HttpVersion version, Span<byte> target, Span<byte> path, Span<byte> query, Span<byte> customMethod, bool pathEncoded);

-    }
-    public abstract class HttpHeaders : ICollection<KeyValuePair<string, StringValues>>, IDictionary<string, StringValues>, IEnumerable, IEnumerable<KeyValuePair<string, StringValues>>, IHeaderDictionary {
 {
-        protected bool _isReadOnly;

-        protected Dictionary<string, StringValues> MaybeUnknown;

-        protected Nullable<long> _contentLength;

-        protected HttpHeaders();

-        public Nullable<long> ContentLength { get; set; }

-        public int Count { get; }

-        StringValues Microsoft.AspNetCore.Http.IHeaderDictionary.this[string key] { get; set; }

-        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.IsReadOnly { get; }

-        StringValues System.Collections.Generic.IDictionary<System.String,Microsoft.Extensions.Primitives.StringValues>.this[string key] { get; set; }

-        ICollection<string> System.Collections.Generic.IDictionary<System.String,Microsoft.Extensions.Primitives.StringValues>.Keys { get; }

-        ICollection<StringValues> System.Collections.Generic.IDictionary<System.String,Microsoft.Extensions.Primitives.StringValues>.Values { get; }

-        protected Dictionary<string, StringValues> Unknown { get; }

-        protected virtual bool AddValueFast(string key, in StringValues value);

-        protected static StringValues AppendValue(in StringValues existing, string append);

-        protected static int BitCount(long value);

-        protected virtual void ClearFast();

-        protected virtual bool CopyToFast(KeyValuePair<string, StringValues>[] array, int arrayIndex);

-        protected virtual int GetCountFast();

-        protected virtual IEnumerator<KeyValuePair<string, StringValues>> GetEnumeratorFast();

-        public static TransferCoding GetFinalTransferCoding(in StringValues transferEncoding);

-        public static ConnectionOptions ParseConnection(in StringValues connection);

-        protected virtual bool RemoveFast(string key);

-        public void Reset();

-        public void SetReadOnly();

-        protected virtual void SetValueFast(string key, in StringValues value);

-        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.Add(KeyValuePair<string, StringValues> item);

-        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.Clear();

-        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.Contains(KeyValuePair<string, StringValues> item);

-        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex);

-        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.Remove(KeyValuePair<string, StringValues> item);

-        void System.Collections.Generic.IDictionary<System.String,Microsoft.Extensions.Primitives.StringValues>.Add(string key, StringValues value);

-        bool System.Collections.Generic.IDictionary<System.String,Microsoft.Extensions.Primitives.StringValues>.ContainsKey(string key);

-        bool System.Collections.Generic.IDictionary<System.String,Microsoft.Extensions.Primitives.StringValues>.Remove(string key);

-        bool System.Collections.Generic.IDictionary<System.String,Microsoft.Extensions.Primitives.StringValues>.TryGetValue(string key, out StringValues value);

-        IEnumerator<KeyValuePair<string, StringValues>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-        protected void ThrowArgumentException();

-        protected void ThrowDuplicateKeyException();

-        protected void ThrowHeadersReadOnlyException();

-        protected void ThrowKeyNotFoundException();

-        protected virtual bool TryGetValueFast(string key, out StringValues value);

-        public static void ValidateHeaderNameCharacters(string headerCharacters);

-        public static void ValidateHeaderValueCharacters(in StringValues headerValues);

-        public static void ValidateHeaderValueCharacters(string headerCharacters);

-    }
     public enum HttpMethod : byte {
         Connect = (byte)7,
         Custom = (byte)9,
         Delete = (byte)2,
         Get = (byte)0,
         Head = (byte)4,
         None = (byte)255,
         Options = (byte)8,
         Patch = (byte)6,
         Post = (byte)3,
         Put = (byte)1,
         Trace = (byte)5,
     }
     public class HttpParser<TRequestHandler> : IHttpParser<TRequestHandler> where TRequestHandler : IHttpHeadersHandler, IHttpRequestLineHandler {
         public HttpParser();
         public HttpParser(bool showErrorDetails);
-        bool Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpParser<TRequestHandler>.ParseHeaders(TRequestHandler handler, in ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined, out int consumedBytes);

         bool Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpParser<TRequestHandler>.ParseRequestLine(TRequestHandler handler, in ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined);
-        public bool ParseHeaders(TRequestHandler handler, in ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined, out int consumedBytes);

+        public bool ParseHeaders(TRequestHandler handler, ref SequenceReader<byte> reader);
         public bool ParseRequestLine(TRequestHandler handler, in ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined);
     }
-    public abstract class HttpProtocol : IEnumerable, IEnumerable<KeyValuePair<Type, object>>, IFeatureCollection, IHttpBodyControlFeature, IHttpConnectionFeature, IHttpMaxRequestBodySizeFeature, IHttpRequestFeature, IHttpRequestIdentifierFeature, IHttpRequestLifetimeFeature, IHttpResponseControl, IHttpResponseFeature, IHttpUpgradeFeature {
 {
-        protected HttpVersion _httpVersion;

-        protected RequestProcessingStatus _requestProcessingStatus;

-        protected Streams _streams;

-        protected volatile bool _keepAlive;

-        protected string _methodText;

-        public HttpProtocol(HttpConnectionContext context);

-        public bool AllowSynchronousIO { get; set; }

-        public IFeatureCollection ConnectionFeatures { get; }

-        protected string ConnectionId { get; }

-        public string ConnectionIdFeature { get; set; }

-        public bool HasResponseStarted { get; }

-        public bool HasStartedConsumingRequestBody { get; set; }

-        protected HttpRequestHeaders HttpRequestHeaders { get; }

-        public IHttpResponseControl HttpResponseControl { get; set; }

-        protected HttpResponseHeaders HttpResponseHeaders { get; }

-        public string HttpVersion { get; set; }

-        public bool IsUpgradableRequest { get; private set; }

-        public bool IsUpgraded { get; set; }

-        public IPAddress LocalIpAddress { get; set; }

-        public int LocalPort { get; set; }

-        protected IKestrelTrace Log { get; }

-        public Nullable<long> MaxRequestBodySize { get; set; }

-        public HttpMethod Method { get; set; }

-        bool Microsoft.AspNetCore.Http.Features.IFeatureCollection.IsReadOnly { get; }

-        object Microsoft.AspNetCore.Http.Features.IFeatureCollection.this[Type key] { get; set; }

-        int Microsoft.AspNetCore.Http.Features.IFeatureCollection.Revision { get; }

-        bool Microsoft.AspNetCore.Http.Features.IHttpBodyControlFeature.AllowSynchronousIO { get; set; }

-        string Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.ConnectionId { get; set; }

-        IPAddress Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.LocalIpAddress { get; set; }

-        int Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.LocalPort { get; set; }

-        IPAddress Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.RemoteIpAddress { get; set; }

-        int Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.RemotePort { get; set; }

-        bool Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature.IsReadOnly { get; }

-        Nullable<long> Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature.MaxRequestBodySize { get; set; }

-        Stream Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Body { get; set; }

-        IHeaderDictionary Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Headers { get; set; }

-        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Method { get; set; }

-        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Path { get; set; }

-        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.PathBase { get; set; }

-        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Protocol { get; set; }

-        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.QueryString { get; set; }

-        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.RawTarget { get; set; }

-        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Scheme { get; set; }

-        string Microsoft.AspNetCore.Http.Features.IHttpRequestIdentifierFeature.TraceIdentifier { get; set; }

-        CancellationToken Microsoft.AspNetCore.Http.Features.IHttpRequestLifetimeFeature.RequestAborted { get; set; }

-        Stream Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.Body { get; set; }

-        bool Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.HasStarted { get; }

-        IHeaderDictionary Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.Headers { get; set; }

-        string Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.ReasonPhrase { get; set; }

-        int Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.StatusCode { get; set; }

-        bool Microsoft.AspNetCore.Http.Features.IHttpUpgradeFeature.IsUpgradableRequest { get; }

-        public IHttpOutputProducer Output { get; protected set; }

-        public string Path { get; set; }

-        public string PathBase { get; set; }

-        public string QueryString { get; set; }

-        public string RawTarget { get; set; }

-        public string ReasonPhrase { get; set; }

-        public IPAddress RemoteIpAddress { get; set; }

-        public int RemotePort { get; set; }

-        public CancellationToken RequestAborted { get; set; }

-        public Stream RequestBody { get; set; }

-        public Pipe RequestBodyPipe { get; protected set; }

-        public IHeaderDictionary RequestHeaders { get; set; }

-        public Stream ResponseBody { get; set; }

-        public IHeaderDictionary ResponseHeaders { get; set; }

-        public string Scheme { get; set; }

-        protected KestrelServerOptions ServerOptions { get; }

-        public ServiceContext ServiceContext { get; }

-        public int StatusCode { get; set; }

-        public ITimeoutControl TimeoutControl { get; }

-        public string TraceIdentifier { get; set; }

-        protected void AbortRequest();

-        protected abstract void ApplicationAbort();

-        protected virtual bool BeginRead(out ValueTask<ReadResult> awaitable);

-        protected virtual void BeginRequestProcessing();

-        protected abstract MessageBody CreateMessageBody();

-        protected abstract string CreateRequestId();

-        protected Task FireOnCompleted();

-        protected Task FireOnStarting();

-        public Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken));

-        public void HandleNonBodyResponseWrite();

-        public Task InitializeResponseAsync(int firstWriteByteCount);

-        public Task InitializeResponseAwaited(Task startingTask, int firstWriteByteCount);

-        public void InitializeStreams(MessageBody messageBody);

-        TFeature Microsoft.AspNetCore.Http.Features.IFeatureCollection.Get<TFeature>();

-        void Microsoft.AspNetCore.Http.Features.IFeatureCollection.Set<TFeature>(TFeature feature);

-        void Microsoft.AspNetCore.Http.Features.IHttpRequestLifetimeFeature.Abort();

-        void Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.OnCompleted(Func<object, Task> callback, object state);

-        void Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.OnStarting(Func<object, Task> callback, object state);

-        Task<Stream> Microsoft.AspNetCore.Http.Features.IHttpUpgradeFeature.UpgradeAsync();

-        public void OnCompleted(Func<object, Task> callback, object state);

-        protected virtual void OnErrorAfterResponseStarted();

-        public void OnHeader(Span<byte> name, Span<byte> value);

-        protected virtual void OnRequestProcessingEnded();

-        protected virtual void OnRequestProcessingEnding();

-        protected abstract void OnReset();

-        public void OnStarting(Func<object, Task> callback, object state);

-        protected void PoisonRequestBodyStream(Exception abortReason);

-        public Task ProcessRequestsAsync<TContext>(IHttpApplication<TContext> application);

-        public void ProduceContinue();

-        protected Task ProduceEnd();

-        protected void ReportApplicationError(Exception ex);

-        public void Reset();

-        protected void ResetHttp1Features();

-        protected void ResetHttp2Features();

-        public void SetBadRequestState(BadHttpRequestException ex);

-        public bool StatusCanHaveBody(int statusCode);

-        public void StopStreams();

-        IEnumerator<KeyValuePair<Type, object>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.Type,System.Object>>.GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-        public void ThrowRequestTargetRejected(Span<byte> target);

-        protected abstract bool TryParseRequest(ReadResult result, out bool endConnection);

-        protected Task TryProduceInvalidRequestResponse();

-        protected void VerifyResponseContentLength();

-        public Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default(CancellationToken));

-        public Task WriteAsyncAwaited(Task initializeTask, ReadOnlyMemory<byte> data, CancellationToken cancellationToken);

-    }
-    public class HttpRequestHeaders : HttpHeaders {
 {
-        public HttpRequestHeaders();

-        public bool HasConnection { get; }

-        public bool HasTransferEncoding { get; }

-        public StringValues HeaderAccept { get; set; }

-        public StringValues HeaderAcceptCharset { get; set; }

-        public StringValues HeaderAcceptEncoding { get; set; }

-        public StringValues HeaderAcceptLanguage { get; set; }

-        public StringValues HeaderAccessControlRequestHeaders { get; set; }

-        public StringValues HeaderAccessControlRequestMethod { get; set; }

-        public StringValues HeaderAllow { get; set; }

-        public StringValues HeaderAuthorization { get; set; }

-        public StringValues HeaderCacheControl { get; set; }

-        public StringValues HeaderConnection { get; set; }

-        public StringValues HeaderContentEncoding { get; set; }

-        public StringValues HeaderContentLanguage { get; set; }

-        public StringValues HeaderContentLength { get; set; }

-        public StringValues HeaderContentLocation { get; set; }

-        public StringValues HeaderContentMD5 { get; set; }

-        public StringValues HeaderContentRange { get; set; }

-        public StringValues HeaderContentType { get; set; }

-        public StringValues HeaderCookie { get; set; }

-        public StringValues HeaderDate { get; set; }

-        public StringValues HeaderExpect { get; set; }

-        public StringValues HeaderExpires { get; set; }

-        public StringValues HeaderFrom { get; set; }

-        public StringValues HeaderHost { get; set; }

-        public StringValues HeaderIfMatch { get; set; }

-        public StringValues HeaderIfModifiedSince { get; set; }

-        public StringValues HeaderIfNoneMatch { get; set; }

-        public StringValues HeaderIfRange { get; set; }

-        public StringValues HeaderIfUnmodifiedSince { get; set; }

-        public StringValues HeaderKeepAlive { get; set; }

-        public StringValues HeaderLastModified { get; set; }

-        public StringValues HeaderMaxForwards { get; set; }

-        public StringValues HeaderOrigin { get; set; }

-        public StringValues HeaderPragma { get; set; }

-        public StringValues HeaderProxyAuthorization { get; set; }

-        public StringValues HeaderRange { get; set; }

-        public StringValues HeaderReferer { get; set; }

-        public StringValues HeaderTE { get; set; }

-        public StringValues HeaderTrailer { get; set; }

-        public StringValues HeaderTransferEncoding { get; set; }

-        public StringValues HeaderTranslate { get; set; }

-        public StringValues HeaderUpgrade { get; set; }

-        public StringValues HeaderUserAgent { get; set; }

-        public StringValues HeaderVia { get; set; }

-        public StringValues HeaderWarning { get; set; }

-        public int HostCount { get; }

-        protected override bool AddValueFast(string key, in StringValues value);

-        public unsafe void Append(byte* pKeyBytes, int keyLength, string value);

-        public void Append(Span<byte> name, string value);

-        protected override void ClearFast();

-        protected override bool CopyToFast(KeyValuePair<string, StringValues>[] array, int arrayIndex);

-        protected override int GetCountFast();

-        public HttpRequestHeaders.Enumerator GetEnumerator();

-        protected override IEnumerator<KeyValuePair<string, StringValues>> GetEnumeratorFast();

-        protected override bool RemoveFast(string key);

-        protected override void SetValueFast(string key, in StringValues value);

-        protected override bool TryGetValueFast(string key, out StringValues value);

-        public struct Enumerator : IDisposable, IEnumerator, IEnumerator<KeyValuePair<string, StringValues>> {
 {
-            public KeyValuePair<string, StringValues> Current { get; }

-            object System.Collections.IEnumerator.Current { get; }

-            public void Dispose();

-            public bool MoveNext();

-            public void Reset();

-        }
-    }
-    public enum HttpRequestTarget {
 {
-        AbsoluteForm = 1,

-        AsteriskForm = 3,

-        AuthorityForm = 2,

-        OriginForm = 0,

-        Unknown = -1,

-    }
-    public class HttpResponseHeaders : HttpHeaders {
 {
-        public HttpResponseHeaders();

-        public bool HasConnection { get; }

-        public bool HasDate { get; }

-        public bool HasServer { get; }

-        public bool HasTransferEncoding { get; }

-        public StringValues HeaderAcceptRanges { get; set; }

-        public StringValues HeaderAccessControlAllowCredentials { get; set; }

-        public StringValues HeaderAccessControlAllowHeaders { get; set; }

-        public StringValues HeaderAccessControlAllowMethods { get; set; }

-        public StringValues HeaderAccessControlAllowOrigin { get; set; }

-        public StringValues HeaderAccessControlExposeHeaders { get; set; }

-        public StringValues HeaderAccessControlMaxAge { get; set; }

-        public StringValues HeaderAge { get; set; }

-        public StringValues HeaderAllow { get; set; }

-        public StringValues HeaderCacheControl { get; set; }

-        public StringValues HeaderConnection { get; set; }

-        public StringValues HeaderContentEncoding { get; set; }

-        public StringValues HeaderContentLanguage { get; set; }

-        public StringValues HeaderContentLength { get; set; }

-        public StringValues HeaderContentLocation { get; set; }

-        public StringValues HeaderContentMD5 { get; set; }

-        public StringValues HeaderContentRange { get; set; }

-        public StringValues HeaderContentType { get; set; }

-        public StringValues HeaderDate { get; set; }

-        public StringValues HeaderETag { get; set; }

-        public StringValues HeaderExpires { get; set; }

-        public StringValues HeaderKeepAlive { get; set; }

-        public StringValues HeaderLastModified { get; set; }

-        public StringValues HeaderLocation { get; set; }

-        public StringValues HeaderPragma { get; set; }

-        public StringValues HeaderProxyAuthenticate { get; set; }

-        public StringValues HeaderRetryAfter { get; set; }

-        public StringValues HeaderServer { get; set; }

-        public StringValues HeaderSetCookie { get; set; }

-        public StringValues HeaderTrailer { get; set; }

-        public StringValues HeaderTransferEncoding { get; set; }

-        public StringValues HeaderUpgrade { get; set; }

-        public StringValues HeaderVary { get; set; }

-        public StringValues HeaderVia { get; set; }

-        public StringValues HeaderWarning { get; set; }

-        public StringValues HeaderWWWAuthenticate { get; set; }

-        protected override bool AddValueFast(string key, in StringValues value);

-        protected override void ClearFast();

-        protected override bool CopyToFast(KeyValuePair<string, StringValues>[] array, int arrayIndex);

-        protected override int GetCountFast();

-        public HttpResponseHeaders.Enumerator GetEnumerator();

-        protected override IEnumerator<KeyValuePair<string, StringValues>> GetEnumeratorFast();

-        protected override bool RemoveFast(string key);

-        public void SetRawConnection(in StringValues value, byte[] raw);

-        public void SetRawDate(in StringValues value, byte[] raw);

-        public void SetRawServer(in StringValues value, byte[] raw);

-        public void SetRawTransferEncoding(in StringValues value, byte[] raw);

-        protected override void SetValueFast(string key, in StringValues value);

-        protected override bool TryGetValueFast(string key, out StringValues value);

-        public struct Enumerator : IDisposable, IEnumerator, IEnumerator<KeyValuePair<string, StringValues>> {
 {
-            public KeyValuePair<string, StringValues> Current { get; }

-            object System.Collections.IEnumerator.Current { get; }

-            public void Dispose();

-            public bool MoveNext();

-            public void Reset();

-        }
-    }
-    public class HttpResponseTrailers : HttpHeaders {
 {
-        public HttpResponseTrailers();

-        public StringValues HeaderETag { get; set; }

-        protected override bool AddValueFast(string key, in StringValues value);

-        protected override void ClearFast();

-        protected override bool CopyToFast(KeyValuePair<string, StringValues>[] array, int arrayIndex);

-        protected override int GetCountFast();

-        public HttpResponseTrailers.Enumerator GetEnumerator();

-        protected override IEnumerator<KeyValuePair<string, StringValues>> GetEnumeratorFast();

-        protected override bool RemoveFast(string key);

-        protected override void SetValueFast(string key, in StringValues value);

-        protected override bool TryGetValueFast(string key, out StringValues value);

-        public struct Enumerator : IDisposable, IEnumerator, IEnumerator<KeyValuePair<string, StringValues>> {
 {
-            public KeyValuePair<string, StringValues> Current { get; }

-            object System.Collections.IEnumerator.Current { get; }

-            public void Dispose();

-            public bool MoveNext();

-            public void Reset();

-        }
-    }
     public enum HttpScheme {
         Http = 0,
         Https = 1,
         Unknown = -1,
     }
     public enum HttpVersion {
         Http10 = 0,
         Http11 = 1,
         Http2 = 2,
         Unknown = -1,
     }
     public interface IHttpHeadersHandler {
         void OnHeader(Span<byte> name, Span<byte> value);
+        void OnHeadersComplete();
     }
-    public interface IHttpOutputAborter {
 {
-        void Abort(ConnectionAbortedException abortReason);

-    }
-    public interface IHttpOutputProducer {
 {
-        Task FlushAsync(CancellationToken cancellationToken);

-        Task Write100ContinueAsync();

-        Task WriteAsync<T>(Func<PipeWriter, T, long> callback, T state, CancellationToken cancellationToken);

-        Task WriteDataAsync(ReadOnlySpan<byte> data, CancellationToken cancellationToken);

-        void WriteResponseHeaders(int statusCode, string ReasonPhrase, HttpResponseHeaders responseHeaders);

-        Task WriteStreamSuffixAsync();

-    }
     public interface IHttpParser<TRequestHandler> where TRequestHandler : IHttpHeadersHandler, IHttpRequestLineHandler {
-        bool ParseHeaders(TRequestHandler handler, in ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined, out int consumedBytes);

+        bool ParseHeaders(TRequestHandler handler, ref SequenceReader<byte> reader);
         bool ParseRequestLine(TRequestHandler handler, in ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined);
     }
     public interface IHttpRequestLineHandler {
         void OnStartLine(HttpMethod method, HttpVersion version, Span<byte> target, Span<byte> path, Span<byte> query, Span<byte> customMethod, bool pathEncoded);
     }
-    public interface IHttpResponseControl {
 {
-        Task FlushAsync(CancellationToken cancellationToken);

-        void ProduceContinue();

-        Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken);

-    }
-    public abstract class MessageBody {
 {
-        protected MessageBody(HttpProtocol context, MinDataRate minRequestBodyDataRate);

-        public virtual bool IsEmpty { get; }

-        protected IKestrelTrace Log { get; }

-        public bool RequestKeepAlive { get; protected set; }

-        public bool RequestUpgrade { get; protected set; }

-        public static MessageBody ZeroContentLengthClose { get; }

-        public static MessageBody ZeroContentLengthKeepAlive { get; }

-        protected void AddAndCheckConsumedBytes(long consumedBytes);

-        public virtual Task ConsumeAsync();

-        public virtual Task CopyToAsync(Stream destination, CancellationToken cancellationToken = default(CancellationToken));

-        protected virtual Task OnConsumeAsync();

-        protected virtual void OnDataRead(long bytesRead);

-        protected virtual void OnReadStarted();

-        protected virtual void OnReadStarting();

-        protected virtual Task OnStopAsync();

-        public virtual ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken));

-        public virtual Task StopAsync();

-        protected void TryProduceContinue();

-    }
-    public static class PathNormalizer {
 {
-        public unsafe static bool ContainsDotSegments(byte* start, byte* end);

-        public static string DecodePath(Span<byte> path, bool pathEncoded, string rawTarget, int queryLength);

-        public unsafe static int RemoveDotSegments(byte* start, byte* end);

-        public static int RemoveDotSegments(Span<byte> input);

-    }
-    public static class PipelineExtensions {
 {
-        public static ArraySegment<byte> GetArray(this Memory<byte> buffer);

-        public static ArraySegment<byte> GetArray(this ReadOnlyMemory<byte> memory);

-        public static ReadOnlySpan<byte> ToSpan(this ReadOnlySequence<byte> buffer);

-    }
-    public enum ProduceEndType {
 {
-        ConnectionKeepAlive = 2,

-        SocketDisconnect = 1,

-        SocketShutdown = 0,

-    }
-    public static class ReasonPhrases {
 {
-        public static byte[] ToStatusBytes(int statusCode, string reasonPhrase = null);

-    }
-    public enum RequestProcessingStatus {
 {
-        AppStarted = 3,

-        ParsingHeaders = 2,

-        ParsingRequestLine = 1,

-        RequestPending = 0,

-        ResponseStarted = 4,

-    }
-    public enum RequestRejectionReason {
 {
-        BadChunkSizeData = 9,

-        BadChunkSuffix = 8,

-        ChunkedRequestIncomplete = 10,

-        ConnectMethodRequired = 23,

-        FinalTransferCodingNotChunked = 19,

-        HeadersExceedMaxTotalSize = 14,

-        InvalidCharactersInHeaderName = 12,

-        InvalidContentLength = 5,

-        InvalidHostHeader = 26,

-        InvalidRequestHeader = 2,

-        InvalidRequestHeadersNoCRLF = 3,

-        InvalidRequestLine = 1,

-        InvalidRequestTarget = 11,

-        LengthRequired = 20,

-        LengthRequiredHttp10 = 21,

-        MalformedRequestInvalidHeaders = 4,

-        MissingHostHeader = 24,

-        MultipleContentLengths = 6,

-        MultipleHostHeaders = 25,

-        OptionsMethodRequired = 22,

-        RequestBodyExceedsContentLength = 28,

-        RequestBodyTimeout = 18,

-        RequestBodyTooLarge = 16,

-        RequestHeadersTimeout = 17,

-        RequestLineTooLong = 13,

-        TooManyHeaders = 15,

-        UnexpectedEndOfRequestContent = 7,

-        UnrecognizedHTTPVersion = 0,

-        UpgradeRequestCannotHavePayload = 27,

-    }
-    public enum TransferCoding {
 {
-        Chunked = 1,

-        None = 0,

-        Other = 2,

-    }
 }
```

