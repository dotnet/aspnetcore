// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core
{
    public partial class KestrelServer : Microsoft.AspNetCore.Hosting.Server.IServer, System.IDisposable
    {
        internal KestrelServer(Microsoft.AspNetCore.Connections.IConnectionListenerFactory transportFactory, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.ServiceContext serviceContext) { }
    }
    public sealed partial class BadHttpRequestException : System.IO.IOException
    {
        internal Microsoft.Extensions.Primitives.StringValues AllowedHeader { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        internal Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.RequestRejectionReason Reason { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]internal static Microsoft.AspNetCore.Server.Kestrel.Core.BadHttpRequestException GetException(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.RequestRejectionReason reason) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]internal static Microsoft.AspNetCore.Server.Kestrel.Core.BadHttpRequestException GetException(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.RequestRejectionReason reason, string detail) { throw null; }
        [System.Diagnostics.StackTraceHiddenAttribute]
        internal static void Throw(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.RequestRejectionReason reason) { }
        [System.Diagnostics.StackTraceHiddenAttribute]
        internal static void Throw(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.RequestRejectionReason reason, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod method) { }
        [System.Diagnostics.StackTraceHiddenAttribute]
        internal static void Throw(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.RequestRejectionReason reason, Microsoft.Extensions.Primitives.StringValues detail) { }
        [System.Diagnostics.StackTraceHiddenAttribute]
        internal static void Throw(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.RequestRejectionReason reason, string detail) { }
    }
    internal sealed partial class LocalhostListenOptions : Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions
    {
        internal LocalhostListenOptions(int port) : base (default(System.Net.IPEndPoint)) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        internal override System.Threading.Tasks.Task BindAsync(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.AddressBindContext context) { throw null; }
        internal Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions Clone(System.Net.IPAddress address) { throw null; }
        internal override string GetDisplayName() { throw null; }
    }
    internal sealed partial class AnyIPListenOptions : Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions
    {
        internal AnyIPListenOptions(int port) : base (default(System.Net.IPEndPoint)) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        internal override System.Threading.Tasks.Task BindAsync(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.AddressBindContext context) { throw null; }
    }
    public partial class KestrelServerOptions
    {
        internal System.Security.Cryptography.X509Certificates.X509Certificate2 DefaultCertificate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal bool IsDevCertLoaded { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal bool Latin1RequestHeaders { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal System.Collections.Generic.List<Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions> ListenOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        internal void ApplyDefaultCert(Microsoft.AspNetCore.Server.Kestrel.Https.HttpsConnectionAdapterOptions httpsOptions) { }
        internal void ApplyEndpointDefaults(Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions listenOptions) { }
        internal void ApplyHttpsDefaults(Microsoft.AspNetCore.Server.Kestrel.Https.HttpsConnectionAdapterOptions httpsOptions) { }
    }
    internal static partial class CoreStrings
    {
        internal static string AddressBindingFailed { get { throw null; } }
        internal static string ArgumentOutOfRange { get { throw null; } }
        internal static string AuthenticationFailed { get { throw null; } }
        internal static string AuthenticationTimedOut { get { throw null; } }
        internal static string BadRequest { get { throw null; } }
        internal static string BadRequest_BadChunkSizeData { get { throw null; } }
        internal static string BadRequest_BadChunkSuffix { get { throw null; } }
        internal static string BadRequest_ChunkedRequestIncomplete { get { throw null; } }
        internal static string BadRequest_FinalTransferCodingNotChunked { get { throw null; } }
        internal static string BadRequest_HeadersExceedMaxTotalSize { get { throw null; } }
        internal static string BadRequest_InvalidCharactersInHeaderName { get { throw null; } }
        internal static string BadRequest_InvalidContentLength_Detail { get { throw null; } }
        internal static string BadRequest_InvalidHostHeader { get { throw null; } }
        internal static string BadRequest_InvalidHostHeader_Detail { get { throw null; } }
        internal static string BadRequest_InvalidRequestHeadersNoCRLF { get { throw null; } }
        internal static string BadRequest_InvalidRequestHeader_Detail { get { throw null; } }
        internal static string BadRequest_InvalidRequestLine { get { throw null; } }
        internal static string BadRequest_InvalidRequestLine_Detail { get { throw null; } }
        internal static string BadRequest_InvalidRequestTarget_Detail { get { throw null; } }
        internal static string BadRequest_LengthRequired { get { throw null; } }
        internal static string BadRequest_LengthRequiredHttp10 { get { throw null; } }
        internal static string BadRequest_MalformedRequestInvalidHeaders { get { throw null; } }
        internal static string BadRequest_MethodNotAllowed { get { throw null; } }
        internal static string BadRequest_MissingHostHeader { get { throw null; } }
        internal static string BadRequest_MultipleContentLengths { get { throw null; } }
        internal static string BadRequest_MultipleHostHeaders { get { throw null; } }
        internal static string BadRequest_RequestBodyTimeout { get { throw null; } }
        internal static string BadRequest_RequestBodyTooLarge { get { throw null; } }
        internal static string BadRequest_RequestHeadersTimeout { get { throw null; } }
        internal static string BadRequest_RequestLineTooLong { get { throw null; } }
        internal static string BadRequest_TooManyHeaders { get { throw null; } }
        internal static string BadRequest_UnexpectedEndOfRequestContent { get { throw null; } }
        internal static string BadRequest_UnrecognizedHTTPVersion { get { throw null; } }
        internal static string BadRequest_UpgradeRequestCannotHavePayload { get { throw null; } }
        internal static string BigEndianNotSupported { get { throw null; } }
        internal static string BindingToDefaultAddress { get { throw null; } }
        internal static string BindingToDefaultAddresses { get { throw null; } }
        internal static string CannotUpgradeNonUpgradableRequest { get { throw null; } }
        internal static string CertNotFoundInStore { get { throw null; } }
        internal static string ConcurrentTimeoutsNotSupported { get { throw null; } }
        internal static string ConfigureHttpsFromMethodCall { get { throw null; } }
        internal static string ConfigurePathBaseFromMethodCall { get { throw null; } }
        internal static string ConnectionAbortedByApplication { get { throw null; } }
        internal static string ConnectionAbortedByClient { get { throw null; } }
        internal static string ConnectionAbortedDuringServerShutdown { get { throw null; } }
        internal static string ConnectionOrStreamAbortedByCancellationToken { get { throw null; } }
        internal static string ConnectionShutdownError { get { throw null; } }
        internal static string ConnectionTimedBecauseResponseMininumDataRateNotSatisfied { get { throw null; } }
        internal static string ConnectionTimedOutByServer { get { throw null; } }
        internal static System.Globalization.CultureInfo Culture { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal static string DynamicPortOnLocalhostNotSupported { get { throw null; } }
        internal static string EndpointAlreadyInUse { get { throw null; } }
        internal static string EndPointHttp2NotNegotiated { get { throw null; } }
        internal static string EndpointMissingUrl { get { throw null; } }
        internal static string EndPointRequiresAtLeastOneProtocol { get { throw null; } }
        internal static string FallbackToIPv4Any { get { throw null; } }
        internal static string GreaterThanZeroRequired { get { throw null; } }
        internal static string HeaderNotAllowedOnResponse { get { throw null; } }
        internal static string HeadersAreReadOnly { get { throw null; } }
        internal static string HPackErrorDynamicTableSizeUpdateNotAtBeginningOfHeaderBlock { get { throw null; } }
        internal static string HPackErrorDynamicTableSizeUpdateTooLarge { get { throw null; } }
        internal static string HPackErrorIncompleteHeaderBlock { get { throw null; } }
        internal static string HPackErrorIndexOutOfRange { get { throw null; } }
        internal static string HPackErrorIntegerTooBig { get { throw null; } }
        internal static string HPackErrorNotEnoughBuffer { get { throw null; } }
        internal static string HPackHuffmanError { get { throw null; } }
        internal static string HPackHuffmanErrorDestinationTooSmall { get { throw null; } }
        internal static string HPackHuffmanErrorEOS { get { throw null; } }
        internal static string HPackHuffmanErrorIncomplete { get { throw null; } }
        internal static string HPackStringLengthTooLarge { get { throw null; } }
        internal static string Http2ConnectionFaulted { get { throw null; } }
        internal static string Http2ErrorConnectionSpecificHeaderField { get { throw null; } }
        internal static string Http2ErrorConnectMustNotSendSchemeOrPath { get { throw null; } }
        internal static string Http2ErrorContinuationWithNoHeaders { get { throw null; } }
        internal static string Http2ErrorDuplicatePseudoHeaderField { get { throw null; } }
        internal static string Http2ErrorFlowControlWindowExceeded { get { throw null; } }
        internal static string Http2ErrorFrameOverLimit { get { throw null; } }
        internal static string Http2ErrorHeaderNameUppercase { get { throw null; } }
        internal static string Http2ErrorHeadersInterleaved { get { throw null; } }
        internal static string Http2ErrorHeadersWithTrailersNoEndStream { get { throw null; } }
        internal static string Http2ErrorInitialWindowSizeInvalid { get { throw null; } }
        internal static string Http2ErrorInvalidPreface { get { throw null; } }
        internal static string Http2ErrorMaxStreams { get { throw null; } }
        internal static string Http2ErrorMethodInvalid { get { throw null; } }
        internal static string Http2ErrorMinTlsVersion { get { throw null; } }
        internal static string Http2ErrorMissingMandatoryPseudoHeaderFields { get { throw null; } }
        internal static string Http2ErrorPaddingTooLong { get { throw null; } }
        internal static string Http2ErrorPseudoHeaderFieldAfterRegularHeaders { get { throw null; } }
        internal static string Http2ErrorPushPromiseReceived { get { throw null; } }
        internal static string Http2ErrorResponsePseudoHeaderField { get { throw null; } }
        internal static string Http2ErrorSettingsAckLengthNotZero { get { throw null; } }
        internal static string Http2ErrorSettingsLengthNotMultipleOfSix { get { throw null; } }
        internal static string Http2ErrorSettingsParameterOutOfRange { get { throw null; } }
        internal static string Http2ErrorStreamAborted { get { throw null; } }
        internal static string Http2ErrorStreamClosed { get { throw null; } }
        internal static string Http2ErrorStreamHalfClosedRemote { get { throw null; } }
        internal static string Http2ErrorStreamIdEven { get { throw null; } }
        internal static string Http2ErrorStreamIdle { get { throw null; } }
        internal static string Http2ErrorStreamIdNotZero { get { throw null; } }
        internal static string Http2ErrorStreamIdZero { get { throw null; } }
        internal static string Http2ErrorStreamSelfDependency { get { throw null; } }
        internal static string Http2ErrorTrailerNameUppercase { get { throw null; } }
        internal static string Http2ErrorTrailersContainPseudoHeaderField { get { throw null; } }
        internal static string Http2ErrorUnexpectedFrameLength { get { throw null; } }
        internal static string Http2ErrorUnknownPseudoHeaderField { get { throw null; } }
        internal static string Http2ErrorWindowUpdateIncrementZero { get { throw null; } }
        internal static string Http2ErrorWindowUpdateSizeInvalid { get { throw null; } }
        internal static string Http2MinDataRateNotSupported { get { throw null; } }
        internal static string HTTP2NoTlsOsx { get { throw null; } }
        internal static string HTTP2NoTlsWin7 { get { throw null; } }
        internal static string Http2StreamAborted { get { throw null; } }
        internal static string Http2StreamErrorAfterHeaders { get { throw null; } }
        internal static string Http2StreamErrorLessDataThanLength { get { throw null; } }
        internal static string Http2StreamErrorMoreDataThanLength { get { throw null; } }
        internal static string Http2StreamErrorPathInvalid { get { throw null; } }
        internal static string Http2StreamErrorSchemeMismatch { get { throw null; } }
        internal static string Http2StreamResetByApplication { get { throw null; } }
        internal static string Http2StreamResetByClient { get { throw null; } }
        internal static string Http2TellClientToCalmDown { get { throw null; } }
        internal static string InvalidAsciiOrControlChar { get { throw null; } }
        internal static string InvalidContentLength_InvalidNumber { get { throw null; } }
        internal static string InvalidEmptyHeaderName { get { throw null; } }
        internal static string InvalidServerCertificateEku { get { throw null; } }
        internal static string InvalidUrl { get { throw null; } }
        internal static string KeyAlreadyExists { get { throw null; } }
        internal static string MaxRequestBodySizeCannotBeModifiedAfterRead { get { throw null; } }
        internal static string MaxRequestBodySizeCannotBeModifiedForUpgradedRequests { get { throw null; } }
        internal static string MaxRequestBufferSmallerThanRequestHeaderBuffer { get { throw null; } }
        internal static string MaxRequestBufferSmallerThanRequestLineBuffer { get { throw null; } }
        internal static string MinimumGracePeriodRequired { get { throw null; } }
        internal static string MultipleCertificateSources { get { throw null; } }
        internal static string NetworkInterfaceBindingFailed { get { throw null; } }
        internal static string NoCertSpecifiedNoDevelopmentCertificateFound { get { throw null; } }
        internal static string NonNegativeNumberOrNullRequired { get { throw null; } }
        internal static string NonNegativeNumberRequired { get { throw null; } }
        internal static string NonNegativeTimeSpanRequired { get { throw null; } }
        internal static string OverridingWithKestrelOptions { get { throw null; } }
        internal static string OverridingWithPreferHostingUrls { get { throw null; } }
        internal static string ParameterReadOnlyAfterResponseStarted { get { throw null; } }
        internal static string PositiveFiniteTimeSpanRequired { get { throw null; } }
        internal static string PositiveNumberOrNullMinDataRateRequired { get { throw null; } }
        internal static string PositiveNumberOrNullRequired { get { throw null; } }
        internal static string PositiveNumberRequired { get { throw null; } }
        internal static string PositiveTimeSpanRequired { get { throw null; } }
        internal static string PositiveTimeSpanRequired1 { get { throw null; } }
        internal static string ProtocolSelectionFailed { get { throw null; } }
        internal static string RequestProcessingAborted { get { throw null; } }
        internal static string RequestProcessingEndError { get { throw null; } }
        internal static string RequestTrailersNotAvailable { get { throw null; } }
        internal static System.Resources.ResourceManager ResourceManager { get { throw null; } }
        internal static string ResponseStreamWasUpgraded { get { throw null; } }
        internal static string ServerAlreadyStarted { get { throw null; } }
        internal static string ServerCertificateRequired { get { throw null; } }
        internal static string ServerShutdownDuringConnectionInitialization { get { throw null; } }
        internal static string StartAsyncBeforeGetMemory { get { throw null; } }
        internal static string SynchronousReadsDisallowed { get { throw null; } }
        internal static string SynchronousWritesDisallowed { get { throw null; } }
        internal static string TooFewBytesWritten { get { throw null; } }
        internal static string TooManyBytesWritten { get { throw null; } }
        internal static string UnableToConfigureHttpsBindings { get { throw null; } }
        internal static string UnhandledApplicationException { get { throw null; } }
        internal static string UnixSocketPathMustBeAbsolute { get { throw null; } }
        internal static string UnknownTransportMode { get { throw null; } }
        internal static string UnsupportedAddressScheme { get { throw null; } }
        internal static string UpgradeCannotBeCalledMultipleTimes { get { throw null; } }
        internal static string UpgradedConnectionLimitReached { get { throw null; } }
        internal static string WritingToResponseBodyAfterResponseCompleted { get { throw null; } }
        internal static string WritingToResponseBodyNotSupported { get { throw null; } }
        internal static string FormatAddressBindingFailed(object address) { throw null; }
        internal static string FormatArgumentOutOfRange(object min, object max) { throw null; }
        internal static string FormatBadRequest_FinalTransferCodingNotChunked(object detail) { throw null; }
        internal static string FormatBadRequest_InvalidContentLength_Detail(object detail) { throw null; }
        internal static string FormatBadRequest_InvalidHostHeader_Detail(object detail) { throw null; }
        internal static string FormatBadRequest_InvalidRequestHeader_Detail(object detail) { throw null; }
        internal static string FormatBadRequest_InvalidRequestLine_Detail(object detail) { throw null; }
        internal static string FormatBadRequest_InvalidRequestTarget_Detail(object detail) { throw null; }
        internal static string FormatBadRequest_LengthRequired(object detail) { throw null; }
        internal static string FormatBadRequest_LengthRequiredHttp10(object detail) { throw null; }
        internal static string FormatBadRequest_UnrecognizedHTTPVersion(object detail) { throw null; }
        internal static string FormatBindingToDefaultAddress(object address) { throw null; }
        internal static string FormatBindingToDefaultAddresses(object address0, object address1) { throw null; }
        internal static string FormatCertNotFoundInStore(object subject, object storeLocation, object storeName, object allowInvalid) { throw null; }
        internal static string FormatConfigureHttpsFromMethodCall(object methodName) { throw null; }
        internal static string FormatConfigurePathBaseFromMethodCall(object methodName) { throw null; }
        internal static string FormatEndpointAlreadyInUse(object endpoint) { throw null; }
        internal static string FormatEndpointMissingUrl(object endpointName) { throw null; }
        internal static string FormatFallbackToIPv4Any(object port) { throw null; }
        internal static string FormatHeaderNotAllowedOnResponse(object name, object statusCode) { throw null; }
        internal static string FormatHPackErrorDynamicTableSizeUpdateTooLarge(object size, object maxSize) { throw null; }
        internal static string FormatHPackErrorIndexOutOfRange(object index) { throw null; }
        internal static string FormatHPackStringLengthTooLarge(object length, object maxStringLength) { throw null; }
        internal static string FormatHttp2ErrorFrameOverLimit(object size, object limit) { throw null; }
        internal static string FormatHttp2ErrorHeadersInterleaved(object frameType, object streamId, object headersStreamId) { throw null; }
        internal static string FormatHttp2ErrorMethodInvalid(object method) { throw null; }
        internal static string FormatHttp2ErrorMinTlsVersion(object protocol) { throw null; }
        internal static string FormatHttp2ErrorPaddingTooLong(object frameType) { throw null; }
        internal static string FormatHttp2ErrorSettingsParameterOutOfRange(object parameter) { throw null; }
        internal static string FormatHttp2ErrorStreamAborted(object frameType, object streamId) { throw null; }
        internal static string FormatHttp2ErrorStreamClosed(object frameType, object streamId) { throw null; }
        internal static string FormatHttp2ErrorStreamHalfClosedRemote(object frameType, object streamId) { throw null; }
        internal static string FormatHttp2ErrorStreamIdEven(object frameType, object streamId) { throw null; }
        internal static string FormatHttp2ErrorStreamIdle(object frameType, object streamId) { throw null; }
        internal static string FormatHttp2ErrorStreamIdNotZero(object frameType) { throw null; }
        internal static string FormatHttp2ErrorStreamIdZero(object frameType) { throw null; }
        internal static string FormatHttp2ErrorStreamSelfDependency(object frameType, object streamId) { throw null; }
        internal static string FormatHttp2ErrorUnexpectedFrameLength(object frameType, object expectedLength) { throw null; }
        internal static string FormatHttp2StreamErrorPathInvalid(object path) { throw null; }
        internal static string FormatHttp2StreamErrorSchemeMismatch(object requestScheme, object transportScheme) { throw null; }
        internal static string FormatHttp2StreamResetByApplication(object errorCode) { throw null; }
        internal static string FormatInvalidAsciiOrControlChar(object character) { throw null; }
        internal static string FormatInvalidContentLength_InvalidNumber(object value) { throw null; }
        internal static string FormatInvalidServerCertificateEku(object thumbprint) { throw null; }
        internal static string FormatInvalidUrl(object url) { throw null; }
        internal static string FormatMaxRequestBufferSmallerThanRequestHeaderBuffer(object requestBufferSize, object requestHeaderSize) { throw null; }
        internal static string FormatMaxRequestBufferSmallerThanRequestLineBuffer(object requestBufferSize, object requestLineSize) { throw null; }
        internal static string FormatMinimumGracePeriodRequired(object heartbeatInterval) { throw null; }
        internal static string FormatMultipleCertificateSources(object endpointName) { throw null; }
        internal static string FormatNetworkInterfaceBindingFailed(object address, object interfaceName, object error) { throw null; }
        internal static string FormatOverridingWithKestrelOptions(object addresses, object methodName) { throw null; }
        internal static string FormatOverridingWithPreferHostingUrls(object settingName, object addresses) { throw null; }
        internal static string FormatParameterReadOnlyAfterResponseStarted(object name) { throw null; }
        internal static string FormatTooFewBytesWritten(object written, object expected) { throw null; }
        internal static string FormatTooManyBytesWritten(object written, object expected) { throw null; }
        internal static string FormatUnknownTransportMode(object mode) { throw null; }
        internal static string FormatUnsupportedAddressScheme(object address) { throw null; }
        internal static string FormatWritingToResponseBodyNotSupported(object statusCode) { throw null; }
    }

    public partial class ListenOptions : Microsoft.AspNetCore.Connections.IConnectionBuilder
    {
        internal readonly System.Collections.Generic.List<System.Func<Microsoft.AspNetCore.Connections.ConnectionDelegate, Microsoft.AspNetCore.Connections.ConnectionDelegate>> _middleware;
        internal ListenOptions(System.Net.IPEndPoint endPoint) { }
        internal ListenOptions(string socketPath) { }
        internal ListenOptions(ulong fileHandle) { }
        internal ListenOptions(ulong fileHandle, Microsoft.AspNetCore.Connections.FileHandleType handleType) { }
        public Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions KestrelServerOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]internal set { } }
        internal System.Net.EndPoint EndPoint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal bool IsHttp { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal bool IsTls { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal string Scheme { get { throw null; } }
        internal virtual string GetDisplayName() { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        internal virtual System.Threading.Tasks.Task BindAsync(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.AddressBindContext context) { throw null; }
    }
}

namespace Microsoft.AspNetCore.Server.Kestrel.Https.Internal
{
    internal partial class HttpsConnectionMiddleware
    {
        public HttpsConnectionMiddleware(Microsoft.AspNetCore.Connections.ConnectionDelegate next, Microsoft.AspNetCore.Server.Kestrel.Https.HttpsConnectionAdapterOptions options) { }
        public HttpsConnectionMiddleware(Microsoft.AspNetCore.Connections.ConnectionDelegate next, Microsoft.AspNetCore.Server.Kestrel.Https.HttpsConnectionAdapterOptions options, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public System.Threading.Tasks.Task OnConnectionAsync(Microsoft.AspNetCore.Connections.ConnectionContext context) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Https
{
    public static partial class CertificateLoader
    {
        internal static bool DoesCertificateHaveAnAccessiblePrivateKey(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw null; }
        internal static bool IsCertificateAllowedForServerAuth(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { throw null; }
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal
{
    internal static partial class MemoryPoolExtensions
    {
        public static int GetMinimumAllocSize(this System.Buffers.MemoryPool<byte> pool) { throw null; }
        public static int GetMinimumSegmentSize(this System.Buffers.MemoryPool<byte> pool) { throw null; }
    }
    internal partial class DuplexPipeStreamAdapter<TStream> : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.DuplexPipeStream, System.IO.Pipelines.IDuplexPipe where TStream : System.IO.Stream
    {
        public DuplexPipeStreamAdapter(System.IO.Pipelines.IDuplexPipe duplexPipe, System.Func<System.IO.Stream, TStream> createStream) : base (default(System.IO.Pipelines.PipeReader), default(System.IO.Pipelines.PipeWriter), default(bool)) { }
        public DuplexPipeStreamAdapter(System.IO.Pipelines.IDuplexPipe duplexPipe, System.IO.Pipelines.StreamPipeReaderOptions readerOptions, System.IO.Pipelines.StreamPipeWriterOptions writerOptions, System.Func<System.IO.Stream, TStream> createStream) : base (default(System.IO.Pipelines.PipeReader), default(System.IO.Pipelines.PipeWriter), default(bool)) { }
        public System.IO.Pipelines.PipeReader Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.IO.Pipelines.PipeWriter Output { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public TStream Stream { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        protected override void Dispose(bool disposing) { }
        public override System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
    }
    internal partial class DuplexPipeStream : System.IO.Stream
    {
        public DuplexPipeStream(System.IO.Pipelines.PipeReader input, System.IO.Pipelines.PipeWriter output, bool throwOnCancelled = false) { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public override System.IAsyncResult BeginRead(byte[] buffer, int offset, int count, System.AsyncCallback callback, object state) { throw null; }
        public override System.IAsyncResult BeginWrite(byte[] buffer, int offset, int count, System.AsyncCallback callback, object state) { throw null; }
        public void CancelPendingRead() { }
        public override int EndRead(System.IAsyncResult asyncResult) { throw null; }
        public override void EndWrite(System.IAsyncResult asyncResult) { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Threading.Tasks.ValueTask<int> ReadAsync(System.Memory<byte> destination, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override System.Threading.Tasks.ValueTask WriteAsync(System.ReadOnlyMemory<byte> source, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    internal partial class ConnectionLimitMiddleware
    {
        internal ConnectionLimitMiddleware(Microsoft.AspNetCore.Connections.ConnectionDelegate next, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ResourceCounter concurrentConnectionCounter, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace trace) { }
        public ConnectionLimitMiddleware(Microsoft.AspNetCore.Connections.ConnectionDelegate next, long connectionLimit, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace trace) { }
        public System.Threading.Tasks.Task OnConnectionAsync(Microsoft.AspNetCore.Connections.ConnectionContext connection) { throw null; }
    }
    internal static partial class HttpConnectionBuilderExtensions
    {
        public static Microsoft.AspNetCore.Connections.IConnectionBuilder UseHttpServer<TContext>(this Microsoft.AspNetCore.Connections.IConnectionBuilder builder, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.ServiceContext serviceContext, Microsoft.AspNetCore.Hosting.Server.IHttpApplication<TContext> application, Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols protocols) { throw null; }
    }
    internal partial class HttpConnection : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ITimeoutHandler
    {
        public HttpConnection(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.HttpConnectionContext context) { }
        internal void Initialize(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.IRequestProcessor requestProcessor) { }
        public void OnTimeout(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.TimeoutReason reason) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task ProcessRequestsAsync<TContext>(Microsoft.AspNetCore.Hosting.Server.IHttpApplication<TContext> httpApplication) { throw null; }
    }
    internal partial class ConnectionDispatcher
    {
        public ConnectionDispatcher(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.ServiceContext serviceContext, Microsoft.AspNetCore.Connections.ConnectionDelegate connectionDelegate) { }
        public System.Threading.Tasks.Task StartAcceptingConnections(Microsoft.AspNetCore.Connections.IConnectionListener listener) { throw null; }
    }
    internal partial class ServerAddressesFeature : Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature
    {
        public ServerAddressesFeature() { }
        public System.Collections.Generic.ICollection<string> Addresses { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool PreferHostingUrls { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    internal partial class AddressBindContext
    {
        public AddressBindContext() { }
        public System.Collections.Generic.ICollection<string> Addresses { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Func<Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions, System.Threading.Tasks.Task> CreateBinding { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.List<Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions> ListenOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.Extensions.Logging.ILogger Logger { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions ServerOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    internal partial class AddressBinder
    {
        public AddressBinder() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public static System.Threading.Tasks.Task BindAsync(Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature addresses, Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions serverOptions, Microsoft.Extensions.Logging.ILogger logger, System.Func<Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions, System.Threading.Tasks.Task> createBinding) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        internal static System.Threading.Tasks.Task BindEndpointAsync(Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions endpoint, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.AddressBindContext context) { throw null; }
        internal static Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions ParseAddress(string address, out bool https) { throw null; }
        protected internal static bool TryCreateIPEndPoint(Microsoft.AspNetCore.Http.BindingAddress address, out System.Net.IPEndPoint endpoint) { throw null; }
    }
    internal partial class EndpointConfig
    {
        public EndpointConfig() { }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.CertificateConfig Certificate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.Extensions.Configuration.IConfigurationSection ConfigSection { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols? Protocols { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Url { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    internal partial class EndpointDefaults
    {
        public EndpointDefaults() { }
        public Microsoft.Extensions.Configuration.IConfigurationSection ConfigSection { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols? Protocols { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    internal partial class CertificateConfig
    {
        public CertificateConfig(Microsoft.Extensions.Configuration.IConfigurationSection configSection) { }
        public bool? AllowInvalid { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.Extensions.Configuration.IConfigurationSection ConfigSection { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool IsFileCert { get { throw null; } }
        public bool IsStoreCert { get { throw null; } }
        public string Location { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Password { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Path { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Store { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Subject { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    internal partial class ConfigurationReader
    {
        public ConfigurationReader(Microsoft.Extensions.Configuration.IConfiguration configuration) { }
        public System.Collections.Generic.IDictionary<string, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.CertificateConfig> Certificates { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.EndpointDefaults EndpointDefaults { get { throw null; } }
        public System.Collections.Generic.IEnumerable<Microsoft.AspNetCore.Server.Kestrel.Core.Internal.EndpointConfig> Endpoints { get { throw null; } }
        public bool Latin1RequestHeaders { get { throw null; } }
    }
    internal partial class HttpConnectionContext
    {
        public HttpConnectionContext() { }
        public Microsoft.AspNetCore.Connections.ConnectionContext ConnectionContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.Features.IFeatureCollection ConnectionFeatures { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ConnectionId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Net.IPEndPoint LocalEndPoint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Buffers.MemoryPool<byte> MemoryPool { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols Protocols { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Net.IPEndPoint RemoteEndPoint { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.ServiceContext ServiceContext { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ITimeoutControl TimeoutControl { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.IO.Pipelines.IDuplexPipe Transport { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    internal partial interface IRequestProcessor
    {
        void Abort(Microsoft.AspNetCore.Connections.ConnectionAbortedException ex);
        void HandleReadDataRateTimeout();
        void HandleRequestHeadersTimeout();
        void OnInputOrOutputCompleted();
        System.Threading.Tasks.Task ProcessRequestsAsync<TContext>(Microsoft.AspNetCore.Hosting.Server.IHttpApplication<TContext> application);
        void StopProcessingNextRequest();
        void Tick(System.DateTimeOffset now);
    }
    internal partial class KestrelServerOptionsSetup : Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>
    {
        public KestrelServerOptionsSetup(System.IServiceProvider services) { }
        public void Configure(Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions options) { }
    }
    internal partial class ServiceContext
    {
        public ServiceContext() { }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ConnectionManager ConnectionManager { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.DateHeaderValueManager DateHeaderValueManager { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.Heartbeat Heartbeat { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpParser<Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1ParsingHandler> HttpParser { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace Log { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.IO.Pipelines.PipeScheduler Scheduler { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions ServerOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ISystemClock SystemClock { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
}

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal sealed partial class Http1ContentLengthMessageBody : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1MessageBody
    {
        public Http1ContentLengthMessageBody(bool keepAlive, long contentLength, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1Connection context) : base (default(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1Connection)) { }
        public override void AdvanceTo(System.SequencePosition consumed) { }
        public override void AdvanceTo(System.SequencePosition consumed, System.SequencePosition examined) { }
        public override void CancelPendingRead() { }
        public override void Complete(System.Exception exception) { }
        public override System.Threading.Tasks.Task ConsumeAsync() { throw null; }
        protected override void OnReadStarting() { }
        protected override System.Threading.Tasks.Task OnStopAsync() { throw null; }
        public override System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> ReadAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public override System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> ReadAsyncInternal(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override bool TryRead(out System.IO.Pipelines.ReadResult readResult) { throw null; }
        public override bool TryReadInternal(out System.IO.Pipelines.ReadResult readResult) { throw null; }
    }
    internal static partial class ReasonPhrases
    {
        public static byte[] ToStatusBytes(int statusCode, string reasonPhrase = null) { throw null; }
    }
    internal static partial class PathNormalizer
    {
        public unsafe static bool ContainsDotSegments(byte* start, byte* end) { throw null; }
        public static string DecodePath(System.Span<byte> path, bool pathEncoded, string rawTarget, int queryLength) { throw null; }
        public unsafe static int RemoveDotSegments(byte* start, byte* end) { throw null; }
        public static int RemoveDotSegments(System.Span<byte> input) { throw null; }
    }
    internal enum RequestRejectionReason
    {
        UnrecognizedHTTPVersion = 0,
        InvalidRequestLine = 1,
        InvalidRequestHeader = 2,
        InvalidRequestHeadersNoCRLF = 3,
        MalformedRequestInvalidHeaders = 4,
        InvalidContentLength = 5,
        MultipleContentLengths = 6,
        UnexpectedEndOfRequestContent = 7,
        BadChunkSuffix = 8,
        BadChunkSizeData = 9,
        ChunkedRequestIncomplete = 10,
        InvalidRequestTarget = 11,
        InvalidCharactersInHeaderName = 12,
        RequestLineTooLong = 13,
        HeadersExceedMaxTotalSize = 14,
        TooManyHeaders = 15,
        RequestBodyTooLarge = 16,
        RequestHeadersTimeout = 17,
        RequestBodyTimeout = 18,
        FinalTransferCodingNotChunked = 19,
        LengthRequired = 20,
        LengthRequiredHttp10 = 21,
        OptionsMethodRequired = 22,
        ConnectMethodRequired = 23,
        MissingHostHeader = 24,
        MultipleHostHeaders = 25,
        InvalidHostHeader = 26,
        UpgradeRequestCannotHavePayload = 27,
        RequestBodyExceedsContentLength = 28,
    }
    internal static partial class ChunkWriter
    {
        public static int BeginChunkBytes(int dataCount, System.Span<byte> span) { throw null; }
        internal static int GetPrefixBytesForChunk(int length, out bool sliceOneByte) { throw null; }
        internal static int WriteBeginChunkBytes(this ref System.Buffers.BufferWriter<System.IO.Pipelines.PipeWriter> start, int dataCount) { throw null; }
        internal static void WriteEndChunkBytes(this ref System.Buffers.BufferWriter<System.IO.Pipelines.PipeWriter> start) { }
    }
    internal sealed partial class HttpRequestPipeReader : System.IO.Pipelines.PipeReader
    {
        public HttpRequestPipeReader() { }
        public void Abort(System.Exception error = null) { }
        public override void AdvanceTo(System.SequencePosition consumed) { }
        public override void AdvanceTo(System.SequencePosition consumed, System.SequencePosition examined) { }
        public override void CancelPendingRead() { }
        public override void Complete(System.Exception exception = null) { }
        public override System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> ReadAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public void StartAcceptingReads(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.MessageBody body) { }
        public void StopAcceptingReads() { }
        public override bool TryRead(out System.IO.Pipelines.ReadResult result) { throw null; }
    }
    internal sealed partial class HttpRequestStream : System.IO.Stream
    {
        public HttpRequestStream(Microsoft.AspNetCore.Http.Features.IHttpBodyControlFeature bodyControl, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpRequestPipeReader pipeReader) { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
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
    internal abstract partial class Http1MessageBody : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.MessageBody
    {
        protected bool _completed;
        protected readonly Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1Connection _context;
        protected Http1MessageBody(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1Connection context) : base (default(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol)) { }
        protected void CheckCompletedReadResult(System.IO.Pipelines.ReadResult result) { }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.MessageBody For(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpVersion httpVersion, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpRequestHeaders headers, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1Connection context) { throw null; }
        protected override System.Threading.Tasks.Task OnConsumeAsync() { throw null; }
        public abstract System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> ReadAsyncInternal(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        protected void ThrowIfCompleted() { }
        public abstract bool TryReadInternal(out System.IO.Pipelines.ReadResult readResult);
    }
    internal partial interface IHttpOutputAborter
    {
        void Abort(Microsoft.AspNetCore.Connections.ConnectionAbortedException abortReason);
    }
    internal partial class Http1OutputProducer : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpOutputAborter, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpOutputProducer, System.IDisposable
    {
        public Http1OutputProducer(System.IO.Pipelines.PipeWriter pipeWriter, string connectionId, Microsoft.AspNetCore.Connections.ConnectionContext connectionContext, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace log, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ITimeoutControl timeoutControl, Microsoft.AspNetCore.Server.Kestrel.Core.Features.IHttpMinResponseDataRateFeature minResponseDataRateFeature, System.Buffers.MemoryPool<byte> memoryPool) { }
        public void Abort(Microsoft.AspNetCore.Connections.ConnectionAbortedException error) { }
        public void Advance(int bytes) { }
        public void CancelPendingFlush() { }
        public void Dispose() { }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FirstWriteAsync(int statusCode, string reasonPhrase, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseHeaders responseHeaders, bool autoChunk, System.ReadOnlySpan<byte> buffer, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FirstWriteChunkedAsync(int statusCode, string reasonPhrase, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseHeaders responseHeaders, bool autoChunk, System.ReadOnlySpan<byte> buffer, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FlushAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Memory<byte> GetMemory(int sizeHint = 0) { throw null; }
        public System.Span<byte> GetSpan(int sizeHint = 0) { throw null; }
        public void Reset() { }
        public void Stop() { }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> Write100ContinueAsync() { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteChunkAsync(System.ReadOnlySpan<byte> buffer, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task WriteDataAsync(System.ReadOnlySpan<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteDataToPipeAsync(System.ReadOnlySpan<byte> buffer, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public void WriteResponseHeaders(int statusCode, string reasonPhrase, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseHeaders responseHeaders, bool autoChunk, bool appComplete) { }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteStreamSuffixAsync() { throw null; }
    }
    internal sealed partial class HttpResponseStream : System.IO.Stream
    {
        public HttpResponseStream(Microsoft.AspNetCore.Http.Features.IHttpBodyControlFeature bodyControl, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponsePipeWriter pipeWriter) { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
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
    internal sealed partial class HttpResponsePipeWriter : System.IO.Pipelines.PipeWriter
    {
        public HttpResponsePipeWriter(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpResponseControl pipeControl) { }
        public void Abort() { }
        public override void Advance(int bytes) { }
        public override void CancelPendingFlush() { }
        public override void Complete(System.Exception exception = null) { }
        public override System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FlushAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Memory<byte> GetMemory(int sizeHint = 0) { throw null; }
        public override System.Span<byte> GetSpan(int sizeHint = 0) { throw null; }
        public void StartAcceptingWrites() { }
        public System.Threading.Tasks.Task StopAcceptingWritesAsync() { throw null; }
        public override System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteAsync(System.ReadOnlyMemory<byte> source, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    internal partial class DateHeaderValueManager : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IHeartbeatHandler
    {
        public DateHeaderValueManager() { }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.DateHeaderValueManager.DateHeaderValues GetDateHeaderValues() { throw null; }
        public void OnHeartbeat(System.DateTimeOffset now) { }
        public partial class DateHeaderValues
        {
            public byte[] Bytes;
            public string String;
            public DateHeaderValues() { }
        }
    }
    [System.FlagsAttribute]
    internal enum ConnectionOptions
    {
        None = 0,
        Close = 1,
        KeepAlive = 2,
        Upgrade = 4,
    }
    internal abstract partial class HttpHeaders : Microsoft.AspNetCore.Http.IHeaderDictionary, System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.Generic.IDictionary<string, Microsoft.Extensions.Primitives.StringValues>, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.IEnumerable
    {
        protected System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues> MaybeUnknown;
        protected long _bits;
        protected long? _contentLength;
        protected bool _isReadOnly;
        protected HttpHeaders() { }
        public long? ContentLength { get { throw null; } set { } }
        public int Count { get { throw null; } }
        Microsoft.Extensions.Primitives.StringValues Microsoft.AspNetCore.Http.IHeaderDictionary.this[string key] { get { throw null; } set { } }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.IsReadOnly { get { throw null; } }
        Microsoft.Extensions.Primitives.StringValues System.Collections.Generic.IDictionary<System.String,Microsoft.Extensions.Primitives.StringValues>.this[string key] { get { throw null; } set { } }
        System.Collections.Generic.ICollection<string> System.Collections.Generic.IDictionary<System.String,Microsoft.Extensions.Primitives.StringValues>.Keys { get { throw null; } }
        System.Collections.Generic.ICollection<Microsoft.Extensions.Primitives.StringValues> System.Collections.Generic.IDictionary<System.String,Microsoft.Extensions.Primitives.StringValues>.Values { get { throw null; } }
        protected System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues> Unknown { get { throw null; } }
        protected virtual bool AddValueFast(string key, Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]protected static Microsoft.Extensions.Primitives.StringValues AppendValue(Microsoft.Extensions.Primitives.StringValues existing, string append) { throw null; }
        protected virtual void ClearFast() { }
        protected virtual bool CopyToFast(System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>[] array, int arrayIndex) { throw null; }
        protected virtual int GetCountFast() { throw null; }
        protected virtual System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> GetEnumeratorFast() { throw null; }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.TransferCoding GetFinalTransferCoding(Microsoft.Extensions.Primitives.StringValues transferEncoding) { throw null; }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.ConnectionOptions ParseConnection(Microsoft.Extensions.Primitives.StringValues connection) { throw null; }
        protected virtual bool RemoveFast(string key) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]protected bool RemoveUnknown(string key) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public void Reset() { }
        public void SetReadOnly() { }
        protected virtual void SetValueFast(string key, Microsoft.Extensions.Primitives.StringValues value) { }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.Add(System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> item) { }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.Clear() { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.Contains(System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> item) { throw null; }
        void System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.CopyTo(System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>[] array, int arrayIndex) { }
        bool System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.Remove(System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> item) { throw null; }
        void System.Collections.Generic.IDictionary<System.String,Microsoft.Extensions.Primitives.StringValues>.Add(string key, Microsoft.Extensions.Primitives.StringValues value) { }
        bool System.Collections.Generic.IDictionary<System.String,Microsoft.Extensions.Primitives.StringValues>.ContainsKey(string key) { throw null; }
        bool System.Collections.Generic.IDictionary<System.String,Microsoft.Extensions.Primitives.StringValues>.Remove(string key) { throw null; }
        bool System.Collections.Generic.IDictionary<System.String,Microsoft.Extensions.Primitives.StringValues>.TryGetValue(string key, out Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.String,Microsoft.Extensions.Primitives.StringValues>>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        protected static void ThrowArgumentException() { }
        protected static void ThrowDuplicateKeyException() { }
        protected static void ThrowHeadersReadOnlyException() { }
        protected static void ThrowKeyNotFoundException() { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]protected bool TryGetUnknown(string key, ref Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        protected virtual bool TryGetValueFast(string key, out Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        public static void ValidateHeaderNameCharacters(string headerCharacters) { }
        public static void ValidateHeaderValueCharacters(Microsoft.Extensions.Primitives.StringValues headerValues) { }
        public static void ValidateHeaderValueCharacters(string headerCharacters) { }
    }
    internal abstract partial class HttpProtocol : Microsoft.AspNetCore.Http.Features.IEndpointFeature, Microsoft.AspNetCore.Http.Features.IFeatureCollection, Microsoft.AspNetCore.Http.Features.IHttpBodyControlFeature, Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature, Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature, Microsoft.AspNetCore.Http.Features.IHttpRequestFeature, Microsoft.AspNetCore.Http.Features.IHttpRequestIdentifierFeature, Microsoft.AspNetCore.Http.Features.IHttpRequestLifetimeFeature, Microsoft.AspNetCore.Http.Features.IHttpRequestTrailersFeature, Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature, Microsoft.AspNetCore.Http.Features.IHttpResponseFeature, Microsoft.AspNetCore.Http.Features.IHttpUpgradeFeature, Microsoft.AspNetCore.Http.Features.IRequestBodyPipeFeature, Microsoft.AspNetCore.Http.Features.IRouteValuesFeature, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpResponseControl, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.Type, object>>, System.Collections.IEnumerable
    {
        protected System.Exception _applicationException;
        protected Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.BodyControl _bodyControl;
        protected Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpVersion _httpVersion;
        protected volatile bool _keepAlive;
        protected string _methodText;
        protected string _parsedPath;
        protected string _parsedQueryString;
        protected string _parsedRawTarget;
        protected Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.RequestProcessingStatus _requestProcessingStatus;
        public HttpProtocol(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.HttpConnectionContext context) { }
        public bool AllowSynchronousIO { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.Features.IFeatureCollection ConnectionFeatures { get { throw null; } }
        protected string ConnectionId { get { throw null; } }
        public string ConnectionIdFeature { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool HasFlushedHeaders { get { throw null; } }
        public bool HasResponseCompleted { get { throw null; } }
        public bool HasResponseStarted { get { throw null; } }
        public bool HasStartedConsumingRequestBody { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpRequestHeaders HttpRequestHeaders { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpResponseControl HttpResponseControl { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseHeaders HttpResponseHeaders { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public string HttpVersion { get { throw null; } [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]set { } }
        public bool IsUpgradableRequest { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool IsUpgraded { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Net.IPAddress LocalIpAddress { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int LocalPort { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace Log { get { throw null; } }
        public long? MaxRequestBodySize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod Method { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        Microsoft.AspNetCore.Http.Endpoint Microsoft.AspNetCore.Http.Features.IEndpointFeature.Endpoint { get { throw null; } set { } }
        bool Microsoft.AspNetCore.Http.Features.IFeatureCollection.IsReadOnly { get { throw null; } }
        object Microsoft.AspNetCore.Http.Features.IFeatureCollection.this[System.Type key] { get { throw null; } set { } }
        int Microsoft.AspNetCore.Http.Features.IFeatureCollection.Revision { get { throw null; } }
        bool Microsoft.AspNetCore.Http.Features.IHttpBodyControlFeature.AllowSynchronousIO { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.ConnectionId { get { throw null; } set { } }
        System.Net.IPAddress Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.LocalIpAddress { get { throw null; } set { } }
        int Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.LocalPort { get { throw null; } set { } }
        System.Net.IPAddress Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.RemoteIpAddress { get { throw null; } set { } }
        int Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature.RemotePort { get { throw null; } set { } }
        bool Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature.IsReadOnly { get { throw null; } }
        long? Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature.MaxRequestBodySize { get { throw null; } set { } }
        System.IO.Stream Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Body { get { throw null; } set { } }
        Microsoft.AspNetCore.Http.IHeaderDictionary Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Headers { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Method { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Path { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.PathBase { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Protocol { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.QueryString { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.RawTarget { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpRequestFeature.Scheme { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpRequestIdentifierFeature.TraceIdentifier { get { throw null; } set { } }
        System.Threading.CancellationToken Microsoft.AspNetCore.Http.Features.IHttpRequestLifetimeFeature.RequestAborted { get { throw null; } set { } }
        bool Microsoft.AspNetCore.Http.Features.IHttpRequestTrailersFeature.Available { get { throw null; } }
        Microsoft.AspNetCore.Http.IHeaderDictionary Microsoft.AspNetCore.Http.Features.IHttpRequestTrailersFeature.Trailers { get { throw null; } }
        System.IO.Stream Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature.Stream { get { throw null; } }
        System.IO.Pipelines.PipeWriter Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature.Writer { get { throw null; } }
        System.IO.Stream Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.Body { get { throw null; } set { } }
        bool Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.HasStarted { get { throw null; } }
        Microsoft.AspNetCore.Http.IHeaderDictionary Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.Headers { get { throw null; } set { } }
        string Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.ReasonPhrase { get { throw null; } set { } }
        int Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.StatusCode { get { throw null; } set { } }
        bool Microsoft.AspNetCore.Http.Features.IHttpUpgradeFeature.IsUpgradableRequest { get { throw null; } }
        System.IO.Pipelines.PipeReader Microsoft.AspNetCore.Http.Features.IRequestBodyPipeFeature.Reader { get { throw null; } }
        Microsoft.AspNetCore.Routing.RouteValueDictionary Microsoft.AspNetCore.Http.Features.IRouteValuesFeature.RouteValues { get { throw null; } set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate MinRequestBodyDataRate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpOutputProducer Output { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]protected set { } }
        public string Path { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string PathBase { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string QueryString { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string RawTarget { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string ReasonPhrase { get { throw null; } set { } }
        public System.Net.IPAddress RemoteIpAddress { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int RemotePort { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Threading.CancellationToken RequestAborted { get { throw null; } set { } }
        public System.IO.Stream RequestBody { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.IO.Pipelines.PipeReader RequestBodyPipeReader { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.IHeaderDictionary RequestHeaders { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.IHeaderDictionary RequestTrailers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool RequestTrailersAvailable { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.IO.Stream ResponseBody { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.IO.Pipelines.PipeWriter ResponseBodyPipeWriter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Http.IHeaderDictionary ResponseHeaders { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseTrailers ResponseTrailers { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Scheme { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        protected Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions ServerOptions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.ServiceContext ServiceContext { get { throw null; } }
        public int StatusCode { get { throw null; } set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ITimeoutControl TimeoutControl { get { throw null; } }
        public string TraceIdentifier { get { throw null; } set { } }
        protected void AbortRequest() { }
        public void Advance(int bytes) { }
        protected abstract void ApplicationAbort();
        protected virtual bool BeginRead(out System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> awaitable) { throw null; }
        protected virtual void BeginRequestProcessing() { }
        public void CancelPendingFlush() { }
        public System.Threading.Tasks.Task CompleteAsync(System.Exception exception = null) { throw null; }
        protected abstract Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.MessageBody CreateMessageBody();
        protected abstract string CreateRequestId();
        protected System.Threading.Tasks.Task FireOnCompleted() { throw null; }
        protected System.Threading.Tasks.Task FireOnStarting() { throw null; }
        public System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FlushPipeAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Memory<byte> GetMemory(int sizeHint = 0) { throw null; }
        public System.Span<byte> GetSpan(int sizeHint = 0) { throw null; }
        public void HandleNonBodyResponseWrite() { }
        public void InitializeBodyControl(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.MessageBody messageBody) { }
        public System.Threading.Tasks.Task InitializeResponseAsync(int firstWriteByteCount) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task InitializeResponseAwaited(System.Threading.Tasks.Task startingTask, int firstWriteByteCount) { throw null; }
        TFeature Microsoft.AspNetCore.Http.Features.IFeatureCollection.Get<TFeature>() { throw null; }
        void Microsoft.AspNetCore.Http.Features.IFeatureCollection.Set<TFeature>(TFeature feature) { }
        void Microsoft.AspNetCore.Http.Features.IHttpRequestLifetimeFeature.Abort() { }
        System.Threading.Tasks.Task Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature.CompleteAsync() { throw null; }
        void Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature.DisableBuffering() { }
        System.Threading.Tasks.Task Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature.SendFileAsync(string path, long offset, long? count, System.Threading.CancellationToken cancellation) { throw null; }
        System.Threading.Tasks.Task Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature.StartAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        void Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.OnCompleted(System.Func<object, System.Threading.Tasks.Task> callback, object state) { }
        void Microsoft.AspNetCore.Http.Features.IHttpResponseFeature.OnStarting(System.Func<object, System.Threading.Tasks.Task> callback, object state) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        System.Threading.Tasks.Task<System.IO.Stream> Microsoft.AspNetCore.Http.Features.IHttpUpgradeFeature.UpgradeAsync() { throw null; }
        public void OnCompleted(System.Func<object, System.Threading.Tasks.Task> callback, object state) { }
        protected virtual void OnErrorAfterResponseStarted() { }
        public void OnHeader(System.Span<byte> name, System.Span<byte> value) { }
        public void OnHeadersComplete() { }
        protected virtual void OnRequestProcessingEnded() { }
        protected virtual void OnRequestProcessingEnding() { }
        protected abstract void OnReset();
        public void OnStarting(System.Func<object, System.Threading.Tasks.Task> callback, object state) { }
        public void OnTrailer(System.Span<byte> name, System.Span<byte> value) { }
        public void OnTrailersComplete() { }
        protected void PoisonRequestBodyStream(System.Exception abortReason) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task ProcessRequestsAsync<TContext>(Microsoft.AspNetCore.Hosting.Server.IHttpApplication<TContext> application) { throw null; }
        public void ProduceContinue() { }
        protected System.Threading.Tasks.Task ProduceEnd() { throw null; }
        public void ReportApplicationError(System.Exception ex) { }
        public void Reset() { }
        internal void ResetFeatureCollection() { }
        protected void ResetHttp1Features() { }
        protected void ResetHttp2Features() { }
        internal void ResetState() { }
        public void SetBadRequestState(Microsoft.AspNetCore.Server.Kestrel.Core.BadHttpRequestException ex) { }
        System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<System.Type, object>> System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<System.Type,System.Object>>.GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        [System.Diagnostics.StackTraceHiddenAttribute]
        public void ThrowRequestTargetRejected(System.Span<byte> target) { }
        protected abstract bool TryParseRequest(System.IO.Pipelines.ReadResult result, out bool endConnection);
        protected System.Threading.Tasks.Task TryProduceInvalidRequestResponse() { throw null; }
        protected bool VerifyResponseContentLength(out System.Exception ex) { throw null; }
        public System.Threading.Tasks.Task WriteAsync(System.ReadOnlyMemory<byte> data, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteAsyncAwaited(System.Threading.Tasks.Task initializeTask, System.ReadOnlyMemory<byte> data, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WritePipeAsync(System.ReadOnlyMemory<byte> data, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    internal sealed partial class HttpRequestHeaders : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpHeaders
    {
        public HttpRequestHeaders(bool reuseHeaderValues = true, bool useLatin1 = false) { }
        public bool HasConnection { get { throw null; } }
        public bool HasTransferEncoding { get { throw null; } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAccept { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAcceptCharset { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAcceptEncoding { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAcceptLanguage { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAccessControlRequestHeaders { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAccessControlRequestMethod { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAllow { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAuthorization { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderCacheControl { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderConnection { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentEncoding { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentLanguage { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentLength { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentLocation { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentMD5 { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentRange { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentType { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderCookie { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderCorrelationContext { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderDate { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderDNT { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderExpect { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderExpires { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderFrom { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderHost { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderIfMatch { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderIfModifiedSince { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderIfNoneMatch { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderIfRange { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderIfUnmodifiedSince { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderKeepAlive { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderLastModified { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderMaxForwards { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderOrigin { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderPragma { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderProxyAuthorization { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderRange { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderReferer { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderRequestId { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderTE { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderTraceParent { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderTraceState { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderTrailer { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderTransferEncoding { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderTranslate { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderUpgrade { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderUpgradeInsecureRequests { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderUserAgent { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderVia { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderWarning { get { throw null; } set { } }
        public int HostCount { get { throw null; } }
        protected override bool AddValueFast(string key, Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        public void Append(System.Span<byte> name, System.Span<byte> value) { }
        protected override void ClearFast() { }
        protected override bool CopyToFast(System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>[] array, int arrayIndex) { throw null; }
        protected override int GetCountFast() { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpRequestHeaders.Enumerator GetEnumerator() { throw null; }
        protected override System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> GetEnumeratorFast() { throw null; }
        public void OnHeadersComplete() { }
        protected override bool RemoveFast(string key) { throw null; }
        protected override void SetValueFast(string key, Microsoft.Extensions.Primitives.StringValues value) { }
        protected override bool TryGetValueFast(string key, out Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.IEnumerator, System.IDisposable
        {
            private readonly Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpRequestHeaders _collection;
            private readonly long _bits;
            private int _next;
            private System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> _current;
            private readonly bool _hasUnknown;
            private System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues>.Enumerator _unknownEnumerator;
            internal Enumerator(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpRequestHeaders collection) { throw null; }
            public System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            public void Reset() { }
        }
    }
    internal sealed partial class HttpResponseHeaders : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpHeaders
    {
        public HttpResponseHeaders() { }
        public bool HasConnection { get { throw null; } }
        public bool HasDate { get { throw null; } }
        public bool HasServer { get { throw null; } }
        public bool HasTransferEncoding { get { throw null; } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAcceptRanges { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAccessControlAllowCredentials { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAccessControlAllowHeaders { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAccessControlAllowMethods { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAccessControlAllowOrigin { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAccessControlExposeHeaders { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAccessControlMaxAge { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAge { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderAllow { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderCacheControl { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderConnection { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentEncoding { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentLanguage { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentLength { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentLocation { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentMD5 { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentRange { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderContentType { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderDate { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderETag { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderExpires { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderKeepAlive { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderLastModified { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderLocation { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderPragma { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderProxyAuthenticate { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderRetryAfter { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderServer { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderSetCookie { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderTrailer { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderTransferEncoding { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderUpgrade { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderVary { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderVia { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderWarning { get { throw null; } set { } }
        public Microsoft.Extensions.Primitives.StringValues HeaderWWWAuthenticate { get { throw null; } set { } }
        protected override bool AddValueFast(string key, Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        protected override void ClearFast() { }
        internal void CopyTo(ref System.Buffers.BufferWriter<System.IO.Pipelines.PipeWriter> buffer) { }
        internal void CopyToFast(ref System.Buffers.BufferWriter<System.IO.Pipelines.PipeWriter> output) { }
        protected override bool CopyToFast(System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>[] array, int arrayIndex) { throw null; }
        protected override int GetCountFast() { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseHeaders.Enumerator GetEnumerator() { throw null; }
        protected override System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> GetEnumeratorFast() { throw null; }
        protected override bool RemoveFast(string key) { throw null; }
        public void SetRawConnection(Microsoft.Extensions.Primitives.StringValues value, byte[] raw) { }
        public void SetRawDate(Microsoft.Extensions.Primitives.StringValues value, byte[] raw) { }
        public void SetRawServer(Microsoft.Extensions.Primitives.StringValues value, byte[] raw) { }
        public void SetRawTransferEncoding(Microsoft.Extensions.Primitives.StringValues value, byte[] raw) { }
        protected override void SetValueFast(string key, Microsoft.Extensions.Primitives.StringValues value) { }
        protected override bool TryGetValueFast(string key, out Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute]
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.IEnumerator, System.IDisposable
        {
            private readonly Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseHeaders _collection;
            private readonly long _bits;
            private int _next;
            private System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> _current;
            private readonly bool _hasUnknown;
            private System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues>.Enumerator _unknownEnumerator;
            internal Enumerator(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseHeaders collection) { throw null; }
            public System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            public void Reset() { }
        }
    }
    internal partial class HttpResponseTrailers : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpHeaders
    {
        public HttpResponseTrailers() { }
        public Microsoft.Extensions.Primitives.StringValues HeaderETag { get { throw null; } set { } }
        protected override bool AddValueFast(string key, Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        protected override void ClearFast() { }
        protected override bool CopyToFast(System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>[] array, int arrayIndex) { throw null; }
        protected override int GetCountFast() { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseTrailers.Enumerator GetEnumerator() { throw null; }
        protected override System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>> GetEnumeratorFast() { throw null; }
        protected override bool RemoveFast(string key) { throw null; }
        protected override void SetValueFast(string key, Microsoft.Extensions.Primitives.StringValues value) { }
        protected override bool TryGetValueFast(string key, out Microsoft.Extensions.Primitives.StringValues value) { throw null; }
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>>, System.Collections.IEnumerator, System.IDisposable
        {
            private readonly Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseTrailers _collection;
            private readonly long _bits;
            private int _next;
            private System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> _current;
            private readonly bool _hasUnknown;
            private System.Collections.Generic.Dictionary<string, Microsoft.Extensions.Primitives.StringValues>.Enumerator _unknownEnumerator;
            internal Enumerator(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseTrailers collection) { throw null; }
            public System.Collections.Generic.KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            public void Reset() { }
        }
    }
    internal partial class Http1Connection : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol, Microsoft.AspNetCore.Server.Kestrel.Core.Features.IHttpMinRequestBodyDataRateFeature, Microsoft.AspNetCore.Server.Kestrel.Core.Features.IHttpMinResponseDataRateFeature, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.IRequestProcessor
    {
        protected readonly long _keepAliveTicks;
        public Http1Connection(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.HttpConnectionContext context) : base (default(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.HttpConnectionContext)) { }
        public System.IO.Pipelines.PipeReader Input { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public System.Buffers.MemoryPool<byte> MemoryPool { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate Microsoft.AspNetCore.Server.Kestrel.Core.Features.IHttpMinRequestBodyDataRateFeature.MinDataRate { get { throw null; } set { } }
        Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate Microsoft.AspNetCore.Server.Kestrel.Core.Features.IHttpMinResponseDataRateFeature.MinDataRate { get { throw null; } set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate MinResponseDataRate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool RequestTimedOut { get { throw null; } }
        public void Abort(Microsoft.AspNetCore.Connections.ConnectionAbortedException abortReason) { }
        protected override void ApplicationAbort() { }
        protected override bool BeginRead(out System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> awaitable) { throw null; }
        protected override void BeginRequestProcessing() { }
        protected override Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.MessageBody CreateMessageBody() { throw null; }
        protected override string CreateRequestId() { throw null; }
        internal void EnsureHostHeaderExists() { }
        public void HandleReadDataRateTimeout() { }
        public void HandleRequestHeadersTimeout() { }
        void Microsoft.AspNetCore.Server.Kestrel.Core.Internal.IRequestProcessor.Tick(System.DateTimeOffset now) { }
        public void OnInputOrOutputCompleted() { }
        protected override void OnRequestProcessingEnded() { }
        protected override void OnRequestProcessingEnding() { }
        protected override void OnReset() { }
        public void OnStartLine(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod method, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpVersion version, System.Span<byte> target, System.Span<byte> path, System.Span<byte> query, System.Span<byte> customMethod, bool pathEncoded) { }
        public void ParseRequest(in System.Buffers.ReadOnlySequence<byte> buffer, out System.SequencePosition consumed, out System.SequencePosition examined) { throw null; }
        public void SendTimeoutResponse() { }
        public void StopProcessingNextRequest() { }
        public bool TakeMessageHeaders(in System.Buffers.ReadOnlySequence<byte> buffer, bool trailers, out System.SequencePosition consumed, out System.SequencePosition examined) { throw null; }
        public bool TakeStartLine(in System.Buffers.ReadOnlySequence<byte> buffer, out System.SequencePosition consumed, out System.SequencePosition examined) { throw null; }
        protected override bool TryParseRequest(System.IO.Pipelines.ReadResult result, out bool endConnection) { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal readonly partial struct Http1ParsingHandler : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpHeadersHandler, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpRequestLineHandler
    {
        public readonly Http1Connection Connection;
        public readonly bool Trailers;
        public Http1ParsingHandler(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1Connection connection) { throw null; }
        public Http1ParsingHandler(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.Http1Connection connection, bool trailers) { throw null; }
        public void OnHeader(System.Span<byte> name, System.Span<byte> value) { }
        public void OnHeadersComplete() { }
        public void OnStartLine(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod method, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpVersion version, System.Span<byte> target, System.Span<byte> path, System.Span<byte> query, System.Span<byte> customMethod, bool pathEncoded) { }
    }
    internal partial interface IHttpOutputProducer
    {
        void Advance(int bytes);
        void CancelPendingFlush();
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FirstWriteAsync(int statusCode, string reasonPhrase, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseHeaders responseHeaders, bool autoChunk, System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FirstWriteChunkedAsync(int statusCode, string reasonPhrase, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseHeaders responseHeaders, bool autoChunk, System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FlushAsync(System.Threading.CancellationToken cancellationToken);
        System.Memory<byte> GetMemory(int sizeHint = 0);
        System.Span<byte> GetSpan(int sizeHint = 0);
        void Reset();
        void Stop();
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> Write100ContinueAsync();
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteChunkAsync(System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.Task WriteDataAsync(System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken);
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteDataToPipeAsync(System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken);
        void WriteResponseHeaders(int statusCode, string reasonPhrase, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseHeaders responseHeaders, bool autoChunk, bool appCompleted);
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteStreamSuffixAsync();
    }
    internal partial interface IHttpResponseControl
    {
        void Advance(int bytes);
        void CancelPendingFlush();
        System.Threading.Tasks.Task CompleteAsync(System.Exception exception = null);
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FlushPipeAsync(System.Threading.CancellationToken cancellationToken);
        System.Memory<byte> GetMemory(int sizeHint = 0);
        System.Span<byte> GetSpan(int sizeHint = 0);
        void ProduceContinue();
        System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WritePipeAsync(System.ReadOnlyMemory<byte> source, System.Threading.CancellationToken cancellationToken);
    }
    internal abstract partial class MessageBody
    {
        protected long _alreadyTimedBytes;
        protected bool _backpressure;
        protected long _examinedUnconsumedBytes;
        protected bool _timingEnabled;
        protected MessageBody(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol context) { }
        public virtual bool IsEmpty { get { throw null; } }
        protected Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace Log { get { throw null; } }
        public bool RequestKeepAlive { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]protected set { } }
        public bool RequestUpgrade { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]protected set { } }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.MessageBody ZeroContentLengthClose { get { throw null; } }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.MessageBody ZeroContentLengthKeepAlive { get { throw null; } }
        protected void AddAndCheckConsumedBytes(long consumedBytes) { }
        public abstract void AdvanceTo(System.SequencePosition consumed);
        public abstract void AdvanceTo(System.SequencePosition consumed, System.SequencePosition examined);
        public abstract void CancelPendingRead();
        public abstract void Complete(System.Exception exception);
        public virtual System.Threading.Tasks.Task ConsumeAsync() { throw null; }
        protected void CountBytesRead(long bytesInReadResult) { }
        protected long OnAdvance(System.IO.Pipelines.ReadResult readResult, System.SequencePosition consumed, System.SequencePosition examined) { throw null; }
        protected virtual System.Threading.Tasks.Task OnConsumeAsync() { throw null; }
        protected virtual void OnDataRead(long bytesRead) { }
        protected virtual void OnReadStarted() { }
        protected virtual void OnReadStarting() { }
        protected virtual System.Threading.Tasks.Task OnStopAsync() { throw null; }
        public abstract System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> ReadAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken));
        protected System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> StartTimingReadAsync(System.Threading.Tasks.ValueTask<System.IO.Pipelines.ReadResult> readAwaitable, System.Threading.CancellationToken cancellationToken) { throw null; }
        public virtual System.Threading.Tasks.Task StopAsync() { throw null; }
        protected void StopTimingRead(long bytesInReadResult) { }
        protected void TryProduceContinue() { }
        public abstract bool TryRead(out System.IO.Pipelines.ReadResult readResult);
        protected void TryStart() { }
        protected void TryStop() { }
    }
    internal enum RequestProcessingStatus
    {
        RequestPending = 0,
        ParsingRequestLine = 1,
        ParsingHeaders = 2,
        AppStarted = 3,
        HeadersCommitted = 4,
        HeadersFlushed = 5,
        ResponseCompleted = 6,
    }
    [System.FlagsAttribute]
    internal enum TransferCoding
    {
        None = 0,
        Chunked = 1,
        Other = 2,
    }
}

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    internal static partial class Http2FrameReader
    {
        public const int HeaderLength = 9;
        public const int SettingSize = 6;
        public static int GetPayloadFieldsLength(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2Frame frame) { throw null; }
        public static bool ReadFrame(in System.Buffers.ReadOnlySequence<byte> readableBuffer, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2Frame frame, uint maxFrameSize, out System.Buffers.ReadOnlySequence<byte> framePayload) { throw null; }
        public static System.Collections.Generic.IList<Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2PeerSetting> ReadSettings(in System.Buffers.ReadOnlySequence<byte> payload) { throw null; }
    }
    internal static partial class Bitshifter
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public static uint ReadUInt24BigEndian(System.ReadOnlySpan<byte> source) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public static uint ReadUInt31BigEndian(System.ReadOnlySpan<byte> source) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public static void WriteUInt24BigEndian(System.Span<byte> destination, uint value) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public static void WriteUInt31BigEndian(System.Span<byte> destination, uint value) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public static void WriteUInt31BigEndian(System.Span<byte> destination, uint value, bool preserveHighestBit) { }
    }
    internal partial class Http2Connection : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpHeadersHandler, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.IHttp2StreamLifetimeHandler, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.IRequestProcessor
    {
        public Http2Connection(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.HttpConnectionContext context) { }
        public static byte[] ClientPreface { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public Microsoft.AspNetCore.Http.Features.IFeatureCollection ConnectionFeatures { get { throw null; } }
        public string ConnectionId { get { throw null; } }
        public System.IO.Pipelines.PipeReader Input { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerLimits Limits { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace Log { get { throw null; } }
        internal Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2PeerSettings ServerSettings { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ISystemClock SystemClock { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ITimeoutControl TimeoutControl { get { throw null; } }
        public void Abort(Microsoft.AspNetCore.Connections.ConnectionAbortedException ex) { }
        public void DecrementActiveClientStreamCount() { }
        public void HandleReadDataRateTimeout() { }
        public void HandleRequestHeadersTimeout() { }
        public void IncrementActiveClientStreamCount() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task ProcessRequestsAsync<TContext>(Microsoft.AspNetCore.Hosting.Server.IHttpApplication<TContext> application) { throw null; }
        void Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.IHttp2StreamLifetimeHandler.OnStreamCompleted(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2Stream stream) { }
        void Microsoft.AspNetCore.Server.Kestrel.Core.Internal.IRequestProcessor.Tick(System.DateTimeOffset now) { }
        public void OnHeader(System.Span<byte> name, System.Span<byte> value) { }
        public void OnHeadersComplete() { }
        public void OnInputOrOutputCompleted() { }
        public void StopProcessingNextRequest() { }
        public void StopProcessingNextRequest(bool serverInitiated) { }
    }
    internal partial interface IHttp2StreamLifetimeHandler
    {
        void DecrementActiveClientStreamCount();
        void OnStreamCompleted(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2Stream stream);
    }
    internal partial class Http2FrameWriter
    {
        public Http2FrameWriter(System.IO.Pipelines.PipeWriter outputPipeWriter, Microsoft.AspNetCore.Connections.ConnectionContext connectionContext, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2Connection http2Connection, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl.OutputFlowControl connectionOutputFlowControl, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ITimeoutControl timeoutControl, Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate minResponseDataRate, string connectionId, System.Buffers.MemoryPool<byte> memoryPool, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace log) { }
        public void Abort(Microsoft.AspNetCore.Connections.ConnectionAbortedException error) { }
        public void AbortPendingStreamDataWrites(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl.StreamOutputFlowControl flowControl) { }
        public void Complete() { }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FlushAsync(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpOutputAborter outputAborter, System.Threading.CancellationToken cancellationToken) { throw null; }
        public bool TryUpdateConnectionWindow(int bytes) { throw null; }
        public bool TryUpdateStreamWindow(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl.StreamOutputFlowControl flowControl, int bytes) { throw null; }
        public void UpdateMaxFrameSize(uint maxFrameSize) { }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> Write100ContinueAsync(int streamId) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteDataAsync(int streamId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl.StreamOutputFlowControl flowControl, in System.Buffers.ReadOnlySequence<byte> data, bool endStream) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteGoAwayAsync(int lastStreamId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode errorCode) { throw null; }
        internal static void WriteHeader(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2Frame frame, System.IO.Pipelines.PipeWriter output) { }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WritePingAsync(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2PingFrameFlags flags, in System.Buffers.ReadOnlySequence<byte> payload) { throw null; }
        public void WriteResponseHeaders(int streamId, int statusCode, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2HeadersFrameFlags headerFrameFlags, Microsoft.AspNetCore.Http.IHeaderDictionary headers) { }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteResponseTrailers(int streamId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpResponseTrailers headers) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteRstStreamAsync(int streamId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode errorCode) { throw null; }
        internal static void WriteSettings(System.Collections.Generic.IList<Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2PeerSetting> settings, System.Span<byte> destination) { }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteSettingsAckAsync() { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteSettingsAsync(System.Collections.Generic.IList<Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2PeerSetting> settings) { throw null; }
        public System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> WriteWindowUpdateAsync(int streamId, int sizeIncrement) { throw null; }
    }
    internal abstract partial class Http2Stream : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol, Microsoft.AspNetCore.Http.Features.IHttpResetFeature, Microsoft.AspNetCore.Http.Features.IHttpResponseTrailersFeature, Microsoft.AspNetCore.Server.Kestrel.Core.Features.IHttp2StreamIdFeature, Microsoft.AspNetCore.Server.Kestrel.Core.Features.IHttpMinRequestBodyDataRateFeature, System.Threading.IThreadPoolWorkItem
    {
        public Http2Stream(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2StreamContext context) : base (default(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.HttpConnectionContext)) { }
        internal long DrainExpirationTicks { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool EndStreamReceived { get { throw null; } }
        public long? InputRemaining { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]internal set { } }
        Microsoft.AspNetCore.Http.IHeaderDictionary Microsoft.AspNetCore.Http.Features.IHttpResponseTrailersFeature.Trailers { get { throw null; } set { } }
        int Microsoft.AspNetCore.Server.Kestrel.Core.Features.IHttp2StreamIdFeature.StreamId { get { throw null; } }
        Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate Microsoft.AspNetCore.Server.Kestrel.Core.Features.IHttpMinRequestBodyDataRateFeature.MinDataRate { get { throw null; } set { } }
        public bool ReceivedEmptyRequestBody { get { throw null; } }
        public System.IO.Pipelines.Pipe RequestBodyPipe { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool RequestBodyStarted { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        internal bool RstStreamReceived { get { throw null; } }
        public int StreamId { get { throw null; } }
        public void Abort(System.IO.IOException abortReason) { }
        public void AbortRstStreamReceived() { }
        protected override void ApplicationAbort() { }
        protected override Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.MessageBody CreateMessageBody() { throw null; }
        protected override string CreateRequestId() { throw null; }
        public void DecrementActiveClientStreamCount() { }
        public abstract void Execute();
        void Microsoft.AspNetCore.Http.Features.IHttpResetFeature.Reset(int errorCode) { }
        public System.Threading.Tasks.Task OnDataAsync(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2Frame dataFrame, in System.Buffers.ReadOnlySequence<byte> payload) { throw null; }
        public void OnDataRead(int bytesRead) { }
        public void OnEndStreamReceived() { }
        protected override void OnErrorAfterResponseStarted() { }
        protected override void OnRequestProcessingEnded() { }
        protected override void OnReset() { }
        internal void ResetAndAbort(Microsoft.AspNetCore.Connections.ConnectionAbortedException abortReason, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode error) { }
        protected override bool TryParseRequest(System.IO.Pipelines.ReadResult result, out bool endConnection) { throw null; }
        public bool TryUpdateOutputWindow(int bytes) { throw null; }
    }
    internal sealed partial class Http2StreamContext : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.HttpConnectionContext
    {
        public Http2StreamContext() { }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2PeerSettings ClientPeerSettings { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl.InputFlowControl ConnectionInputFlowControl { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl.OutputFlowControl ConnectionOutputFlowControl { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2FrameWriter FrameWriter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2PeerSettings ServerPeerSettings { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int StreamId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.IHttp2StreamLifetimeHandler StreamLifetimeHandler { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    internal sealed partial class Http2ConnectionErrorException : System.Exception
    {
        public Http2ConnectionErrorException(string message, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode errorCode) { }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode ErrorCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    [System.FlagsAttribute]
    internal enum Http2ContinuationFrameFlags : byte
    {
        NONE = (byte)0,
        END_HEADERS = (byte)4,
    }
    [System.FlagsAttribute]
    internal enum Http2DataFrameFlags : byte
    {
        NONE = (byte)0,
        END_STREAM = (byte)1,
        PADDED = (byte)8,
    }
    internal enum Http2ErrorCode : uint
    {
        NO_ERROR = (uint)0,
        PROTOCOL_ERROR = (uint)1,
        INTERNAL_ERROR = (uint)2,
        FLOW_CONTROL_ERROR = (uint)3,
        SETTINGS_TIMEOUT = (uint)4,
        STREAM_CLOSED = (uint)5,
        FRAME_SIZE_ERROR = (uint)6,
        REFUSED_STREAM = (uint)7,
        CANCEL = (uint)8,
        COMPRESSION_ERROR = (uint)9,
        CONNECT_ERROR = (uint)10,
        ENHANCE_YOUR_CALM = (uint)11,
        INADEQUATE_SECURITY = (uint)12,
        HTTP_1_1_REQUIRED = (uint)13,
    }
    internal partial class Http2Frame
    {
        public Http2Frame() { }
        public bool ContinuationEndHeaders { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ContinuationFrameFlags ContinuationFlags { get { throw null; } set { } }
        public bool DataEndStream { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2DataFrameFlags DataFlags { get { throw null; } set { } }
        public bool DataHasPadding { get { throw null; } }
        public byte DataPadLength { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int DataPayloadLength { get { throw null; } }
        public byte Flags { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode GoAwayErrorCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int GoAwayLastStreamId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool HeadersEndHeaders { get { throw null; } }
        public bool HeadersEndStream { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2HeadersFrameFlags HeadersFlags { get { throw null; } set { } }
        public bool HeadersHasPadding { get { throw null; } }
        public bool HeadersHasPriority { get { throw null; } }
        public byte HeadersPadLength { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int HeadersPayloadLength { get { throw null; } }
        public byte HeadersPriorityWeight { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int HeadersStreamDependency { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int PayloadLength { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool PingAck { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2PingFrameFlags PingFlags { get { throw null; } set { } }
        public bool PriorityIsExclusive { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int PriorityStreamDependency { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public byte PriorityWeight { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode RstStreamErrorCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool SettingsAck { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2SettingsFrameFlags SettingsFlags { get { throw null; } set { } }
        public int StreamId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2FrameType Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int WindowUpdateSizeIncrement { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public void PrepareContinuation(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ContinuationFrameFlags flags, int streamId) { }
        public void PrepareData(int streamId, byte? padLength = default(byte?)) { }
        public void PrepareGoAway(int lastStreamId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode errorCode) { }
        public void PrepareHeaders(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2HeadersFrameFlags flags, int streamId) { }
        public void PreparePing(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2PingFrameFlags flags) { }
        public void PreparePriority(int streamId, int streamDependency, bool exclusive, byte weight) { }
        public void PrepareRstStream(int streamId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode errorCode) { }
        public void PrepareSettings(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2SettingsFrameFlags flags) { }
        public void PrepareWindowUpdate(int streamId, int sizeIncrement) { }
        internal object ShowFlags() { throw null; }
        public override string ToString() { throw null; }
    }
    internal enum Http2FrameType : byte
    {
        DATA = (byte)0,
        HEADERS = (byte)1,
        PRIORITY = (byte)2,
        RST_STREAM = (byte)3,
        SETTINGS = (byte)4,
        PUSH_PROMISE = (byte)5,
        PING = (byte)6,
        GOAWAY = (byte)7,
        WINDOW_UPDATE = (byte)8,
        CONTINUATION = (byte)9,
    }
    [System.FlagsAttribute]
    internal enum Http2HeadersFrameFlags : byte
    {
        NONE = (byte)0,
        END_STREAM = (byte)1,
        END_HEADERS = (byte)4,
        PADDED = (byte)8,
        PRIORITY = (byte)32,
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal readonly partial struct Http2PeerSetting
    {
        private readonly int _dummyPrimitive;
        public Http2PeerSetting(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2SettingsParameter parameter, uint value) { throw null; }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2SettingsParameter Parameter { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public uint Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    internal partial class Http2PeerSettings
    {
        public const bool DefaultEnablePush = true;
        public const uint DefaultHeaderTableSize = (uint)4096;
        public const uint DefaultInitialWindowSize = (uint)65535;
        public const uint DefaultMaxConcurrentStreams = (uint)4294967295;
        public const uint DefaultMaxFrameSize = (uint)16384;
        public const uint DefaultMaxHeaderListSize = (uint)4294967295;
        internal const int MaxAllowedMaxFrameSize = 16777215;
        public const uint MaxWindowSize = (uint)2147483647;
        internal const int MinAllowedMaxFrameSize = 16384;
        public Http2PeerSettings() { }
        public bool EnablePush { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public uint HeaderTableSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public uint InitialWindowSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public uint MaxConcurrentStreams { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public uint MaxFrameSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public uint MaxHeaderListSize { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        internal System.Collections.Generic.IList<Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2PeerSetting> GetNonProtocolDefaults() { throw null; }
        public void Update(System.Collections.Generic.IList<Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2PeerSetting> settings) { }
    }
    [System.FlagsAttribute]
    internal enum Http2PingFrameFlags : byte
    {
        NONE = (byte)0,
        ACK = (byte)1,
    }
    [System.FlagsAttribute]
    internal enum Http2SettingsFrameFlags : byte
    {
        NONE = (byte)0,
        ACK = (byte)1,
    }
    internal enum Http2SettingsParameter : ushort
    {
        SETTINGS_HEADER_TABLE_SIZE = (ushort)1,
        SETTINGS_ENABLE_PUSH = (ushort)2,
        SETTINGS_MAX_CONCURRENT_STREAMS = (ushort)3,
        SETTINGS_INITIAL_WINDOW_SIZE = (ushort)4,
        SETTINGS_MAX_FRAME_SIZE = (ushort)5,
        SETTINGS_MAX_HEADER_LIST_SIZE = (ushort)6,
    }
    internal sealed partial class Http2StreamErrorException : System.Exception
    {
        public Http2StreamErrorException(int streamId, string message, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode errorCode) { }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode ErrorCode { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public int StreamId { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
}

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl
{
    internal partial class OutputFlowControlAwaitable : System.Runtime.CompilerServices.ICriticalNotifyCompletion, System.Runtime.CompilerServices.INotifyCompletion
    {
        public OutputFlowControlAwaitable() { }
        public bool IsCompleted { get { throw null; } }
        public void Complete() { }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl.OutputFlowControlAwaitable GetAwaiter() { throw null; }
        public void GetResult() { }
        public void OnCompleted(System.Action continuation) { }
        public void UnsafeOnCompleted(System.Action continuation) { }
    }
    internal partial class StreamOutputFlowControl
    {
        public StreamOutputFlowControl(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl.OutputFlowControl connectionLevelFlowControl, uint initialWindowSize) { }
        public int Available { get { throw null; } }
        public bool IsAborted { get { throw null; } }
        public void Abort() { }
        public void Advance(int bytes) { }
        public int AdvanceUpToAndWait(long bytes, out Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl.OutputFlowControlAwaitable awaitable) { throw null; }
        public bool TryUpdateWindow(int bytes) { throw null; }
    }
    internal partial class OutputFlowControl
    {
        public OutputFlowControl(uint initialWindowSize) { }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl.OutputFlowControlAwaitable AvailabilityAwaitable { get { throw null; } }
        public int Available { get { throw null; } }
        public bool IsAborted { get { throw null; } }
        public void Abort() { }
        public void Advance(int bytes) { }
        public bool TryUpdateWindow(int bytes) { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal partial struct FlowControl
    {
        private int _dummyPrimitive;
        public FlowControl(uint initialWindowSize) { throw null; }
        public int Available { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public bool IsAborted { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void Abort() { }
        public void Advance(int bytes) { }
        public bool TryUpdateWindow(int bytes) { throw null; }
    }
    internal partial class InputFlowControl
    {
        public InputFlowControl(uint initialWindowSize, uint minWindowSizeIncrement) { }
        public bool IsAvailabilityLow { get { throw null; } }
        public int Abort() { throw null; }
        public void StopWindowUpdates() { }
        public bool TryAdvance(int bytes) { throw null; }
        public bool TryUpdateWindow(int bytes, out int updateSize) { throw null; }
    }
}

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack
{
    internal sealed partial class HuffmanDecodingException : System.Exception
    {
        public HuffmanDecodingException(string message) { }
    }
    internal static partial class IntegerEncoder
    {
        public static bool Encode(int i, int n, System.Span<byte> buffer, out int length) { throw null; }
    }
    internal partial class IntegerDecoder
    {
        public IntegerDecoder() { }
        public bool BeginTryDecode(byte b, int prefixLength, out int result) { throw null; }
        public static void ThrowIntegerTooBigException() { }
        public bool TryDecode(byte b, out int result) { throw null; }
    }
    internal partial class Huffman
    {
        public Huffman() { }
        public static int Decode(System.ReadOnlySpan<byte> src, System.Span<byte> dst) { throw null; }
        internal static int DecodeValue(uint data, int validBits, out int decodedBits) { throw null; }
        public static (uint encoded, int bitLength) Encode(int data) { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal readonly partial struct HeaderField
    {
        public const int RfcOverhead = 32;
        private readonly object _dummy;
        public HeaderField(System.Span<byte> name, System.Span<byte> value) { throw null; }
        public int Length { get { throw null; } }
        public byte[] Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public byte[] Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public static int GetLength(int nameLength, int valueLength) { throw null; }
    }
    internal partial class HPackEncoder
    {
        public HPackEncoder() { }
        public bool BeginEncode(System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>> headers, System.Span<byte> buffer, out int length) { throw null; }
        public bool BeginEncode(int statusCode, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>> headers, System.Span<byte> buffer, out int length) { throw null; }
        public bool Encode(System.Span<byte> buffer, out int length) { throw null; }
    }
    internal partial class DynamicTable
    {
        public DynamicTable(int maxSize) { }
        public int Count { get { throw null; } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack.HeaderField this[int index] { get { throw null; } }
        public int MaxSize { get { throw null; } }
        public int Size { get { throw null; } }
        public void Insert(System.Span<byte> name, System.Span<byte> value) { }
        public void Resize(int maxSize) { }
    }
    internal partial class HPackDecoder
    {
        public HPackDecoder(int maxDynamicTableSize, int maxRequestHeaderFieldSize) { }
        internal HPackDecoder(int maxDynamicTableSize, int maxRequestHeaderFieldSize, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack.DynamicTable dynamicTable) { }
        public void Decode(in System.Buffers.ReadOnlySequence<byte> data, bool endHeaders, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpHeadersHandler handler) { }
    }
    internal sealed partial class HPackDecodingException : System.Exception
    {
        public HPackDecodingException(string message) { }
        public HPackDecodingException(string message, System.Exception innerException) { }
    }
    internal sealed partial class HPackEncodingException : System.Exception
    {
        public HPackEncodingException(string message) { }
        public HPackEncodingException(string message, System.Exception innerException) { }
    }
}

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    internal static partial class Constants
    {
        public static readonly string DefaultServerAddress;
        public static readonly string DefaultServerHttpsAddress;
        public const int MaxExceptionDetailSize = 128;
        public const string PipeDescriptorPrefix = "pipefd:";
        public static readonly System.TimeSpan RequestBodyDrainTimeout;
        public const string ServerName = "Kestrel";
        public const string SocketDescriptorPrefix = "sockfd:";
        public const string UnixPipeHostPrefix = "unix:/";
    }
    internal static partial class HttpUtilities
    {
        public const string Http10Version = "HTTP/1.0";
        public const string Http11Version = "HTTP/1.1";
        public const string Http2Version = "HTTP/2";
        public const string HttpsUriScheme = "https://";
        public const string HttpUriScheme = "http://";
        public static string GetAsciiStringEscaped(this System.Span<byte> span, int maxChars) { throw null; }
        public static string GetAsciiStringNonNullCharacters(this System.Span<byte> span) { throw null; }
        public static string GetHeaderName(this System.Span<byte> span) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public static bool GetKnownHttpScheme(this System.Span<byte> span, out Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpScheme knownScheme) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]internal unsafe static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod GetKnownMethod(byte* data, int length, out int methodLength) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public static bool GetKnownMethod(this System.Span<byte> span, out Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod method, out int length) { throw null; }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod GetKnownMethod(string value) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]internal unsafe static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpVersion GetKnownVersion(byte* location, int length) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public static bool GetKnownVersion(this System.Span<byte> span, out Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpVersion knownVersion, out byte length) { throw null; }
        public static string GetRequestHeaderStringNonNullCharacters(this System.Span<byte> span, bool useLatin1) { throw null; }
        public static bool IsHostHeaderValid(string hostText) { throw null; }
        public static string MethodToString(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod method) { throw null; }
        public static string SchemeToString(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpScheme scheme) { throw null; }
        public static string VersionToString(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpVersion httpVersion) { throw null; }
    }
    internal abstract partial class WriteOnlyStream : System.IO.Stream
    {
        protected WriteOnlyStream() { }
        public override bool CanRead { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public override int ReadTimeout { get { throw null; } set { } }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    internal sealed partial class ThrowingWasUpgradedWriteOnlyStream : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.WriteOnlyStream
    {
        public ThrowingWasUpgradedWriteOnlyStream() { }
        public override bool CanSeek { get { throw null; } }
        public override long Length { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public override void Flush() { }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    internal partial class Disposable : System.IDisposable
    {
        public Disposable(System.Action dispose) { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
    }
    internal sealed partial class DebuggerWrapper : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IDebugger
    {
        public bool IsAttached { get { throw null; } }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IDebugger Singleton { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
    }
    internal partial class SystemClock : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ISystemClock
    {
        public SystemClock() { }
        public System.DateTimeOffset UtcNow { get { throw null; } }
        public long UtcNowTicks { get { throw null; } }
        public System.DateTimeOffset UtcNowUnsynchronized { get { throw null; } }
    }
    internal partial class HeartbeatManager : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IHeartbeatHandler, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ISystemClock
    {
        public HeartbeatManager(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ConnectionManager connectionManager) { }
        public System.DateTimeOffset UtcNow { get { throw null; } }
        public long UtcNowTicks { get { throw null; } }
        public System.DateTimeOffset UtcNowUnsynchronized { get { throw null; } }
        public void OnHeartbeat(System.DateTimeOffset now) { }
    }

    internal partial class StringUtilities
    {
        public StringUtilities() { }
        public static bool BytesOrdinalEqualsStringAndAscii(string previousValue, System.Span<byte> newValue) { throw null; }
        public static string ConcatAsHexSuffix(string str, char separator, uint number) { throw null; }
        public unsafe static bool TryGetAsciiString(byte* input, char* output, int count) { throw null; }
        public unsafe static bool TryGetLatin1String(byte* input, char* output, int count) { throw null; }
    }
    internal partial class TimeoutControl : Microsoft.AspNetCore.Server.Kestrel.Core.Features.IConnectionTimeoutFeature, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ITimeoutControl
    {
        public TimeoutControl(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ITimeoutHandler timeoutHandler) { }
        internal Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IDebugger Debugger { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.TimeoutReason TimerReason { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public void BytesRead(long count) { }
        public void BytesWrittenToBuffer(Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate minRate, long count) { }
        public void CancelTimeout() { }
        internal void Initialize(long nowTicks) { }
        public void InitializeHttp2(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl.InputFlowControl connectionInputFlowControl) { }
        void Microsoft.AspNetCore.Server.Kestrel.Core.Features.IConnectionTimeoutFeature.ResetTimeout(System.TimeSpan timeSpan) { }
        void Microsoft.AspNetCore.Server.Kestrel.Core.Features.IConnectionTimeoutFeature.SetTimeout(System.TimeSpan timeSpan) { }
        public void ResetTimeout(long ticks, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.TimeoutReason timeoutReason) { }
        public void SetTimeout(long ticks, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.TimeoutReason timeoutReason) { }
        public void StartRequestBody(Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate minRate) { }
        public void StartTimingRead() { }
        public void StartTimingWrite() { }
        public void StopRequestBody() { }
        public void StopTimingRead() { }
        public void StopTimingWrite() { }
        public void Tick(System.DateTimeOffset now) { }
    }
    internal partial class KestrelTrace : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace, Microsoft.Extensions.Logging.ILogger
    {
        public KestrelTrace(Microsoft.Extensions.Logging.ILogger logger) { }
        public virtual void ApplicationAbortedConnection(string connectionId, string traceIdentifier) { }
        public virtual void ApplicationError(string connectionId, string traceIdentifier, System.Exception ex) { }
        public virtual void ApplicationNeverCompleted(string connectionId) { }
        public virtual System.IDisposable BeginScope<TState>(TState state) { throw null; }
        public virtual void ConnectionAccepted(string connectionId) { }
        public virtual void ConnectionBadRequest(string connectionId, Microsoft.AspNetCore.Server.Kestrel.Core.BadHttpRequestException ex) { }
        public virtual void ConnectionDisconnect(string connectionId) { }
        public virtual void ConnectionHeadResponseBodyWrite(string connectionId, long count) { }
        public virtual void ConnectionKeepAlive(string connectionId) { }
        public virtual void ConnectionPause(string connectionId) { }
        public virtual void ConnectionRejected(string connectionId) { }
        public virtual void ConnectionResume(string connectionId) { }
        public virtual void ConnectionStart(string connectionId) { }
        public virtual void ConnectionStop(string connectionId) { }
        public virtual void HeartbeatSlow(System.TimeSpan interval, System.DateTimeOffset now) { }
        public virtual void HPackDecodingError(string connectionId, int streamId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack.HPackDecodingException ex) { }
        public virtual void HPackEncodingError(string connectionId, int streamId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack.HPackEncodingException ex) { }
        public virtual void Http2ConnectionClosed(string connectionId, int highestOpenedStreamId) { }
        public virtual void Http2ConnectionClosing(string connectionId) { }
        public virtual void Http2ConnectionError(string connectionId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ConnectionErrorException ex) { }
        public void Http2FrameReceived(string connectionId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2Frame frame) { }
        public void Http2FrameSending(string connectionId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2Frame frame) { }
        public virtual void Http2StreamError(string connectionId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2StreamErrorException ex) { }
        public void Http2StreamResetAbort(string traceIdentifier, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode error, Microsoft.AspNetCore.Connections.ConnectionAbortedException abortReason) { }
        public virtual bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) { throw null; }
        public virtual void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, System.Exception exception, System.Func<TState, System.Exception, string> formatter) { }
        public virtual void NotAllConnectionsAborted() { }
        public virtual void NotAllConnectionsClosedGracefully() { }
        public virtual void RequestBodyDone(string connectionId, string traceIdentifier) { }
        public virtual void RequestBodyDrainTimedOut(string connectionId, string traceIdentifier) { }
        public virtual void RequestBodyMinimumDataRateNotSatisfied(string connectionId, string traceIdentifier, double rate) { }
        public virtual void RequestBodyNotEntirelyRead(string connectionId, string traceIdentifier) { }
        public virtual void RequestBodyStart(string connectionId, string traceIdentifier) { }
        public virtual void RequestProcessingError(string connectionId, System.Exception ex) { }
        public virtual void ResponseMinimumDataRateNotSatisfied(string connectionId, string traceIdentifier) { }
    }
    internal partial class BodyControl
    {
        public BodyControl(Microsoft.AspNetCore.Http.Features.IHttpBodyControlFeature bodyControl, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.IHttpResponseControl responseControl) { }
        public void Abort(System.Exception error) { }
        public (System.IO.Stream request, System.IO.Stream response, System.IO.Pipelines.PipeReader reader, System.IO.Pipelines.PipeWriter writer) Start(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.MessageBody body) { throw null; }
        public System.Threading.Tasks.Task StopAsync() { throw null; }
        public System.IO.Stream Upgrade() { throw null; }
    }
    internal partial class ConnectionManager
    {
        public ConnectionManager(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace trace, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ResourceCounter upgradedConnections) { }
        public ConnectionManager(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace trace, long? upgradedConnectionLimit) { }
        public Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ResourceCounter UpgradedConnectionCount { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<bool> AbortAllConnectionsAsync() { throw null; }
        public void AddConnection(long id, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.KestrelConnection connection) { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        public System.Threading.Tasks.Task<bool> CloseAllConnectionsAsync(System.Threading.CancellationToken token) { throw null; }
        public void RemoveConnection(long id) { }
        public void Walk(System.Action<Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.KestrelConnection> callback) { }
    }
    internal partial class Heartbeat : System.IDisposable
    {
        public static readonly System.TimeSpan Interval;
        public Heartbeat(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IHeartbeatHandler[] callbacks, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ISystemClock systemClock, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IDebugger debugger, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace trace) { }
        public void Dispose() { }
        internal void OnHeartbeat() { }
        public void Start() { }
    }
    internal partial interface IDebugger
    {
        bool IsAttached { get; }
    }
    internal partial interface IHeartbeatHandler
    {
        void OnHeartbeat(System.DateTimeOffset now);
    }
    internal partial interface IKestrelTrace : Microsoft.Extensions.Logging.ILogger
    {
        void ApplicationAbortedConnection(string connectionId, string traceIdentifier);
        void ApplicationError(string connectionId, string traceIdentifier, System.Exception ex);
        void ApplicationNeverCompleted(string connectionId);
        void ConnectionAccepted(string connectionId);
        void ConnectionBadRequest(string connectionId, Microsoft.AspNetCore.Server.Kestrel.Core.BadHttpRequestException ex);
        void ConnectionDisconnect(string connectionId);
        void ConnectionHeadResponseBodyWrite(string connectionId, long count);
        void ConnectionKeepAlive(string connectionId);
        void ConnectionPause(string connectionId);
        void ConnectionRejected(string connectionId);
        void ConnectionResume(string connectionId);
        void ConnectionStart(string connectionId);
        void ConnectionStop(string connectionId);
        void HeartbeatSlow(System.TimeSpan interval, System.DateTimeOffset now);
        void HPackDecodingError(string connectionId, int streamId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack.HPackDecodingException ex);
        void HPackEncodingError(string connectionId, int streamId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack.HPackEncodingException ex);
        void Http2ConnectionClosed(string connectionId, int highestOpenedStreamId);
        void Http2ConnectionClosing(string connectionId);
        void Http2ConnectionError(string connectionId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ConnectionErrorException ex);
        void Http2FrameReceived(string connectionId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2Frame frame);
        void Http2FrameSending(string connectionId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2Frame frame);
        void Http2StreamError(string connectionId, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2StreamErrorException ex);
        void Http2StreamResetAbort(string traceIdentifier, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.Http2ErrorCode error, Microsoft.AspNetCore.Connections.ConnectionAbortedException abortReason);
        void NotAllConnectionsAborted();
        void NotAllConnectionsClosedGracefully();
        void RequestBodyDone(string connectionId, string traceIdentifier);
        void RequestBodyDrainTimedOut(string connectionId, string traceIdentifier);
        void RequestBodyMinimumDataRateNotSatisfied(string connectionId, string traceIdentifier, double rate);
        void RequestBodyNotEntirelyRead(string connectionId, string traceIdentifier);
        void RequestBodyStart(string connectionId, string traceIdentifier);
        void RequestProcessingError(string connectionId, System.Exception ex);
        void ResponseMinimumDataRateNotSatisfied(string connectionId, string traceIdentifier);
    }
    internal partial interface ISystemClock
    {
        System.DateTimeOffset UtcNow { get; }
        long UtcNowTicks { get; }
        System.DateTimeOffset UtcNowUnsynchronized { get; }
    }
    internal partial interface ITimeoutControl
    {
        Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.TimeoutReason TimerReason { get; }
        void BytesRead(long count);
        void BytesWrittenToBuffer(Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate minRate, long count);
        void CancelTimeout();
        void InitializeHttp2(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl.InputFlowControl connectionInputFlowControl);
        void ResetTimeout(long ticks, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.TimeoutReason timeoutReason);
        void SetTimeout(long ticks, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.TimeoutReason timeoutReason);
        void StartRequestBody(Microsoft.AspNetCore.Server.Kestrel.Core.MinDataRate minRate);
        void StartTimingRead();
        void StartTimingWrite();
        void StopRequestBody();
        void StopTimingRead();
        void StopTimingWrite();
    }
    internal partial interface ITimeoutHandler
    {
        void OnTimeout(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.TimeoutReason reason);
    }
    internal partial class KestrelConnection : Microsoft.AspNetCore.Connections.Features.IConnectionCompleteFeature, Microsoft.AspNetCore.Connections.Features.IConnectionHeartbeatFeature, Microsoft.AspNetCore.Connections.Features.IConnectionLifetimeNotificationFeature, System.Threading.IThreadPoolWorkItem
    {
        public KestrelConnection(long id, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.ServiceContext serviceContext, Microsoft.AspNetCore.Connections.ConnectionDelegate connectionDelegate, Microsoft.AspNetCore.Connections.ConnectionContext connectionContext, Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.IKestrelTrace logger) { }
        public System.Threading.CancellationToken ConnectionClosedRequested { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Threading.Tasks.Task ExecutionTask { get { throw null; } }
        public Microsoft.AspNetCore.Connections.ConnectionContext TransportConnection { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public void Complete() { }
        [System.Diagnostics.DebuggerStepThroughAttribute]
        internal System.Threading.Tasks.Task ExecuteAsync() { throw null; }
        public System.Threading.Tasks.Task FireOnCompletedAsync() { throw null; }
        void Microsoft.AspNetCore.Connections.Features.IConnectionCompleteFeature.OnCompleted(System.Func<object, System.Threading.Tasks.Task> callback, object state) { }
        public void OnHeartbeat(System.Action<object> action, object state) { }
        public void RequestClose() { }
        void System.Threading.IThreadPoolWorkItem.Execute() { }
        public void TickHeartbeat() { }
    }
    internal abstract partial class ResourceCounter
    {
        protected ResourceCounter() { }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ResourceCounter Unlimited { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } }
        public static Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ResourceCounter Quota(long amount) { throw null; }
        public abstract void ReleaseOne();
        public abstract bool TryLockOne();
        internal partial class FiniteCounter : Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.ResourceCounter
        {
            public FiniteCounter(long max) { }
            internal long Count { get { throw null; } set { } }
            public override void ReleaseOne() { }
            public override bool TryLockOne() { throw null; }
        }
    }
    internal enum TimeoutReason
    {
        None = 0,
        KeepAlive = 1,
        RequestHeaders = 2,
        ReadDataRate = 3,
        WriteDataRate = 4,
        RequestBodyDrain = 5,
        TimeoutFeature = 6,
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure
{
    [System.Diagnostics.Tracing.EventSourceAttribute(Name="Microsoft-AspNetCore-Server-Kestrel")]
    internal sealed partial class KestrelEventSource : System.Diagnostics.Tracing.EventSource
    {
        public static readonly Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.KestrelEventSource Log;
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)][System.Diagnostics.Tracing.EventAttribute(5, Level=System.Diagnostics.Tracing.EventLevel.Verbose)]
        public void ConnectionRejected(string connectionId) { }
        [System.Diagnostics.Tracing.NonEventAttribute]
        public void ConnectionStart(Microsoft.AspNetCore.Connections.ConnectionContext connection) { }
        [System.Diagnostics.Tracing.NonEventAttribute]
        public void ConnectionStop(Microsoft.AspNetCore.Connections.ConnectionContext connection) { }
        [System.Diagnostics.Tracing.NonEventAttribute]
        public void RequestStart(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol httpProtocol) { }
        [System.Diagnostics.Tracing.NonEventAttribute]
        public void RequestStop(Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpProtocol httpProtocol) { }
    }
}
namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure.PipeWriterHelpers
{
    internal sealed partial class ConcurrentPipeWriter : System.IO.Pipelines.PipeWriter
    {
        public ConcurrentPipeWriter(System.IO.Pipelines.PipeWriter innerPipeWriter, System.Buffers.MemoryPool<byte> pool, object sync) { }
        public void Abort() { }
        public override void Advance(int bytes) { }
        public override void CancelPendingFlush() { }
        public override void Complete(System.Exception exception = null) { }
        public override System.Threading.Tasks.ValueTask<System.IO.Pipelines.FlushResult> FlushAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public override System.Memory<byte> GetMemory(int sizeHint = 0) { throw null; }
        public override System.Span<byte> GetSpan(int sizeHint = 0) { throw null; }
    }
}
namespace System.Buffers
{
    internal static partial class BufferExtensions
    {
        public static System.ArraySegment<byte> GetArray(this System.Memory<byte> buffer) { throw null; }
        public static System.ArraySegment<byte> GetArray(this System.ReadOnlyMemory<byte> memory) { throw null; }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public static System.ReadOnlySpan<byte> ToSpan(this in System.Buffers.ReadOnlySequence<byte> buffer) { throw null; }
        internal static void WriteAsciiNoValidation(this ref System.Buffers.BufferWriter<System.IO.Pipelines.PipeWriter> buffer, string data) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]internal static void WriteNumeric(this ref System.Buffers.BufferWriter<System.IO.Pipelines.PipeWriter> buffer, ulong number) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal ref partial struct BufferWriter<T> where T : System.Buffers.IBufferWriter<byte>
    {
        private T _output;
        private System.Span<byte> _span;
        private int _buffered;
        private long _bytesCommitted;
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public BufferWriter(T output) { throw null; }
        public long BytesCommitted { get { throw null; } }
        public System.Span<byte> Span { get { throw null; } }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public void Advance(int count) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public void Commit() { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public void Ensure(int count = 1) { }
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]public void Write(System.ReadOnlySpan<byte> source) { }
    }
}

namespace System.Diagnostics
{
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Constructor | System.AttributeTargets.Method | System.AttributeTargets.Struct, Inherited=false)]
    internal sealed partial class StackTraceHiddenAttribute : System.Attribute
    {
        public StackTraceHiddenAttribute() { }
    }
}
