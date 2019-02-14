// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public abstract class Http1MessageBody : MessageBody
    {
        protected readonly Http1Connection _context;

        protected Http1MessageBody(Http1Connection context)
            : base(context, context.MinRequestBodyDataRate)
        {
            _context = context;
        }

        protected override Task OnConsumeAsync()
        {
            try
            {
                if (_context.RequestBodyPipeReader.TryRead(out var readResult))
                {
                    _context.RequestBodyPipeReader.AdvanceTo(readResult.Buffer.End);

                    if (readResult.IsCompleted)
                    {
                        return Task.CompletedTask;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // TryRead can throw OperationCanceledException https://github.com/dotnet/corefx/issues/32029
                // because of buggy logic, this works around that for now
            }
            catch (BadHttpRequestException ex)
            {
                // At this point, the response has already been written, so this won't result in a 4XX response;
                // however, we still need to stop the request processing loop and log.
                _context.SetBadRequestState(ex);
                return Task.CompletedTask;
            }

            return OnConsumeAsyncAwaited();
        }

        private async Task OnConsumeAsyncAwaited()
        {
            Log.RequestBodyNotEntirelyRead(_context.ConnectionIdFeature, _context.TraceIdentifier);

            _context.TimeoutControl.SetTimeout(Constants.RequestBodyDrainTimeout.Ticks, TimeoutReason.RequestBodyDrain);

            try
            {
                ReadResult result;
                do
                {
                    result = await _context.RequestBodyPipeReader.ReadAsync();
                    _context.RequestBodyPipeReader.AdvanceTo(result.Buffer.End);
                } while (!result.IsCompleted);
            }
            catch (BadHttpRequestException ex)
            {
                _context.SetBadRequestState(ex);
            }
            catch (ConnectionAbortedException)
            {
                Log.RequestBodyDrainTimedOut(_context.ConnectionIdFeature, _context.TraceIdentifier);
            }
            finally
            {
                _context.TimeoutControl.CancelTimeout();
            }
        }

        public static MessageBody For(
            HttpVersion httpVersion,
            HttpRequestHeaders headers,
            Http1Connection context)
        {
            // see also http://tools.ietf.org/html/rfc2616#section-4.4
            var keepAlive = httpVersion != HttpVersion.Http10;

            var upgrade = false;
            if (headers.HasConnection)
            {
                var connectionOptions = HttpHeaders.ParseConnection(headers.HeaderConnection);

                upgrade = (connectionOptions & ConnectionOptions.Upgrade) == ConnectionOptions.Upgrade;
                keepAlive = (connectionOptions & ConnectionOptions.KeepAlive) == ConnectionOptions.KeepAlive;
            }

            if (upgrade)
            {
                if (headers.HeaderTransferEncoding.Count > 0 || (headers.ContentLength.HasValue && headers.ContentLength.Value != 0))
                {
                    BadHttpRequestException.Throw(RequestRejectionReason.UpgradeRequestCannotHavePayload);
                }

                context.RequestBodyPipeReader = new HttpRequestPipeReader();
                return new ForUpgrade(context);
            }

            if (headers.HasTransferEncoding)
            {
                var transferEncoding = headers.HeaderTransferEncoding;
                var transferCoding = HttpHeaders.GetFinalTransferCoding(transferEncoding);

                // https://tools.ietf.org/html/rfc7230#section-3.3.3
                // If a Transfer-Encoding header field
                // is present in a request and the chunked transfer coding is not
                // the final encoding, the message body length cannot be determined
                // reliably; the server MUST respond with the 400 (Bad Request)
                // status code and then close the connection.
                if (transferCoding != TransferCoding.Chunked)
                {
                    BadHttpRequestException.Throw(RequestRejectionReason.FinalTransferCodingNotChunked, in transferEncoding);
                }

                context.RequestBodyPipeReader = new HttpRequestPipeReader();
                // TODO may push more into the wrapper rather than just calling into the message body
                // NBD for now.
                return new ForChunkedEncoding(keepAlive, context);
            }

            if (headers.ContentLength.HasValue)
            {
                var contentLength = headers.ContentLength.Value;

                if (contentLength == 0)
                {
                    return keepAlive ? MessageBody.ZeroContentLengthKeepAlive : MessageBody.ZeroContentLengthClose;
                }

                context.RequestBodyPipeReader = new HttpRequestPipeReader();
                return new ForContentLength(keepAlive, contentLength, context);
            }

            // If we got here, request contains no Content-Length or Transfer-Encoding header.
            // Reject with 411 Length Required.
            if (context.Method == HttpMethod.Post || context.Method == HttpMethod.Put)
            {
                var requestRejectionReason = httpVersion == HttpVersion.Http11 ? RequestRejectionReason.LengthRequired : RequestRejectionReason.LengthRequiredHttp10;
                BadHttpRequestException.Throw(requestRejectionReason, context.Method);
            }

            return keepAlive ? MessageBody.ZeroContentLengthKeepAlive : MessageBody.ZeroContentLengthClose;
        }
    }
}
