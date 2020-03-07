// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal partial class Http1Connection : HttpProtocol, IRequestProcessor
    {
        private const byte ByteAsterisk = (byte)'*';
        private const byte ByteForwardSlash = (byte)'/';
        private const string Asterisk = "*";
        private const string ForwardSlash = "/";

        private readonly HttpConnectionContext _context;
        private readonly IHttpParser<Http1ParsingHandler> _parser;
        private readonly Http1OutputProducer _http1Output;
        protected readonly long _keepAliveTicks;
        private readonly long _requestHeadersTimeoutTicks;

        private int _requestAborted;
        private volatile bool _requestTimedOut;
        private uint _requestCount;

        private HttpRequestTarget _requestTargetForm = HttpRequestTarget.Unknown;
        private Uri _absoluteRequestTarget;

        // The _parsed fields cache the Path, QueryString, RawTarget, and/or _absoluteRequestTarget
        // from the previous request when DisableStringReuse is false.
        private string _parsedPath = null;
        private string _parsedQueryString = null;
        private string _parsedRawTarget = null;
        private Uri _parsedAbsoluteRequestTarget;

        private int _remainingRequestHeadersBytesAllowed;

        public Http1Connection(HttpConnectionContext context)
        {
            Initialize(context);

            _context = context;
            _parser = ServiceContext.HttpParser;
            _keepAliveTicks = ServerOptions.Limits.KeepAliveTimeout.Ticks;
            _requestHeadersTimeoutTicks = ServerOptions.Limits.RequestHeadersTimeout.Ticks;

            _http1Output = new Http1OutputProducer(
                _context.Transport.Output,
                _context.ConnectionId,
                _context.ConnectionContext,
                _context.ServiceContext.Log,
                _context.TimeoutControl,
                this,
                _context.MemoryPool);

            Input = _context.Transport.Input;
            Output = _http1Output;
            MemoryPool = _context.MemoryPool;
        }

        public PipeReader Input { get; }

        public bool RequestTimedOut => _requestTimedOut;

        public MinDataRate MinResponseDataRate { get; set; }

        public MemoryPool<byte> MemoryPool { get; }

        protected override void OnRequestProcessingEnded()
        {
            TimeoutControl.StartDrainTimeout(MinResponseDataRate, ServerOptions.Limits.MaxResponseBufferSize);

            // Prevent RequestAborted from firing. Free up unneeded feature references.
            Reset();

            _http1Output.Dispose();
        }

        public void OnInputOrOutputCompleted()
        {
            _http1Output.Abort(new ConnectionAbortedException(CoreStrings.ConnectionAbortedByClient));
            AbortRequest();
        }

        /// <summary>
        /// Immediately kill the connection and poison the request body stream with an error.
        /// </summary>
        public void Abort(ConnectionAbortedException abortReason)
        {
            if (Interlocked.Exchange(ref _requestAborted, 1) != 0)
            {
                return;
            }

            _http1Output.Abort(abortReason);

            AbortRequest();

            PoisonRequestBodyStream(abortReason);
        }

        protected override void ApplicationAbort()
        {
            Log.ApplicationAbortedConnection(ConnectionId, TraceIdentifier);
            Abort(new ConnectionAbortedException(CoreStrings.ConnectionAbortedByApplication));
        }

        /// <summary>
        /// Stops the request processing loop between requests.
        /// Called on all active connections when the server wants to initiate a shutdown
        /// and after a keep-alive timeout.
        /// </summary>
        public void StopProcessingNextRequest()
        {
            _keepAlive = false;
            Input.CancelPendingRead();
        }

        public void SendTimeoutResponse()
        {
            _requestTimedOut = true;
            Input.CancelPendingRead();
        }

        public void HandleRequestHeadersTimeout()
            => SendTimeoutResponse();

        public void HandleReadDataRateTimeout()
        {
            Log.RequestBodyMinimumDataRateNotSatisfied(ConnectionId, TraceIdentifier, MinRequestBodyDataRate.BytesPerSecond);
            SendTimeoutResponse();
        }

        public void ParseRequest(in ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined)
        {
            consumed = buffer.Start;
            examined = buffer.End;

            switch (_requestProcessingStatus)
            {
                case RequestProcessingStatus.RequestPending:
                    if (buffer.IsEmpty)
                    {
                        break;
                    }

                    TimeoutControl.ResetTimeout(_requestHeadersTimeoutTicks, TimeoutReason.RequestHeaders);

                    _requestProcessingStatus = RequestProcessingStatus.ParsingRequestLine;
                    goto case RequestProcessingStatus.ParsingRequestLine;
                case RequestProcessingStatus.ParsingRequestLine:
                    if (TakeStartLine(buffer, out consumed, out examined))
                    {
                        TrimAndParseHeaders(buffer, ref consumed, out examined);
                        return;
                    }
                    else
                    {
                        break;
                    }
                case RequestProcessingStatus.ParsingHeaders:
                    if (TakeMessageHeaders(buffer, trailers: false, out consumed, out examined))
                    {
                        _requestProcessingStatus = RequestProcessingStatus.AppStarted;
                    }
                    break;
            }

            void TrimAndParseHeaders(in ReadOnlySequence<byte> buffer, ref SequencePosition consumed, out SequencePosition examined)
            {
                var trimmedBuffer = buffer.Slice(consumed, buffer.End);
                _requestProcessingStatus = RequestProcessingStatus.ParsingHeaders;

                if (TakeMessageHeaders(trimmedBuffer, trailers: false, out consumed, out examined))
                {
                    _requestProcessingStatus = RequestProcessingStatus.AppStarted;
                }
            }
        }

        public bool TakeStartLine(in ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined)
        {
            // Make sure the buffer is limited
            if (buffer.Length >= ServerOptions.Limits.MaxRequestLineSize)
            {
                // Input oversize, cap amount checked
                return TrimAndTakeStartLine(buffer, out consumed, out examined);
            }

            return _parser.ParseRequestLine(new Http1ParsingHandler(this), buffer, out consumed, out examined);

            bool TrimAndTakeStartLine(in ReadOnlySequence<byte> buffer, out SequencePosition consumed, out SequencePosition examined)
            {
                var trimmedBuffer = buffer.Slice(buffer.Start, ServerOptions.Limits.MaxRequestLineSize);

                if (!_parser.ParseRequestLine(new Http1ParsingHandler(this), trimmedBuffer, out consumed, out examined))
                {
                    // We read the maximum allowed but didn't complete the start line.
                    BadHttpRequestException.Throw(RequestRejectionReason.RequestLineTooLong);
                }

                return true;
            }
        }

        public bool TakeMessageHeaders(in ReadOnlySequence<byte> buffer, bool trailers, out SequencePosition consumed, out SequencePosition examined)
        {
            // Make sure the buffer is limited
            if (buffer.Length > _remainingRequestHeadersBytesAllowed)
            {
                // Input oversize, cap amount checked
                return TrimAndTakeMessageHeaders(buffer, trailers, out consumed, out examined);
            }

            var reader = new SequenceReader<byte>(buffer);
            var result = false;
            try
            {
                result = _parser.ParseHeaders(new Http1ParsingHandler(this, trailers), ref reader);

                if (result)
                {
                    TimeoutControl.CancelTimeout();
                }

                return result;
            }
            finally
            {
                consumed = reader.Position;
                _remainingRequestHeadersBytesAllowed -= (int)reader.Consumed;

                if (result)
                {
                    examined = consumed;
                }
                else
                {
                    examined = buffer.End;
                }
            }

            bool TrimAndTakeMessageHeaders(in ReadOnlySequence<byte> buffer, bool trailers, out SequencePosition consumed, out SequencePosition examined)
            {
                var trimmedBuffer = buffer.Slice(buffer.Start, _remainingRequestHeadersBytesAllowed);

                var reader = new SequenceReader<byte>(trimmedBuffer);
                var result = false;
                try
                {
                    result = _parser.ParseHeaders(new Http1ParsingHandler(this, trailers), ref reader);

                    if (!result)
                    {
                        // We read the maximum allowed but didn't complete the headers.
                        BadHttpRequestException.Throw(RequestRejectionReason.HeadersExceedMaxTotalSize);
                    }

                    TimeoutControl.CancelTimeout();

                    return result;
                }
                finally
                {
                    consumed = reader.Position;
                    _remainingRequestHeadersBytesAllowed -= (int)reader.Consumed;

                    if (result)
                    {
                        examined = consumed;
                    }
                    else
                    {
                        examined = trimmedBuffer.End;
                    }
                }
            }
        }

        public void OnStartLine(HttpMethod method, HttpVersion version, Span<byte> target, Span<byte> path, Span<byte> query, Span<byte> customMethod, bool pathEncoded)
        {
            Debug.Assert(target.Length != 0, "Request target must be non-zero length");

            var ch = target[0];
            if (ch == ByteForwardSlash)
            {
                // origin-form.
                // The most common form of request-target.
                // https://tools.ietf.org/html/rfc7230#section-5.3.1
                OnOriginFormTarget(pathEncoded, target, path, query);
            }
            else if (ch == ByteAsterisk && target.Length == 1)
            {
                OnAsteriskFormTarget(method);
            }
            else if (target.GetKnownHttpScheme(out _))
            {
                OnAbsoluteFormTarget(target, query);
            }
            else
            {
                // Assume anything else is considered authority form.
                // FYI: this should be an edge case. This should only happen when
                // a client mistakenly thinks this server is a proxy server.
                OnAuthorityFormTarget(method, target);
            }

            Method = method;
            if (method == HttpMethod.Custom)
            {
                _methodText = customMethod.GetAsciiStringNonNullCharacters();
            }

            _httpVersion = version;

            Debug.Assert(RawTarget != null, "RawTarget was not set");
            Debug.Assert(((IHttpRequestFeature)this).Method != null, "Method was not set");
            Debug.Assert(Path != null, "Path was not set");
            Debug.Assert(QueryString != null, "QueryString was not set");
            Debug.Assert(HttpVersion != null, "HttpVersion was not set");
        }

        // Compare with Http2Stream.TryValidatePseudoHeaders
        private void OnOriginFormTarget(bool pathEncoded, Span<byte> target, Span<byte> path, Span<byte> query)
        {
            Debug.Assert(target[0] == ByteForwardSlash, "Should only be called when path starts with /");

            _requestTargetForm = HttpRequestTarget.OriginForm;

            if (target.Length == 1)
            {
                // If target.Length == 1 it can only be a forward slash (e.g. home page)
                // and we know RawTarget and Path are the same and QueryString is Empty
                RawTarget = ForwardSlash;
                Path = ForwardSlash;
                QueryString = string.Empty;
                // Clear parsedData as we won't check it if we come via this path again,
                // an setting to null is fast as it doesn't need to use a GC write barrier.
                _parsedRawTarget = _parsedPath = _parsedQueryString = null;
                _parsedAbsoluteRequestTarget = null;
                return;
            }

            // URIs are always encoded/escaped to ASCII https://tools.ietf.org/html/rfc3986#page-11
            // Multibyte Internationalized Resource Identifiers (IRIs) are first converted to utf8;
            // then encoded/escaped to ASCII  https://www.ietf.org/rfc/rfc3987.txt "Mapping of IRIs to URIs"

            try
            {
                var disableStringReuse = ServerOptions.DisableStringReuse;
                // Read raw target before mutating memory.
                var previousValue = _parsedRawTarget;
                if (disableStringReuse ||
                    previousValue == null || previousValue.Length != target.Length ||
                    !StringUtilities.BytesOrdinalEqualsStringAndAscii(previousValue, target))
                {
                    // The previous string does not match what the bytes would convert to,
                    // so we will need to generate a new string.
                    RawTarget = _parsedRawTarget = target.GetAsciiStringNonNullCharacters();

                    previousValue = _parsedQueryString;
                    if (disableStringReuse ||
                        previousValue == null || previousValue.Length != query.Length ||
                        !StringUtilities.BytesOrdinalEqualsStringAndAscii(previousValue, query))
                    {
                        // The previous string does not match what the bytes would convert to,
                        // so we will need to generate a new string.
                        QueryString = _parsedQueryString = query.GetAsciiStringNonNullCharacters();
                    }
                    else
                    {
                        // Same as previous
                        QueryString = _parsedQueryString;
                    }

                    if (path.Length == 1)
                    {
                        // If path.Length == 1 it can only be a forward slash (e.g. home page)
                        Path = _parsedPath = ForwardSlash;
                    }
                    else
                    {
                        Path = _parsedPath = PathNormalizer.DecodePath(path, pathEncoded, RawTarget, query.Length);
                    }
                }
                else
                {
                    // As RawTarget is the same we can reuse the previous parsed values.
                    RawTarget = _parsedRawTarget;
                    Path = _parsedPath;
                    QueryString = _parsedQueryString;
                }

                // Clear parsedData for absolute target as we won't check it if we come via this path again,
                // an setting to null is fast as it doesn't need to use a GC write barrier.
                _parsedAbsoluteRequestTarget = null;
            }
            catch (InvalidOperationException)
            {
                ThrowRequestTargetRejected(target);
            }
        }

        private void OnAuthorityFormTarget(HttpMethod method, Span<byte> target)
        {
            _requestTargetForm = HttpRequestTarget.AuthorityForm;

            // This is not complete validation. It is just a quick scan for invalid characters
            // but doesn't check that the target fully matches the URI spec.
            if (HttpCharacters.ContainsInvalidAuthorityChar(target))
            {
                ThrowRequestTargetRejected(target);
            }

            // The authority-form of request-target is only used for CONNECT
            // requests (https://tools.ietf.org/html/rfc7231#section-4.3.6).
            if (method != HttpMethod.Connect)
            {
                BadHttpRequestException.Throw(RequestRejectionReason.ConnectMethodRequired);
            }

            // When making a CONNECT request to establish a tunnel through one or
            // more proxies, a client MUST send only the target URI's authority
            // component (excluding any userinfo and its "@" delimiter) as the
            // request-target.For example,
            //
            //  CONNECT www.example.com:80 HTTP/1.1
            //
            // Allowed characters in the 'host + port' section of authority.
            // See https://tools.ietf.org/html/rfc3986#section-3.2

            var previousValue = _parsedRawTarget;
            if (ServerOptions.DisableStringReuse ||
                previousValue == null || previousValue.Length != target.Length ||
                !StringUtilities.BytesOrdinalEqualsStringAndAscii(previousValue, target))
            {
                // The previous string does not match what the bytes would convert to,
                // so we will need to generate a new string.
                RawTarget = _parsedRawTarget = target.GetAsciiStringNonNullCharacters();
            }
            else
            {
                // Reuse previous value
                RawTarget = _parsedRawTarget;
            }

            Path = string.Empty;
            QueryString = string.Empty;
            // Clear parsedData for path, queryString and absolute target as we won't check it if we come via this path again,
            // an setting to null is fast as it doesn't need to use a GC write barrier.
            _parsedPath = _parsedQueryString = null;
            _parsedAbsoluteRequestTarget = null;
        }

        private void OnAsteriskFormTarget(HttpMethod method)
        {
            _requestTargetForm = HttpRequestTarget.AsteriskForm;

            // The asterisk-form of request-target is only used for a server-wide
            // OPTIONS request (https://tools.ietf.org/html/rfc7231#section-4.3.7).
            if (method != HttpMethod.Options)
            {
                BadHttpRequestException.Throw(RequestRejectionReason.OptionsMethodRequired);
            }

            RawTarget = Asterisk;
            Path = string.Empty;
            QueryString = string.Empty;
            // Clear parsedData as we won't check it if we come via this path again,
            // an setting to null is fast as it doesn't need to use a GC write barrier.
            _parsedRawTarget = _parsedPath = _parsedQueryString = null;
            _parsedAbsoluteRequestTarget = null;
        }

        private void OnAbsoluteFormTarget(Span<byte> target, Span<byte> query)
        {
            _requestTargetForm = HttpRequestTarget.AbsoluteForm;

            // absolute-form
            // https://tools.ietf.org/html/rfc7230#section-5.3.2

            // This code should be the edge-case.

            // From the spec:
            //    a server MUST accept the absolute-form in requests, even though
            //    HTTP/1.1 clients will only send them in requests to proxies.

            var disableStringReuse = ServerOptions.DisableStringReuse;
            var previousValue = _parsedRawTarget;
            if (disableStringReuse ||
                previousValue == null || previousValue.Length != target.Length ||
                !StringUtilities.BytesOrdinalEqualsStringAndAscii(previousValue, target))
            {
                // The previous string does not match what the bytes would convert to,
                // so we will need to generate a new string.
                RawTarget = _parsedRawTarget = target.GetAsciiStringNonNullCharacters();

                // Validation of absolute URIs is slow, but clients
                // should not be sending this form anyways, so perf optimization
                // not high priority

                if (!Uri.TryCreate(RawTarget, UriKind.Absolute, out var uri))
                {
                    ThrowRequestTargetRejected(target);
                }

                _absoluteRequestTarget = _parsedAbsoluteRequestTarget = uri;
                Path = _parsedPath = uri.LocalPath;
                // don't use uri.Query because we need the unescaped version
                previousValue = _parsedQueryString;
                if (disableStringReuse ||
                    previousValue == null || previousValue.Length != query.Length ||
                    !StringUtilities.BytesOrdinalEqualsStringAndAscii(previousValue, query))
                {
                    // The previous string does not match what the bytes would convert to,
                    // so we will need to generate a new string.
                    QueryString = _parsedQueryString = query.GetAsciiStringNonNullCharacters();
                }
                else
                {
                    QueryString = _parsedQueryString;
                }
            }
            else
            {
                // As RawTarget is the same we can reuse the previous values.
                RawTarget = _parsedRawTarget;
                Path = _parsedPath;
                QueryString = _parsedQueryString;
                _absoluteRequestTarget = _parsedAbsoluteRequestTarget;
            }
        }

        internal void EnsureHostHeaderExists()
        {
            // https://tools.ietf.org/html/rfc7230#section-5.4
            // A server MUST respond with a 400 (Bad Request) status code to any
            // HTTP/1.1 request message that lacks a Host header field and to any
            // request message that contains more than one Host header field or a
            // Host header field with an invalid field-value.

            var hostCount = HttpRequestHeaders.HostCount;
            var hostText = HttpRequestHeaders.HeaderHost.ToString();
            if (hostCount <= 0)
            {
                if (_httpVersion == Http.HttpVersion.Http10)
                {
                    return;
                }
                BadHttpRequestException.Throw(RequestRejectionReason.MissingHostHeader);
            }
            else if (hostCount > 1)
            {
                BadHttpRequestException.Throw(RequestRejectionReason.MultipleHostHeaders);
            }
            else if (_requestTargetForm != HttpRequestTarget.OriginForm)
            {
                // Tail call
                ValidateNonOriginHostHeader(hostText);
            }
            else if (!HttpUtilities.IsHostHeaderValid(hostText))
            {
                BadHttpRequestException.Throw(RequestRejectionReason.InvalidHostHeader, hostText);
            }
        }

        private void ValidateNonOriginHostHeader(string hostText)
        {
            if (_requestTargetForm == HttpRequestTarget.AuthorityForm)
            {
                if (hostText != RawTarget)
                {
                    BadHttpRequestException.Throw(RequestRejectionReason.InvalidHostHeader, hostText);
                }
            }
            else if (_requestTargetForm == HttpRequestTarget.AbsoluteForm)
            {
                // If the target URI includes an authority component, then a
                // client MUST send a field - value for Host that is identical to that
                // authority component, excluding any userinfo subcomponent and its "@"
                // delimiter.

                // System.Uri doesn't not tell us if the port was in the original string or not.
                // When IsDefaultPort = true, we will allow Host: with or without the default port
                if (hostText != _absoluteRequestTarget.Authority)
                {
                    if (!_absoluteRequestTarget.IsDefaultPort
                        || hostText != _absoluteRequestTarget.Authority + ":" + _absoluteRequestTarget.Port.ToString(CultureInfo.InvariantCulture))
                    {
                        BadHttpRequestException.Throw(RequestRejectionReason.InvalidHostHeader, hostText);
                    }
                }
            }

            if (!HttpUtilities.IsHostHeaderValid(hostText))
            {
                BadHttpRequestException.Throw(RequestRejectionReason.InvalidHostHeader, hostText);
            }
        }

        protected override void OnReset()
        {
            ResetHttp1Features();

            _requestTimedOut = false;
            _requestTargetForm = HttpRequestTarget.Unknown;
            _absoluteRequestTarget = null;
            _remainingRequestHeadersBytesAllowed = ServerOptions.Limits.MaxRequestHeadersTotalSize + 2;
            _requestCount++;

            MinResponseDataRate = ServerOptions.Limits.MinResponseDataRate;
        }

        protected override void OnRequestProcessingEnding()
        {
        }

        protected override string CreateRequestId()
            => StringUtilities.ConcatAsHexSuffix(ConnectionId, ':', _requestCount);

        protected override MessageBody CreateMessageBody()
            => Http1MessageBody.For(_httpVersion, HttpRequestHeaders, this);

        protected override void BeginRequestProcessing()
        {
            // Reset the features and timeout.
            Reset();
            TimeoutControl.SetTimeout(_keepAliveTicks, TimeoutReason.KeepAlive);
        }

        protected override bool BeginRead(out ValueTask<ReadResult> awaitable)
        {
            awaitable = Input.ReadAsync();
            return true;
        }

        protected override bool TryParseRequest(ReadResult result, out bool endConnection)
        {
            var examined = result.Buffer.End;
            var consumed = result.Buffer.End;

            try
            {
                ParseRequest(result.Buffer, out consumed, out examined);
            }
            catch (InvalidOperationException)
            {
                if (_requestProcessingStatus == RequestProcessingStatus.ParsingHeaders)
                {
                    BadHttpRequestException.Throw(RequestRejectionReason.MalformedRequestInvalidHeaders);
                }
                throw;
            }
            finally
            {
                Input.AdvanceTo(consumed, examined);
            }

            if (result.IsCompleted)
            {
                switch (_requestProcessingStatus)
                {
                    case RequestProcessingStatus.RequestPending:
                        endConnection = true;
                        return true;
                    case RequestProcessingStatus.ParsingRequestLine:
                        BadHttpRequestException.Throw(RequestRejectionReason.InvalidRequestLine);
                        break;
                    case RequestProcessingStatus.ParsingHeaders:
                        BadHttpRequestException.Throw(RequestRejectionReason.MalformedRequestInvalidHeaders);
                        break;
                }
            }
            else if (!_keepAlive && _requestProcessingStatus == RequestProcessingStatus.RequestPending)
            {
                // Stop the request processing loop if the server is shutting down or there was a keep-alive timeout
                // and there is no ongoing request.
                endConnection = true;
                return true;
            }
            else if (RequestTimedOut)
            {
                // In this case, there is an ongoing request but the start line/header parsing has timed out, so send
                // a 408 response.
                BadHttpRequestException.Throw(RequestRejectionReason.RequestHeadersTimeout);
            }

            endConnection = false;
            if (_requestProcessingStatus == RequestProcessingStatus.AppStarted)
            {
                EnsureHostHeaderExists();
                return true;
            }
            else
            {
                return false;
            }
        }

        void IRequestProcessor.Tick(DateTimeOffset now) { }
    }
}
