# Microsoft.AspNetCore.Server.Kestrel.Core.Internal

``` diff
-namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal {
 {
-    public class ConnectionDispatcher : IConnectionDispatcher {
 {
-        public ConnectionDispatcher(ServiceContext serviceContext, ConnectionDelegate connectionDelegate);

-        public Task OnConnection(TransportConnection connection);

-    }
-    public class ConnectionLimitMiddleware {
 {
-        public ConnectionLimitMiddleware(ConnectionDelegate next, long connectionLimit, IKestrelTrace trace);

-        public Task OnConnectionAsync(ConnectionContext connection);

-    }
-    public class ConnectionLogScope : IEnumerable, IEnumerable<KeyValuePair<string, object>>, IReadOnlyCollection<KeyValuePair<string, object>>, IReadOnlyList<KeyValuePair<string, object>> {
 {
-        public ConnectionLogScope(string connectionId);

-        public int Count { get; }

-        public KeyValuePair<string, object> this[int index] { get; }

-        public IEnumerator<KeyValuePair<string, object>> GetEnumerator();

-        IEnumerator System.Collections.IEnumerable.GetEnumerator();

-        public override string ToString();

-    }
-    public class HttpConnection : ITimeoutHandler {
 {
-        public HttpConnection(HttpConnectionContext context);

-        public string ConnectionId { get; }

-        public IPEndPoint LocalEndPoint { get; }

-        public IPEndPoint RemoteEndPoint { get; }

-        public void OnTimeout(TimeoutReason reason);

-        public Task ProcessRequestsAsync<TContext>(IHttpApplication<TContext> httpApplication);

-    }
-    public static class HttpConnectionBuilderExtensions {
 {
-        public static IConnectionBuilder UseHttpServer<TContext>(this IConnectionBuilder builder, ServiceContext serviceContext, IHttpApplication<TContext> application, HttpProtocols protocols);

-        public static IConnectionBuilder UseHttpServer<TContext>(this IConnectionBuilder builder, IList<IConnectionAdapter> adapters, ServiceContext serviceContext, IHttpApplication<TContext> application, HttpProtocols protocols);

-    }
-    public class HttpConnectionContext {
 {
-        public HttpConnectionContext();

-        public IList<IConnectionAdapter> ConnectionAdapters { get; set; }

-        public ConnectionContext ConnectionContext { get; set; }

-        public IFeatureCollection ConnectionFeatures { get; set; }

-        public string ConnectionId { get; set; }

-        public IPEndPoint LocalEndPoint { get; set; }

-        public MemoryPool<byte> MemoryPool { get; set; }

-        public HttpProtocols Protocols { get; set; }

-        public IPEndPoint RemoteEndPoint { get; set; }

-        public ServiceContext ServiceContext { get; set; }

-        public ITimeoutControl TimeoutControl { get; set; }

-        public IDuplexPipe Transport { get; set; }

-    }
-    public class HttpConnectionMiddleware<TContext> {
 {
-        public HttpConnectionMiddleware(IList<IConnectionAdapter> adapters, ServiceContext serviceContext, IHttpApplication<TContext> application, HttpProtocols protocols);

-        public Task OnConnectionAsync(ConnectionContext connectionContext);

-    }
-    public interface IRequestProcessor {
 {
-        void Abort(ConnectionAbortedException ex);

-        void HandleReadDataRateTimeout();

-        void HandleRequestHeadersTimeout();

-        void OnInputOrOutputCompleted();

-        Task ProcessRequestsAsync<TContext>(IHttpApplication<TContext> application);

-        void StopProcessingNextRequest();

-        void Tick(DateTimeOffset now);

-    }
-    public class KestrelServerOptionsSetup : IConfigureOptions<KestrelServerOptions> {
 {
-        public KestrelServerOptionsSetup(IServiceProvider services);

-        public void Configure(KestrelServerOptions options);

-    }
-    public class KestrelTrace : IKestrelTrace, ILogger {
 {
-        protected readonly ILogger _logger;

-        public KestrelTrace(ILogger logger);

-        public virtual void ApplicationAbortedConnection(string connectionId, string traceIdentifier);

-        public virtual void ApplicationError(string connectionId, string traceIdentifier, Exception ex);

-        public virtual void ApplicationNeverCompleted(string connectionId);

-        public virtual IDisposable BeginScope<TState>(TState state);

-        public virtual void ConnectionBadRequest(string connectionId, BadHttpRequestException ex);

-        public virtual void ConnectionDisconnect(string connectionId);

-        public virtual void ConnectionHeadResponseBodyWrite(string connectionId, long count);

-        public virtual void ConnectionKeepAlive(string connectionId);

-        public virtual void ConnectionPause(string connectionId);

-        public virtual void ConnectionRejected(string connectionId);

-        public virtual void ConnectionResume(string connectionId);

-        public virtual void ConnectionStart(string connectionId);

-        public virtual void ConnectionStop(string connectionId);

-        public virtual void HeartbeatSlow(TimeSpan interval, DateTimeOffset now);

-        public virtual void HPackDecodingError(string connectionId, int streamId, HPackDecodingException ex);

-        public virtual void HPackEncodingError(string connectionId, int streamId, HPackEncodingException ex);

-        public virtual void Http2ConnectionClosed(string connectionId, int highestOpenedStreamId);

-        public virtual void Http2ConnectionClosing(string connectionId);

-        public virtual void Http2ConnectionError(string connectionId, Http2ConnectionErrorException ex);

-        public void Http2FrameReceived(string connectionId, Http2Frame frame);

-        public void Http2FrameSending(string connectionId, Http2Frame frame);

-        public virtual void Http2StreamError(string connectionId, Http2StreamErrorException ex);

-        public void Http2StreamResetAbort(string traceIdentifier, Http2ErrorCode error, ConnectionAbortedException abortReason);

-        public virtual bool IsEnabled(LogLevel logLevel);

-        public virtual void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter);

-        public virtual void NotAllConnectionsAborted();

-        public virtual void NotAllConnectionsClosedGracefully();

-        public virtual void RequestBodyDone(string connectionId, string traceIdentifier);

-        public virtual void RequestBodyDrainTimedOut(string connectionId, string traceIdentifier);

-        public virtual void RequestBodyMinimumDataRateNotSatisfied(string connectionId, string traceIdentifier, double rate);

-        public virtual void RequestBodyNotEntirelyRead(string connectionId, string traceIdentifier);

-        public virtual void RequestBodyStart(string connectionId, string traceIdentifier);

-        public virtual void RequestProcessingError(string connectionId, Exception ex);

-        public virtual void ResponseMinimumDataRateNotSatisfied(string connectionId, string traceIdentifier);

-    }
-    public class ServiceContext {
 {
-        public ServiceContext();

-        public ConnectionManager ConnectionManager { get; set; }

-        public DateHeaderValueManager DateHeaderValueManager { get; set; }

-        public Heartbeat Heartbeat { get; set; }

-        public IHttpParser<Http1ParsingHandler> HttpParser { get; set; }

-        public IKestrelTrace Log { get; set; }

-        public PipeScheduler Scheduler { get; set; }

-        public KestrelServerOptions ServerOptions { get; set; }

-        public ISystemClock SystemClock { get; set; }

-    }
-}
```

