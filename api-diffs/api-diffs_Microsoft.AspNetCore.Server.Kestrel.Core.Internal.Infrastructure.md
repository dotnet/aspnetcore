# Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure

``` diff
-namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure {
 {
-    public class ConnectionManager {
 {
-        public ConnectionManager(IKestrelTrace trace, ResourceCounter upgradedConnections);

-        public ConnectionManager(IKestrelTrace trace, Nullable<long> upgradedConnectionLimit);

-        public ResourceCounter UpgradedConnectionCount { get; }

-        public void AddConnection(long id, KestrelConnection connection);

-        public void RemoveConnection(long id);

-        public void Walk(Action<KestrelConnection> callback);

-    }
-    public static class ConnectionManagerShutdownExtensions {
 {
-        public static Task<bool> AbortAllConnectionsAsync(this ConnectionManager connectionManager);

-        public static Task<bool> CloseAllConnectionsAsync(this ConnectionManager connectionManager, CancellationToken token);

-    }
-    public class ConnectionReference {
 {
-        public ConnectionReference(KestrelConnection connection);

-        public string ConnectionId { get; }

-        public bool TryGetConnection(out KestrelConnection connection);

-    }
-    public class Disposable : IDisposable {
 {
-        public Disposable(Action dispose);

-        public void Dispose();

-        protected virtual void Dispose(bool disposing);

-    }
-    public class Heartbeat : IDisposable {
 {
-        public static readonly TimeSpan Interval;

-        public Heartbeat(IHeartbeatHandler[] callbacks, ISystemClock systemClock, IDebugger debugger, IKestrelTrace trace);

-        public void Dispose();

-        public void Start();

-    }
-    public class HeartbeatManager : IHeartbeatHandler, ISystemClock {
 {
-        public HeartbeatManager(ConnectionManager connectionManager);

-        public DateTimeOffset UtcNow { get; }

-        public void OnHeartbeat(DateTimeOffset now);

-    }
-    public static class HttpUtilities {
 {
-        public const string Http10Version = "HTTP/1.0";

-        public const string Http11Version = "HTTP/1.1";

-        public const string Http2Version = "HTTP/2";

-        public const string HttpsUriScheme = "https://";

-        public const string HttpUriScheme = "http://";

-        public static string GetAsciiOrUTF8StringNonNullCharacters(this Span<byte> span);

-        public static string GetAsciiStringEscaped(this Span<byte> span, int maxChars);

-        public static string GetAsciiStringNonNullCharacters(this Span<byte> span);

-        public static bool GetKnownHttpScheme(this Span<byte> span, out HttpScheme knownScheme);

-        public static bool GetKnownMethod(this Span<byte> span, out HttpMethod method, out int length);

-        public static HttpMethod GetKnownMethod(string value);

-        public static bool GetKnownVersion(this Span<byte> span, out HttpVersion knownVersion, out byte length);

-        public static bool IsHostHeaderValid(string hostText);

-        public static string MethodToString(HttpMethod method);

-        public static string SchemeToString(HttpScheme scheme);

-        public static string VersionToString(HttpVersion httpVersion);

-    }
-    public interface IDebugger {
 {
-        bool IsAttached { get; }

-    }
-    public interface IHeartbeatHandler {
 {
-        void OnHeartbeat(DateTimeOffset now);

-    }
-    public interface IKestrelTrace : ILogger {
 {
-        void ApplicationAbortedConnection(string connectionId, string traceIdentifier);

-        void ApplicationError(string connectionId, string traceIdentifier, Exception ex);

-        void ApplicationNeverCompleted(string connectionId);

-        void ConnectionBadRequest(string connectionId, BadHttpRequestException ex);

-        void ConnectionDisconnect(string connectionId);

-        void ConnectionHeadResponseBodyWrite(string connectionId, long count);

-        void ConnectionKeepAlive(string connectionId);

-        void ConnectionPause(string connectionId);

-        void ConnectionRejected(string connectionId);

-        void ConnectionResume(string connectionId);

-        void ConnectionStart(string connectionId);

-        void ConnectionStop(string connectionId);

-        void HeartbeatSlow(TimeSpan interval, DateTimeOffset now);

-        void HPackDecodingError(string connectionId, int streamId, HPackDecodingException ex);

-        void HPackEncodingError(string connectionId, int streamId, HPackEncodingException ex);

-        void Http2ConnectionClosed(string connectionId, int highestOpenedStreamId);

-        void Http2ConnectionClosing(string connectionId);

-        void Http2ConnectionError(string connectionId, Http2ConnectionErrorException ex);

-        void Http2FrameReceived(string connectionId, Http2Frame frame);

-        void Http2FrameSending(string connectionId, Http2Frame frame);

-        void Http2StreamError(string connectionId, Http2StreamErrorException ex);

-        void Http2StreamResetAbort(string traceIdentifier, Http2ErrorCode error, ConnectionAbortedException abortReason);

-        void NotAllConnectionsAborted();

-        void NotAllConnectionsClosedGracefully();

-        void RequestBodyDone(string connectionId, string traceIdentifier);

-        void RequestBodyDrainTimedOut(string connectionId, string traceIdentifier);

-        void RequestBodyMinimumDataRateNotSatisfied(string connectionId, string traceIdentifier, double rate);

-        void RequestBodyNotEntirelyRead(string connectionId, string traceIdentifier);

-        void RequestBodyStart(string connectionId, string traceIdentifier);

-        void RequestProcessingError(string connectionId, Exception ex);

-        void ResponseMinimumDataRateNotSatisfied(string connectionId, string traceIdentifier);

-    }
-    public interface ISystemClock {
 {
-        DateTimeOffset UtcNow { get; }

-    }
-    public interface ITimeoutControl {
 {
-        TimeoutReason TimerReason { get; }

-        void BytesRead(long count);

-        void BytesWrittenToBuffer(MinDataRate minRate, long count);

-        void CancelTimeout();

-        void InitializeHttp2(InputFlowControl connectionInputFlowControl);

-        void ResetTimeout(long ticks, TimeoutReason timeoutReason);

-        void SetTimeout(long ticks, TimeoutReason timeoutReason);

-        void StartRequestBody(MinDataRate minRate);

-        void StartTimingRead();

-        void StartTimingWrite();

-        void StopRequestBody();

-        void StopTimingRead();

-        void StopTimingWrite();

-    }
-    public interface ITimeoutHandler {
 {
-        void OnTimeout(TimeoutReason reason);

-    }
-    public class KestrelConnection {
 {
-        public KestrelConnection(TransportConnection transportConnection);

-        public Task ExecutionTask { get; }

-        public TransportConnection TransportConnection { get; }

-    }
-    public sealed class KestrelEventSource : EventSource {
 {
-        public static readonly KestrelEventSource Log;

-        public void ConnectionRejected(string connectionId);

-        public void ConnectionStart(TransportConnection connection);

-        public void ConnectionStop(TransportConnection connection);

-        public void RequestStart(HttpProtocol httpProtocol);

-        public void RequestStop(HttpProtocol httpProtocol);

-    }
-    public abstract class ReadOnlyStream : Stream {
 {
-        protected ReadOnlyStream();

-        public override bool CanRead { get; }

-        public override bool CanWrite { get; }

-        public override int WriteTimeout { get; set; }

-        public override void Write(byte[] buffer, int offset, int count);

-        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);

-    }
-    public abstract class ResourceCounter {
 {
-        protected ResourceCounter();

-        public static ResourceCounter Unlimited { get; }

-        public static ResourceCounter Quota(long amount);

-        public abstract void ReleaseOne();

-        public abstract bool TryLockOne();

-    }
-    public class Streams {
 {
-        public Streams(IHttpBodyControlFeature bodyControl, IHttpResponseControl httpResponseControl);

-        public void Abort(Exception error);

-        public ValueTuple<Stream, Stream> Start(MessageBody body);

-        public void Stop();

-        public Stream Upgrade();

-    }
-    public class ThrowingWasUpgradedWriteOnlyStream : WriteOnlyStream {
 {
-        public ThrowingWasUpgradedWriteOnlyStream();

-        public override bool CanSeek { get; }

-        public override long Length { get; }

-        public override long Position { get; set; }

-        public override void Flush();

-        public override long Seek(long offset, SeekOrigin origin);

-        public override void SetLength(long value);

-        public override void Write(byte[] buffer, int offset, int count);

-        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);

-    }
-    public class TimeoutControl : IConnectionTimeoutFeature, ITimeoutControl {
 {
-        public TimeoutControl(ITimeoutHandler timeoutHandler);

-        public TimeoutReason TimerReason { get; private set; }

-        public void BytesRead(long count);

-        public void BytesWrittenToBuffer(MinDataRate minRate, long count);

-        public void CancelTimeout();

-        public void Initialize(DateTimeOffset now);

-        public void InitializeHttp2(InputFlowControl connectionInputFlowControl);

-        void Microsoft.AspNetCore.Server.Kestrel.Core.Features.IConnectionTimeoutFeature.ResetTimeout(TimeSpan timeSpan);

-        void Microsoft.AspNetCore.Server.Kestrel.Core.Features.IConnectionTimeoutFeature.SetTimeout(TimeSpan timeSpan);

-        public void ResetTimeout(long ticks, TimeoutReason timeoutReason);

-        public void SetTimeout(long ticks, TimeoutReason timeoutReason);

-        public void StartRequestBody(MinDataRate minRate);

-        public void StartTimingRead();

-        public void StartTimingWrite();

-        public void StopRequestBody();

-        public void StopTimingRead();

-        public void StopTimingWrite();

-        public void Tick(DateTimeOffset now);

-    }
-    public static class TimeoutControlExtensions {
 {
-        public static void StartDrainTimeout(this ITimeoutControl timeoutControl, MinDataRate minDataRate, Nullable<long> maxResponseBufferSize);

-    }
-    public enum TimeoutReason {
 {
-        KeepAlive = 1,

-        None = 0,

-        ReadDataRate = 3,

-        RequestBodyDrain = 5,

-        RequestHeaders = 2,

-        TimeoutFeature = 6,

-        WriteDataRate = 4,

-    }
-    public class TimingPipeFlusher {
 {
-        public TimingPipeFlusher(PipeWriter writer, ITimeoutControl timeoutControl, IKestrelTrace log);

-        public Task FlushAsync();

-        public Task FlushAsync(IHttpOutputAborter outputAborter, CancellationToken cancellationToken);

-        public Task FlushAsync(MinDataRate minRate, long count);

-        public Task FlushAsync(MinDataRate minRate, long count, IHttpOutputAborter outputAborter, CancellationToken cancellationToken);

-    }
-    public abstract class WriteOnlyStream : Stream {
 {
-        protected WriteOnlyStream();

-        public override bool CanRead { get; }

-        public override bool CanWrite { get; }

-        public override int ReadTimeout { get; set; }

-        public override int Read(byte[] buffer, int offset, int count);

-        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);

-    }
-}
```

