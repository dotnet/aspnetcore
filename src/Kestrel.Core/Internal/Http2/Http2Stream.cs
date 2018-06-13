// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public partial class Http2Stream : HttpProtocol
    {
        private readonly Http2StreamContext _context;

        public Http2Stream(Http2StreamContext context)
            : base(context)
        {
            _context = context;

            Output = new Http2OutputProducer(StreamId, _context.FrameWriter);
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

        protected override bool TryParseRequest(ReadResult result, out bool endConnection)
        {
            // We don't need any of the parameters because we don't implement BeginRead to actually
            // do the reading from a pipeline, nor do we use endConnection to report connection-level errors.

            _httpVersion = Http.HttpVersion.Http2;
            var methodText = RequestHeaders[HeaderNames.Method];
            Method = HttpUtilities.GetKnownMethod(methodText);
            _methodText = methodText;
            if (!string.Equals(RequestHeaders[HeaderNames.Scheme], Scheme, StringComparison.OrdinalIgnoreCase))
            {
                BadHttpRequestException.Throw(RequestRejectionReason.InvalidRequestLine);
            }

            var path = RequestHeaders[HeaderNames.Path].ToString();
            var queryIndex = path.IndexOf('?');

            Path = queryIndex == -1 ? path : path.Substring(0, queryIndex);
            QueryString = queryIndex == -1 ? string.Empty : path.Substring(queryIndex);
            RawTarget = path;

            // https://tools.ietf.org/html/rfc7230#section-5.4
            // A server MUST respond with a 400 (Bad Request) status code to any
            // HTTP/1.1 request message that lacks a Host header field and to any
            // request message that contains more than one Host header field or a
            // Host header field with an invalid field-value.

            var authority = RequestHeaders[HeaderNames.Authority];
            var host = HttpRequestHeaders.HeaderHost;
            if (authority.Count > 0)
            {
                // https://tools.ietf.org/html/rfc7540#section-8.1.2.3
                // An intermediary that converts an HTTP/2 request to HTTP/1.1 MUST
                // create a Host header field if one is not present in a request by
                // copying the value of the ":authority" pseudo - header field.
                //
                // We take this one step further, we don't want mismatched :authority
                // and Host headers, replace Host if :authority is defined.
                HttpRequestHeaders.HeaderHost = authority;
                host = authority;
            }

            // TODO: OPTIONS * requests?
            // To ensure that the HTTP / 1.1 request line can be reproduced
            // accurately, this pseudo - header field MUST be omitted when
            // translating from an HTTP/ 1.1 request that has a request target in
            // origin or asterisk form(see[RFC7230], Section 5.3).
            // https://tools.ietf.org/html/rfc7230#section-5.3

            if (host.Count <= 0)
            {
                BadHttpRequestException.Throw(RequestRejectionReason.MissingHostHeader);
            }
            else if (host.Count > 1)
            {
                BadHttpRequestException.Throw(RequestRejectionReason.MultipleHostHeaders);
            }

            var hostText = host.ToString();
            HttpUtilities.ValidateHostHeader(hostText);

            endConnection = false;
            return true;
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

        // TODO: The HTTP/2 tests expect the request and response streams to be aborted with
        // non-ConnectionAbortedExceptions. The abortReasons can include things like
        // Http2ConnectionErrorException which don't derive from IOException or
        // OperationCanceledException. This is probably not a good idea.
        public void Http2Abort(Exception abortReason)
        {
            _streams?.Abort(abortReason);

            OnInputOrOutputCompleted();
        }
    }
}
