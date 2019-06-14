# Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2

``` diff
-namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2 {
 {
-    public class Http2Connection : IHttp2StreamLifetimeHandler, IHttpHeadersHandler, IRequestProcessor {
 {
-        public Http2Connection(HttpConnectionContext context);

-        public static byte[] ClientPreface { get; }

-        public IFeatureCollection ConnectionFeatures { get; }

-        public string ConnectionId { get; }

-        public PipeReader Input { get; }

-        public KestrelServerLimits Limits { get; }

-        public IKestrelTrace Log { get; }

-        public ITimeoutControl TimeoutControl { get; }

-        public void Abort(ConnectionAbortedException ex);

-        public void HandleReadDataRateTimeout();

-        public void HandleRequestHeadersTimeout();

-        void Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.IHttp2StreamLifetimeHandler.OnStreamCompleted(int streamId);

-        void Microsoft.AspNetCore.Server.Kestrel.Core.Internal.IRequestProcessor.Tick(DateTimeOffset now);

-        public void OnHeader(Span<byte> name, Span<byte> value);

-        public void OnInputOrOutputCompleted();

-        public Task ProcessRequestsAsync<TContext>(IHttpApplication<TContext> application);

-        public void StopProcessingNextRequest();

-        public void StopProcessingNextRequest(bool sendGracefulGoAway = false);

-    }
-    public class Http2ConnectionErrorException : Exception {
 {
-        public Http2ConnectionErrorException(string message, Http2ErrorCode errorCode);

-        public Http2ErrorCode ErrorCode { get; }

-    }
-    public enum Http2ConnectionState {
 {
-        Closed = 2,

-        Closing = 1,

-        Open = 0,

-    }
-    public enum Http2ContinuationFrameFlags : byte {
 {
-        END_HEADERS = (byte)4,

-        NONE = (byte)0,

-    }
-    public enum Http2DataFrameFlags : byte {
 {
-        END_STREAM = (byte)1,

-        NONE = (byte)0,

-        PADDED = (byte)8,

-    }
-    public enum Http2ErrorCode : uint {
 {
-        CANCEL = (uint)8,

-        COMPRESSION_ERROR = (uint)9,

-        CONNECT_ERROR = (uint)10,

-        ENHANCE_YOUR_CALM = (uint)11,

-        FLOW_CONTROL_ERROR = (uint)3,

-        FRAME_SIZE_ERROR = (uint)6,

-        HTTP_1_1_REQUIRED = (uint)13,

-        INADEQUATE_SECURITY = (uint)12,

-        INTERNAL_ERROR = (uint)2,

-        NO_ERROR = (uint)0,

-        PROTOCOL_ERROR = (uint)1,

-        REFUSED_STREAM = (uint)7,

-        SETTINGS_TIMEOUT = (uint)4,

-        STREAM_CLOSED = (uint)5,

-    }
-    public class Http2Frame {
 {
-        public Http2Frame();

-        public bool ContinuationEndHeaders { get; }

-        public Http2ContinuationFrameFlags ContinuationFlags { get; set; }

-        public bool DataEndStream { get; }

-        public Http2DataFrameFlags DataFlags { get; set; }

-        public bool DataHasPadding { get; }

-        public byte DataPadLength { get; set; }

-        public int DataPayloadLength { get; }

-        public byte Flags { get; set; }

-        public Http2ErrorCode GoAwayErrorCode { get; set; }

-        public int GoAwayLastStreamId { get; set; }

-        public bool HeadersEndHeaders { get; }

-        public bool HeadersEndStream { get; }

-        public Http2HeadersFrameFlags HeadersFlags { get; set; }

-        public bool HeadersHasPadding { get; }

-        public bool HeadersHasPriority { get; }

-        public byte HeadersPadLength { get; set; }

-        public int HeadersPayloadLength { get; }

-        public byte HeadersPriorityWeight { get; set; }

-        public int HeadersStreamDependency { get; set; }

-        public int PayloadLength { get; set; }

-        public bool PingAck { get; }

-        public Http2PingFrameFlags PingFlags { get; set; }

-        public bool PriorityIsExclusive { get; set; }

-        public int PriorityStreamDependency { get; set; }

-        public byte PriorityWeight { get; set; }

-        public Http2ErrorCode RstStreamErrorCode { get; set; }

-        public bool SettingsAck { get; }

-        public Http2SettingsFrameFlags SettingsFlags { get; set; }

-        public int StreamId { get; set; }

-        public Http2FrameType Type { get; set; }

-        public int WindowUpdateSizeIncrement { get; set; }

-        public void PrepareContinuation(Http2ContinuationFrameFlags flags, int streamId);

-        public void PrepareData(int streamId, Nullable<byte> padLength = default(Nullable<byte>));

-        public void PrepareGoAway(int lastStreamId, Http2ErrorCode errorCode);

-        public void PrepareHeaders(Http2HeadersFrameFlags flags, int streamId);

-        public void PreparePing(Http2PingFrameFlags flags);

-        public void PreparePriority(int streamId, int streamDependency, bool exclusive, byte weight);

-        public void PrepareRstStream(int streamId, Http2ErrorCode errorCode);

-        public void PrepareSettings(Http2SettingsFrameFlags flags);

-        public void PrepareWindowUpdate(int streamId, int sizeIncrement);

-        public override string ToString();

-    }
-    public static class Http2FrameReader {
 {
-        public const int HeaderLength = 9;

-        public const int SettingSize = 6;

-        public static int GetPayloadFieldsLength(Http2Frame frame);

-        public static bool ReadFrame(ReadOnlySequence<byte> readableBuffer, Http2Frame frame, uint maxFrameSize, out ReadOnlySequence<byte> framePayload);

-        public static IList<Http2PeerSetting> ReadSettings(ReadOnlySequence<byte> payload);

-    }
-    public enum Http2FrameType : byte {
 {
-        CONTINUATION = (byte)9,

-        DATA = (byte)0,

-        GOAWAY = (byte)7,

-        HEADERS = (byte)1,

-        PING = (byte)6,

-        PRIORITY = (byte)2,

-        PUSH_PROMISE = (byte)5,

-        RST_STREAM = (byte)3,

-        SETTINGS = (byte)4,

-        WINDOW_UPDATE = (byte)8,

-    }
-    public class Http2FrameWriter {
 {
-        public Http2FrameWriter(PipeWriter outputPipeWriter, ConnectionContext connectionContext, Http2Connection http2Connection, OutputFlowControl connectionOutputFlowControl, ITimeoutControl timeoutControl, MinDataRate minResponseDataRate, string connectionId, IKestrelTrace log);

-        public void Abort(ConnectionAbortedException error);

-        public void AbortPendingStreamDataWrites(StreamOutputFlowControl flowControl);

-        public void Complete();

-        public Task FlushAsync(IHttpOutputAborter outputAborter, CancellationToken cancellationToken);

-        public bool TryUpdateConnectionWindow(int bytes);

-        public bool TryUpdateStreamWindow(StreamOutputFlowControl flowControl, int bytes);

-        public void UpdateMaxFrameSize(uint maxFrameSize);

-        public Task Write100ContinueAsync(int streamId);

-        public Task WriteDataAsync(int streamId, StreamOutputFlowControl flowControl, ReadOnlySequence<byte> data, bool endStream);

-        public Task WriteGoAwayAsync(int lastStreamId, Http2ErrorCode errorCode);

-        public Task WritePingAsync(Http2PingFrameFlags flags, ReadOnlySequence<byte> payload);

-        public void WriteResponseHeaders(int streamId, int statusCode, IHeaderDictionary headers);

-        public Task WriteResponseTrailers(int streamId, HttpResponseTrailers headers);

-        public Task WriteRstStreamAsync(int streamId, Http2ErrorCode errorCode);

-        public Task WriteSettingsAckAsync();

-        public Task WriteSettingsAsync(IList<Http2PeerSetting> settings);

-        public Task WriteWindowUpdateAsync(int streamId, int sizeIncrement);

-    }
-    public enum Http2HeadersFrameFlags : byte {
 {
-        END_HEADERS = (byte)4,

-        END_STREAM = (byte)1,

-        NONE = (byte)0,

-        PADDED = (byte)8,

-        PRIORITY = (byte)32,

-    }
-    public class Http2MessageBody : MessageBody {
 {
-        public static MessageBody For(Http2Stream context, MinDataRate minRequestBodyDataRate);

-        protected override void OnDataRead(long bytesRead);

-        protected override void OnReadStarted();

-        protected override void OnReadStarting();

-    }
-    public class Http2OutputProducer : IHttpOutputAborter, IHttpOutputProducer {
 {
-        public Http2OutputProducer(int streamId, Http2FrameWriter frameWriter, StreamOutputFlowControl flowControl, ITimeoutControl timeoutControl, MemoryPool<byte> pool, Http2Stream stream, IKestrelTrace log);

-        public void Dispose();

-        public Task FlushAsync(CancellationToken cancellationToken);

-        void Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpOutputAborter.Abort(ConnectionAbortedException abortReason);

-        public Task Write100ContinueAsync();

-        public Task WriteAsync<T>(Func<PipeWriter, T, long> callback, T state, CancellationToken cancellationToken);

-        public Task WriteDataAsync(ReadOnlySpan<byte> data, CancellationToken cancellationToken);

-        public void WriteResponseHeaders(int statusCode, string ReasonPhrase, HttpResponseHeaders responseHeaders);

-        public Task WriteRstStreamAsync(Http2ErrorCode error);

-        public Task WriteStreamSuffixAsync();

-    }
-    public struct Http2PeerSetting {
 {
-        public Http2PeerSetting(Http2SettingsParameter parameter, uint value);

-        public Http2SettingsParameter Parameter { get; }

-        public uint Value { get; }

-    }
-    public class Http2PeerSettings {
 {
-        public const bool DefaultEnablePush = true;

-        public const uint DefaultHeaderTableSize = (uint)4096;

-        public const uint DefaultInitialWindowSize = (uint)65535;

-        public const uint DefaultMaxConcurrentStreams = (uint)4294967295;

-        public const uint DefaultMaxFrameSize = (uint)16384;

-        public const uint DefaultMaxHeaderListSize = (uint)4294967295;

-        public const uint MaxWindowSize = (uint)2147483647;

-        public Http2PeerSettings();

-        public bool EnablePush { get; set; }

-        public uint HeaderTableSize { get; set; }

-        public uint InitialWindowSize { get; set; }

-        public uint MaxConcurrentStreams { get; set; }

-        public uint MaxFrameSize { get; set; }

-        public uint MaxHeaderListSize { get; set; }

-        public void Update(IList<Http2PeerSetting> settings);

-    }
-    public enum Http2PingFrameFlags : byte {
 {
-        ACK = (byte)1,

-        NONE = (byte)0,

-    }
-    public enum Http2SettingsFrameFlags : byte {
 {
-        ACK = (byte)1,

-        NONE = (byte)0,

-    }
-    public enum Http2SettingsParameter : ushort {
 {
-        SETTINGS_ENABLE_PUSH = (ushort)2,

-        SETTINGS_HEADER_TABLE_SIZE = (ushort)1,

-        SETTINGS_INITIAL_WINDOW_SIZE = (ushort)4,

-        SETTINGS_MAX_CONCURRENT_STREAMS = (ushort)3,

-        SETTINGS_MAX_FRAME_SIZE = (ushort)5,

-        SETTINGS_MAX_HEADER_LIST_SIZE = (ushort)6,

-    }
-    public class Http2SettingsParameterOutOfRangeException : Exception {
 {
-        public Http2SettingsParameterOutOfRangeException(Http2SettingsParameter parameter, long lowerBound, long upperBound);

-        public Http2SettingsParameter Parameter { get; }

-    }
-    public class Http2Stream : HttpProtocol, IHttp2StreamIdFeature, IHttpResponseTrailersFeature {
 {
-        public Http2Stream(Http2StreamContext context);

-        public bool EndStreamReceived { get; }

-        public Nullable<long> InputRemaining { get; internal set; }

-        IHeaderDictionary Microsoft.AspNetCore.Http.Features.IHttpResponseTrailersFeature.Trailers { get; set; }

-        int Microsoft.AspNetCore.Server.Kestrel.Core.Features.IHttp2StreamIdFeature.StreamId { get; }

-        public bool RequestBodyStarted { get; private set; }

-        public int StreamId { get; }

-        public void Abort(IOException abortReason);

-        public void AbortRstStreamReceived();

-        protected override void ApplicationAbort();

-        protected override MessageBody CreateMessageBody();

-        protected override string CreateRequestId();

-        public Task OnDataAsync(Http2Frame dataFrame, ReadOnlySequence<byte> payload);

-        public void OnDataRead(int bytesRead);

-        public void OnEndStreamReceived();

-        protected override void OnErrorAfterResponseStarted();

-        protected override void OnRequestProcessingEnded();

-        protected override void OnReset();

-        protected override bool TryParseRequest(ReadResult result, out bool endConnection);

-        public bool TryUpdateOutputWindow(int bytes);

-    }
-    public class Http2StreamContext : HttpConnectionContext {
 {
-        public Http2StreamContext();

-        public Http2PeerSettings ClientPeerSettings { get; set; }

-        public InputFlowControl ConnectionInputFlowControl { get; set; }

-        public OutputFlowControl ConnectionOutputFlowControl { get; set; }

-        public Http2FrameWriter FrameWriter { get; set; }

-        public Http2PeerSettings ServerPeerSettings { get; set; }

-        public int StreamId { get; set; }

-        public IHttp2StreamLifetimeHandler StreamLifetimeHandler { get; set; }

-    }
-    public class Http2StreamErrorException : Exception {
 {
-        public Http2StreamErrorException(int streamId, string message, Http2ErrorCode errorCode);

-        public Http2ErrorCode ErrorCode { get; }

-        public int StreamId { get; }

-    }
-    public interface IHttp2StreamLifetimeHandler {
 {
-        void OnStreamCompleted(int streamId);

-    }
-    public class ThreadPoolAwaitable : ICriticalNotifyCompletion, INotifyCompletion {
 {
-        public static ThreadPoolAwaitable Instance;

-        public bool IsCompleted { get; }

-        public ThreadPoolAwaitable GetAwaiter();

-        public void GetResult();

-        public void OnCompleted(Action continuation);

-        public void UnsafeOnCompleted(Action continuation);

-    }
-}
```

