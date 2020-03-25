// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    internal abstract partial class Http2Stream : HttpProtocol, IThreadPoolWorkItem, IDisposable
    {
        private Http2StreamContext _context;
        private Http2OutputProducer _http2Output;
        private StreamInputFlowControl _inputFlowControl;
        private StreamOutputFlowControl _outputFlowControl;
        private Http2MessageBody _messageBody;

        private bool _decrementCalled;

        public Pipe RequestBodyPipe { get; set; }

        internal long DrainExpirationTicks { get; set; }

        private StreamCompletionFlags _completionState;
        private readonly object _completionLock = new object();

        public void Initialize(Http2StreamContext context)
        {
            base.Initialize(context);

            CanReuse = false;
            _decrementCalled = false;
            _completionState = StreamCompletionFlags.None;
            InputRemaining = null;
            RequestBodyStarted = false;
            DrainExpirationTicks = 0;

            _context = context;

            // First time the stream is used we need to create flow control, producer and pipes.
            // When a stream is reused these types will be reset and reused.
            if (_inputFlowControl == null)
            {
                _inputFlowControl = new StreamInputFlowControl(
                    this,
                    context.FrameWriter,
                    context.ConnectionInputFlowControl,
                    context.ServerPeerSettings.InitialWindowSize,
                    context.ServerPeerSettings.InitialWindowSize / 2);

                _outputFlowControl = new StreamOutputFlowControl(
                    context.ConnectionOutputFlowControl,
                    context.ClientPeerSettings.InitialWindowSize);

                _http2Output = new Http2OutputProducer(this, context, _outputFlowControl);

                RequestBodyPipe = CreateRequestBodyPipe();

                Output = _http2Output;
            }
            else
            {
                _inputFlowControl.Reset();
                _outputFlowControl.Reset(context.ClientPeerSettings.InitialWindowSize);
                _http2Output.StreamReset();
                RequestBodyPipe.Reset();
            }
        }

        public void InitializeWithExistingContext(int streamId)
        {
            _context.StreamId = streamId;

            Initialize(_context);
        }

        public int StreamId => _context.StreamId;

        public long? InputRemaining { get; internal set; }

        public bool RequestBodyStarted { get; private set; }
        public bool EndStreamReceived => (_completionState & StreamCompletionFlags.EndStreamReceived) == StreamCompletionFlags.EndStreamReceived;
        private bool IsAborted => (_completionState & StreamCompletionFlags.Aborted) == StreamCompletionFlags.Aborted;
        internal bool RstStreamReceived => (_completionState & StreamCompletionFlags.RstStreamReceived) == StreamCompletionFlags.RstStreamReceived;

        public bool ReceivedEmptyRequestBody
        {
            get
            {
                lock (_completionLock)
                {
                    return EndStreamReceived && !RequestBodyStarted;
                }
            }
        }

        public bool CanReuse { get; private set; }

        protected override void OnReset()
        {
            _keepAlive = true;
            _connectionAborted = false;

            ResetHttp2Features();
        }

        protected override void OnRequestProcessingEnded()
        {
            CompleteStream(errored: false);
        }

        public void CompleteStream(bool errored)
        {
            try
            {
                // https://tools.ietf.org/html/rfc7540#section-8.1
                // If the app finished without reading the request body tell the client not to finish sending it.
                if (!EndStreamReceived && !RstStreamReceived)
                {
                    if (!errored)
                    {
                        Log.RequestBodyNotEntirelyRead(ConnectionIdFeature, TraceIdentifier);
                    }

                    var (oldState, newState) = ApplyCompletionFlag(StreamCompletionFlags.Aborted);
                    if (oldState != newState)
                    {
                        Debug.Assert(_decrementCalled);

                        // If there was an error starting the stream then we don't want to write RST_STREAM here.
                        // The connection will handle writing RST_STREAM with the correct error code.
                        if (!errored)
                        {
                            // Don't block on IO. This never faults.
                            _ = _http2Output.WriteRstStreamAsync(Http2ErrorCode.NO_ERROR);
                        }
                        RequestBodyPipe.Writer.Complete();
                    }
                }

                _http2Output.Complete();

                RequestBodyPipe.Reader.Complete();

                // The app can no longer read any more of the request body, so return any bytes that weren't read to the
                // connection's flow-control window.
                _inputFlowControl.Abort();

                // We only want to reuse a stream that has completely finished writing.
                // This is to prevent the situation where Http2OutputProducer.ProcessDataWrites
                // is still running in the background.
                CanReuse = !_keepAlive && HasResponseCompleted;
            }
            finally
            {
                _context.StreamLifetimeHandler.OnStreamCompleted(this);
            }
        }

        protected override string CreateRequestId()
            => StringUtilities.ConcatAsHexSuffix(ConnectionId, ':', (uint)StreamId);

        protected override MessageBody CreateMessageBody()
        {
            if (ReceivedEmptyRequestBody)
            {
                return MessageBody.ZeroContentLengthClose;
            }

            if (_messageBody != null)
            {
                _messageBody.Reset();
            }
            else
            {
                _messageBody = new Http2MessageBody(this);
            }

            return _messageBody;
        }

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
                if (!String.IsNullOrEmpty(HttpRequestHeaders.HeaderScheme) || !String.IsNullOrEmpty(HttpRequestHeaders.HeaderPath))
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
            if (!string.Equals(HttpRequestHeaders.HeaderScheme, Scheme, StringComparison.OrdinalIgnoreCase))
            {
                ResetAndAbort(new ConnectionAbortedException(
                    CoreStrings.FormatHttp2StreamErrorSchemeMismatch(HttpRequestHeaders.HeaderScheme, Scheme)), Http2ErrorCode.PROTOCOL_ERROR);
                return false;
            }

            // :path (and query) - Required
            // Must start with / except may be * for OPTIONS
            var path = HttpRequestHeaders.HeaderPath.ToString();
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

            // Approximate MaxRequestLineSize by totaling the required pseudo header field lengths.
            var requestLineLength = _methodText.Length + Scheme.Length + hostText.Length + path.Length;
            if (requestLineLength > ServerOptions.Limits.MaxRequestLineSize)
            {
                ResetAndAbort(new ConnectionAbortedException(CoreStrings.BadRequest_RequestLineTooLong), Http2ErrorCode.PROTOCOL_ERROR);
                return false;
            }

            var queryIndex = path.IndexOf('?');
            QueryString = queryIndex == -1 ? string.Empty : path.Substring(queryIndex);

            var pathSegment = queryIndex == -1 ? path.AsSpan() : path.AsSpan(0, queryIndex);

            return TryValidatePath(pathSegment);
        }

        private bool TryValidateMethod()
        {
            // :method
            _methodText = HttpRequestHeaders.HeaderMethod.ToString();
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

            var authority = HttpRequestHeaders.HeaderAuthority;
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

            var pathEncoded = pathSegment.Contains('%');

            // Compare with Http1Connection.OnOriginFormTarget

            // URIs are always encoded/escaped to ASCII https://tools.ietf.org/html/rfc3986#page-11
            // Multibyte Internationalized Resource Identifiers (IRIs) are first converted to utf8;
            // then encoded/escaped to ASCII  https://www.ietf.org/rfc/rfc3987.txt "Mapping of IRIs to URIs"

            try
            {
                const int MaxPathBufferStackAllocSize = 256;

                // The decoder operates only on raw bytes
                Span<byte> pathBuffer = pathSegment.Length <= MaxPathBufferStackAllocSize
                    // A constant size plus slice generates better code
                    // https://github.com/dotnet/aspnetcore/pull/19273#discussion_r383159929
                    ? stackalloc byte[MaxPathBufferStackAllocSize].Slice(0, pathSegment.Length)
                    // TODO - Consider pool here for less than 4096
                    // https://github.com/dotnet/aspnetcore/pull/19273#discussion_r383604184
                    : new byte[pathSegment.Length];

                for (var i = 0; i < pathSegment.Length; i++)
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

        public Task OnDataAsync(Http2Frame dataFrame, in ReadOnlySequence<byte> payload)
        {
            // Since padding isn't buffered, immediately count padding bytes as read for flow control purposes.
            if (dataFrame.DataHasPadding)
            {
                // Add 1 byte for the padding length prefix.
                OnDataRead(dataFrame.DataPadLength + 1);
            }

            var dataPayload = payload.Slice(0, dataFrame.DataPayloadLength); // minus padding
            var endStream = dataFrame.DataEndStream;

            if (dataPayload.Length > 0)
            {
                lock (_completionLock)
                {
                    RequestBodyStarted = true;

                    if (endStream)
                    {
                        // No need to send any more window updates for this stream now that we've received all the data.
                        // Call before flushing the request body pipe, because that might induce a window update.
                        _inputFlowControl.StopWindowUpdates();
                    }

                    _inputFlowControl.Advance((int)dataPayload.Length);

                    // This check happens after flow control so that when we throw and abort, the byte count is returned to the connection
                    // level accounting.
                    if (InputRemaining.HasValue)
                    {
                        // https://tools.ietf.org/html/rfc7540#section-8.1.2.6
                        if (dataPayload.Length > InputRemaining.Value)
                        {
                            throw new Http2StreamErrorException(StreamId, CoreStrings.Http2StreamErrorMoreDataThanLength, Http2ErrorCode.PROTOCOL_ERROR);
                        }

                        InputRemaining -= dataPayload.Length;
                    }

                    // Ignore data frames for aborted streams, but only after counting them for purposes of connection level flow control.
                    if (!IsAborted)
                    {
                        foreach (var segment in dataPayload)
                        {
                            RequestBodyPipe.Writer.Write(segment.Span);
                        }

                        // If the stream is completed go ahead and call RequestBodyPipe.Writer.Complete().
                        // Data will still be available to the reader.
                        if (!endStream)
                        {
                            var flushTask = RequestBodyPipe.Writer.FlushAsync();
                            // It shouldn't be possible for the RequestBodyPipe to fill up an return an incomplete task if
                            // _inputFlowControl.Advance() didn't throw.
                            Debug.Assert(flushTask.IsCompleted);
                        }
                    }
                }
            }

            if (endStream)
            {
                OnEndStreamReceived();
            }

            return Task.CompletedTask;
        }

        public void OnEndStreamReceived()
        {
            ApplyCompletionFlag(StreamCompletionFlags.EndStreamReceived);

            if (InputRemaining.HasValue)
            {
                // https://tools.ietf.org/html/rfc7540#section-8.1.2.6
                if (InputRemaining.Value != 0)
                {
                    throw new Http2StreamErrorException(StreamId, CoreStrings.Http2StreamErrorLessDataThanLength, Http2ErrorCode.PROTOCOL_ERROR);
                }
            }

            OnTrailersComplete();
            RequestBodyPipe.Writer.Complete();

            _inputFlowControl.StopWindowUpdates();
        }

        public void OnDataRead(int bytesRead)
        {
            _inputFlowControl.UpdateWindows(bytesRead);
        }

        public bool TryUpdateOutputWindow(int bytes)
        {
            return _context.FrameWriter.TryUpdateStreamWindow(_outputFlowControl, bytes);
        }

        public void AbortRstStreamReceived()
        {
            // Client sent a reset stream frame, decrement total count.
            DecrementActiveClientStreamCount();

            ApplyCompletionFlag(StreamCompletionFlags.RstStreamReceived);
            Abort(new IOException(CoreStrings.Http2StreamResetByClient));
        }

        public void Abort(IOException abortReason)
        {
            var (oldState, newState) = ApplyCompletionFlag(StreamCompletionFlags.Aborted);

            if (oldState == newState)
            {
                return;
            }

            AbortCore(abortReason);
        }

        protected override void OnErrorAfterResponseStarted()
        {
            // We can no longer change the response, send a Reset instead.
            var abortReason = new ConnectionAbortedException(CoreStrings.Http2StreamErrorAfterHeaders);
            ResetAndAbort(abortReason, Http2ErrorCode.INTERNAL_ERROR);
        }

        protected override void ApplicationAbort()
        {
            var abortReason = new ConnectionAbortedException(CoreStrings.ConnectionAbortedByApplication);
            ResetAndAbort(abortReason, Http2ErrorCode.INTERNAL_ERROR);
        }

        internal void ResetAndAbort(ConnectionAbortedException abortReason, Http2ErrorCode error)
        {
            // Future incoming frames will drain for a default grace period to avoid destabilizing the connection.
            var (oldState, newState) = ApplyCompletionFlag(StreamCompletionFlags.Aborted);

            if (oldState == newState)
            {
                return;
            }

            Log.Http2StreamResetAbort(TraceIdentifier, error, abortReason);

            DecrementActiveClientStreamCount();
            // Don't block on IO. This never faults.
            _ = _http2Output.WriteRstStreamAsync(error);

            AbortCore(abortReason);
        }

        private void AbortCore(Exception abortReason)
        {
            // Call _http2Output.Stop() prior to poisoning the request body stream or pipe to
            // ensure that an app that completes early due to the abort doesn't result in header frames being sent.
            _http2Output.Stop();

            AbortRequest();

            // Unblock the request body.
            PoisonRequestBodyStream(abortReason);
            RequestBodyPipe.Writer.Complete(abortReason);

            _inputFlowControl.Abort();
        }

        public void DecrementActiveClientStreamCount()
        {
            // Decrement can be called twice, via calling CompleteAsync and then Abort on the HttpContext.
            // Only decrement once total.
            lock (_completionLock)
            {
                if (_decrementCalled)
                {
                    return;
                }

                _decrementCalled = true;
            }
          
            _context.StreamLifetimeHandler.DecrementActiveClientStreamCount();
        }

        private Pipe CreateRequestBodyPipe()
            => new Pipe(new PipeOptions
            (
                pool: _context.MemoryPool,
                readerScheduler: ServiceContext.Scheduler,
                writerScheduler: PipeScheduler.Inline,
                // Never pause within the window range. Flow control will prevent more data from being added.
                // See the assert in OnDataAsync.
                pauseWriterThreshold: _context.ServerPeerSettings.InitialWindowSize + 1,
                resumeWriterThreshold: _context.ServerPeerSettings.InitialWindowSize + 1,
                useSynchronizationContext: false,
                minimumSegmentSize: _context.MemoryPool.GetMinimumSegmentSize()
            ));

        private (StreamCompletionFlags OldState, StreamCompletionFlags NewState) ApplyCompletionFlag(StreamCompletionFlags completionState)
        {
            lock (_completionLock)
            {
                var oldCompletionState = _completionState;
                _completionState |= completionState;

                return (oldCompletionState, _completionState);
            }
        }

        /// <summary>
        /// Used to kick off the request processing loop by derived classes.
        /// </summary>
        public abstract void Execute();

        public void Dispose()
        {
            _http2Output.Dispose();
        }

        [Flags]
        private enum StreamCompletionFlags
        {
            None = 0,
            RstStreamReceived = 1,
            EndStreamReceived = 2,
            Aborted = 4,
        }
    }
}
