// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal abstract class Http1MessageBody : MessageBody
    {
        protected readonly Http1Connection _context;
        protected bool _completed;

        protected Http1MessageBody(Http1Connection context) : base(context)
        {
            _context = context;
        }

        [StackTraceHidden]
        protected void ThrowUnexpectedEndOfRequestContent()
        {
            // OnInputOrOutputCompleted() is an idempotent method that closes the connection. Sometimes
            // input completion is observed here before the Input.OnWriterCompleted() callback is fired,
            // so we call OnInputOrOutputCompleted() now to prevent a race in our tests where a 400
            // response is written after observing the unexpected end of request content instead of just
            // closing the connection without a response as expected.
            _context.OnInputOrOutputCompleted();

            BadHttpRequestException.Throw(RequestRejectionReason.UnexpectedEndOfRequestContent);
        }

        public abstract bool TryReadInternal(out ReadResult readResult);

        public abstract ValueTask<ReadResult> ReadAsyncInternal(CancellationToken cancellationToken = default);

        protected override Task OnConsumeAsync()
        {
            try
            {
                while (TryReadInternal(out var readResult))
                {
                    AdvanceTo(readResult.Buffer.End);

                    if (readResult.IsCompleted)
                    {
                        return Task.CompletedTask;
                    }
                }
            }
            catch (BadHttpRequestException ex)
            {
                // At this point, the response has already been written, so this won't result in a 4XX response;
                // however, we still need to stop the request processing loop and log.
                _context.SetBadRequestState(ex);
                return Task.CompletedTask;
            }
            catch (InvalidOperationException ex)
            {
                var connectionAbortedException = new ConnectionAbortedException(CoreStrings.ConnectionAbortedByApplication, ex);
                _context.ReportApplicationError(connectionAbortedException);

                // Have to abort the connection because we can't finish draining the request
                _context.StopProcessingNextRequest();
                return Task.CompletedTask;
            }

            return OnConsumeAsyncAwaited();
        }

        protected async Task OnConsumeAsyncAwaited()
        {
            Log.RequestBodyNotEntirelyRead(_context.ConnectionIdFeature, _context.TraceIdentifier);

            _context.TimeoutControl.SetTimeout(Constants.RequestBodyDrainTimeout.Ticks, TimeoutReason.RequestBodyDrain);

            try
            {
                ReadResult result;
                do
                {
                    result = await ReadAsyncInternal();
                    AdvanceTo(result.Buffer.End);
                } while (!result.IsCompleted);
            }
            catch (BadHttpRequestException ex)
            {
                _context.SetBadRequestState(ex);
            }
            catch (OperationCanceledException ex) when (ex is ConnectionAbortedException || ex is TaskCanceledException)
            {
                Log.RequestBodyDrainTimedOut(_context.ConnectionIdFeature, _context.TraceIdentifier);
            }
            catch (InvalidOperationException ex)
            {
                var connectionAbortedException = new ConnectionAbortedException(CoreStrings.ConnectionAbortedByApplication, ex);
                _context.ReportApplicationError(connectionAbortedException);

                // Have to abort the connection because we can't finish draining the request
                _context.StopProcessingNextRequest();
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

                context.OnTrailersComplete(); // No trailers for these.
                return new Http1UpgradeMessageBody(context);
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
                    BadHttpRequestException.Throw(RequestRejectionReason.FinalTransferCodingNotChunked, transferEncoding);
                }

                // TODO may push more into the wrapper rather than just calling into the message body
                // NBD for now.
                return new Http1ChunkedEncodingMessageBody(keepAlive, context);
            }

            if (headers.ContentLength.HasValue)
            {
                var contentLength = headers.ContentLength.Value;

                if (contentLength == 0)
                {
                    return keepAlive ? MessageBody.ZeroContentLengthKeepAlive : MessageBody.ZeroContentLengthClose;
                }

                return new Http1ContentLengthMessageBody(keepAlive, contentLength, context);
            }

            // If we got here, request contains no Content-Length or Transfer-Encoding header.
            // Reject with 411 Length Required.
            if (context.Method == HttpMethod.Post || context.Method == HttpMethod.Put)
            {
                var requestRejectionReason = httpVersion == HttpVersion.Http11 ? RequestRejectionReason.LengthRequired : RequestRejectionReason.LengthRequiredHttp10;
                BadHttpRequestException.Throw(requestRejectionReason, context.Method);
            }

            context.OnTrailersComplete(); // No trailers for these.
            return keepAlive ? MessageBody.ZeroContentLengthKeepAlive : MessageBody.ZeroContentLengthClose;
        }

        protected void ThrowIfCompleted()
        {
            if (_completed)
            {
                throw new InvalidOperationException("Reading is not allowed after the reader was completed.");
            }
        }
    }
}
