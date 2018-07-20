// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Abstractions;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public partial class Http2Stream : HttpProtocol
    {
        private readonly Http2StreamContext _context;
        private readonly Http2OutputProducer _http2Output;
        private readonly Http2StreamOutputFlowControl _outputFlowControl;
        private int _requestAborted;

        public Http2Stream(Http2StreamContext context)
            : base(context)
        {
            _context = context;
            _outputFlowControl = new Http2StreamOutputFlowControl(context.ConnectionOutputFlowControl, context.ClientPeerSettings.InitialWindowSize);
            _http2Output = new Http2OutputProducer(context.StreamId, context.FrameWriter, _outputFlowControl, context.TimeoutControl, context.MemoryPool);
            Output = _http2Output;
        }

        public int StreamId => _context.StreamId;

        public bool RequestBodyStarted { get; private set; }
        public bool EndStreamReceived { get; private set; }

        protected IHttp2StreamLifetimeHandler StreamLifetimeHandler => _context.StreamLifetimeHandler;

        public override bool IsUpgradableRequest => false;

        protected override void OnReset()
        {
            ResetIHttp2StreamIdFeature();
        }

        protected override void OnRequestProcessingEnded()
        {
            StreamLifetimeHandler.OnStreamCompleted(StreamId);
        }

        protected override string CreateRequestId()
            => StringUtilities.ConcatAsHexSuffix(ConnectionId, ':', (uint)StreamId);

        protected override MessageBody CreateMessageBody()
            => Http2MessageBody.For(HttpRequestHeaders, this);

        // Compare to Http1Connection.OnStartLine
        protected override bool TryParseRequest(ReadResult result, out bool endConnection)
        {
            // We don't need any of the parameters because we don't implement BeginRead to actually
            // do the reading from a pipeline, nor do we use endConnection to report connection-level errors.
            endConnection = !TryValidatePseudoHeaders();
            return true;
        }

        private bool TryValidatePseudoHeaders()
        {
            // The initial pseudo header validation takes place in Http2Connection.ValidateHeader and StartStream
            // They make sure the right fields are at least present (except for Connect requests) exactly once.

            _httpVersion = Http.HttpVersion.Http2;

            if (!TryValidateMethod())
            {
                return false;
            }

            if (!TryValidateAuthorityAndHost(out var hostText))
            {
                return false;
            }

            // CONNECT - :scheme and :path must be excluded
            if (Method == HttpMethod.Connect)
            {
                if (!String.IsNullOrEmpty(RequestHeaders[HeaderNames.Scheme]) || !String.IsNullOrEmpty(RequestHeaders[HeaderNames.Path]))
                {
                    ResetAndAbort(new ConnectionAbortedException(CoreStrings.Http2ErrorConnectMustNotSendSchemeOrPath), Http2ErrorCode.PROTOCOL_ERROR);
                    return false;
                }

                RawTarget = hostText;

                return true;
            }

            // :scheme https://tools.ietf.org/html/rfc7540#section-8.1.2.3
            // ":scheme" is not restricted to "http" and "https" schemed URIs.  A
            // proxy or gateway can translate requests for non - HTTP schemes,
            // enabling the use of HTTP to interact with non - HTTP services.

            // - That said, we shouldn't allow arbitrary values or use them to populate Request.Scheme, right?
            // - For now we'll restrict it to http/s and require it match the transport.
            // - We'll need to find some concrete scenarios to warrant unblocking this.
            if (!string.Equals(RequestHeaders[HeaderNames.Scheme], Scheme, StringComparison.OrdinalIgnoreCase))
            {
                ResetAndAbort(new ConnectionAbortedException(
                    CoreStrings.FormatHttp2StreamErrorSchemeMismatch(RequestHeaders[HeaderNames.Scheme], Scheme)), Http2ErrorCode.PROTOCOL_ERROR);
                return false;
            }

            // :path (and query) - Required
            // Must start with / except may be * for OPTIONS
            var path = RequestHeaders[HeaderNames.Path].ToString();
            RawTarget = path;

            // OPTIONS - https://tools.ietf.org/html/rfc7540#section-8.1.2.3
            // This pseudo-header field MUST NOT be empty for "http" or "https"
            // URIs; "http" or "https" URIs that do not contain a path component
            // MUST include a value of '/'.  The exception to this rule is an
            // OPTIONS request for an "http" or "https" URI that does not include
            // a path component; these MUST include a ":path" pseudo-header field
            // with a value of '*'.
            if (Method == HttpMethod.Options && path.Length == 1 && path[0] == '*')
            {
                // * is stored in RawTarget only since HttpRequest expects Path to be empty or start with a /.
                Path = string.Empty;
                QueryString = string.Empty;
                return true;
            }

            var queryIndex = path.IndexOf('?');
            QueryString = queryIndex == -1 ? string.Empty : path.Substring(queryIndex);

            var pathSegment = queryIndex == -1 ? path.AsSpan() : path.AsSpan(0, queryIndex);

            return TryValidatePath(pathSegment);
        }

        private bool TryValidateMethod()
        {
            // :method
            _methodText = RequestHeaders[HeaderNames.Method].ToString();
            Method = HttpUtilities.GetKnownMethod(_methodText);

            if (Method == HttpMethod.None)
            {
                ResetAndAbort(new ConnectionAbortedException(CoreStrings.FormatHttp2ErrorMethodInvalid(_methodText)), Http2ErrorCode.PROTOCOL_ERROR);
                return false;
            }

            if (Method == HttpMethod.Custom)
            {
                if (HttpCharacters.IndexOfInvalidTokenChar(_methodText) >= 0)
                {
                    ResetAndAbort(new ConnectionAbortedException(CoreStrings.FormatHttp2ErrorMethodInvalid(_methodText)), Http2ErrorCode.PROTOCOL_ERROR);
                    return false;
                }
            }

            return true;
        }

        private bool TryValidateAuthorityAndHost(out string hostText)
        {
            // :authority (optional)
            // Prefer this over Host

            var authority = RequestHeaders[HeaderNames.Authority];
            var host = HttpRequestHeaders.HeaderHost;
            if (!StringValues.IsNullOrEmpty(authority))
            {
                // https://tools.ietf.org/html/rfc7540#section-8.1.2.3
                // Clients that generate HTTP/2 requests directly SHOULD use the ":authority"
                // pseudo - header field instead of the Host header field.
                // An intermediary that converts an HTTP/2 request to HTTP/1.1 MUST
                // create a Host header field if one is not present in a request by
                // copying the value of the ":authority" pseudo - header field.

                // We take this one step further, we don't want mismatched :authority
                // and Host headers, replace Host if :authority is defined. The application
                // will operate on the Host header.
                HttpRequestHeaders.HeaderHost = authority;
                host = authority;
            }

            // https://tools.ietf.org/html/rfc7230#section-5.4
            // A server MUST respond with a 400 (Bad Request) status code to any
            // HTTP/1.1 request message that lacks a Host header field and to any
            // request message that contains more than one Host header field or a
            // Host header field with an invalid field-value.
            hostText = host.ToString();
            if (host.Count > 1 || !HttpUtilities.IsHostHeaderValid(hostText))
            {
                // RST replaces 400
                ResetAndAbort(new ConnectionAbortedException(CoreStrings.FormatBadRequest_InvalidHostHeader_Detail(hostText)), Http2ErrorCode.PROTOCOL_ERROR);
                return false;
            }

            return true;
        }

        private bool TryValidatePath(ReadOnlySpan<char> pathSegment)
        {
            // Must start with a leading slash
            if (pathSegment.Length == 0 || pathSegment[0] != '/')
            {
                ResetAndAbort(new ConnectionAbortedException(CoreStrings.FormatHttp2StreamErrorPathInvalid(RawTarget)), Http2ErrorCode.PROTOCOL_ERROR);
                return false;
            }

            var pathEncoded = pathSegment.IndexOf('%') >= 0;

            // Compare with Http1Connection.OnOriginFormTarget

            // URIs are always encoded/escaped to ASCII https://tools.ietf.org/html/rfc3986#page-11
            // Multibyte Internationalized Resource Identifiers (IRIs) are first converted to utf8;
            // then encoded/escaped to ASCII  https://www.ietf.org/rfc/rfc3987.txt "Mapping of IRIs to URIs"

            try
            {
                // The decoder operates only on raw bytes
                var pathBuffer = new byte[pathSegment.Length].AsSpan();
                for (int i = 0; i < pathSegment.Length; i++)
                {
                    var ch = pathSegment[i];
                    // The header parser should already be checking this
                    Debug.Assert(32 < ch && ch < 127);
                    pathBuffer[i] = (byte)ch;
                }

                Path = PathNormalizer.DecodePath(pathBuffer, pathEncoded, RawTarget, QueryString.Length);

                return true;
            }
            catch (InvalidOperationException)
            {
                ResetAndAbort(new ConnectionAbortedException(CoreStrings.FormatHttp2StreamErrorPathInvalid(RawTarget)), Http2ErrorCode.PROTOCOL_ERROR);
                return false;
            }
        }

        public async Task OnDataAsync(ArraySegment<byte> data, bool endStream)
        {
            // TODO: content-length accounting
            // TODO: flow-control

            try
            {
                if (data.Count > 0)
                {
                    RequestBodyPipe.Writer.Write(data);

                    RequestBodyStarted = true;
                    await RequestBodyPipe.Writer.FlushAsync();
                }

                if (endStream)
                {
                    EndStreamReceived = true;
                    RequestBodyPipe.Writer.Complete();
                }
            }
            catch (Exception ex)
            {
                RequestBodyPipe.Writer.Complete(ex);
            }
        }

        public bool TryUpdateOutputWindow(int bytes)
        {
            return _context.FrameWriter.TryUpdateStreamWindow(_outputFlowControl, bytes);
        }

        public override void Abort(ConnectionAbortedException abortReason)
        {
            if (Interlocked.Exchange(ref _requestAborted, 1) != 0)
            {
                return;
            }

            AbortCore(abortReason);
        }

        protected override void ApplicationAbort()
        {
            var abortReason = new ConnectionAbortedException(CoreStrings.ConnectionAbortedByApplication);
            ResetAndAbort(abortReason, Http2ErrorCode.CANCEL);
        }

        private void ResetAndAbort(ConnectionAbortedException abortReason, Http2ErrorCode error)
        {
            if (Interlocked.Exchange(ref _requestAborted, 1) != 0)
            {
                return;
            }

            Log.Http2StreamResetAbort(TraceIdentifier, error, abortReason);

            // Don't block on IO. This never faults.
            _ = _http2Output.WriteRstStreamAsync(error);

            AbortCore(abortReason);
        }

        private void AbortCore(ConnectionAbortedException abortReason)
        {
            base.Abort(abortReason);

            // Unblock the request body.
            RequestBodyPipe.Writer.Complete(new IOException(CoreStrings.Http2StreamAborted, abortReason));
        }
    }
}
