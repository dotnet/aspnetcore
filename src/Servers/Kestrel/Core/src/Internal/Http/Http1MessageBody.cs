// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

using BadHttpRequestException = Microsoft.AspNetCore.Http.BadHttpRequestException;

internal abstract class Http1MessageBody : MessageBody
{
    protected readonly Http1Connection _context;
    private bool _readerCompleted;

    protected Http1MessageBody(Http1Connection context, bool keepAlive) : base(context)
    {
        _context = context;
        RequestKeepAlive = keepAlive;
    }

    public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfReaderCompleted();
        return ReadAsyncInternal(cancellationToken);
    }

    public abstract ValueTask<ReadResult> ReadAsyncInternal(CancellationToken cancellationToken = default);

    public override bool TryRead(out ReadResult readResult)
    {
        ThrowIfReaderCompleted();
        return TryReadInternal(out readResult);
    }

    public abstract bool TryReadInternal(out ReadResult readResult);

    public override void Complete(Exception? exception)
    {
        _readerCompleted = true;
        _context.ReportApplicationError(exception);
    }

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
            Log.RequestBodyDrainBodyReaderInvalidState(_context.ConnectionIdFeature, _context.TraceIdentifier, ex);

            // Have to abort the connection because we can't finish draining the request
            _context.StopProcessingNextRequest(ConnectionEndReason.InvalidBodyReaderState);
            return Task.CompletedTask;
        }

        return OnConsumeAsyncAwaited();
    }

    protected async Task OnConsumeAsyncAwaited()
    {
        Log.RequestBodyNotEntirelyRead(_context.ConnectionIdFeature, _context.TraceIdentifier);

        _context.TimeoutControl.SetTimeout(Constants.RequestBodyDrainTimeout, TimeoutReason.RequestBodyDrain);

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
            Log.RequestBodyDrainBodyReaderInvalidState(_context.ConnectionIdFeature, _context.TraceIdentifier, ex);

            // Have to abort the connection because we can't finish draining the request
            _context.StopProcessingNextRequest(ConnectionEndReason.InvalidBodyReaderState);
        }
        finally
        {
            _context.TimeoutControl.CancelTimeout();
        }
    }

    protected override void OnObservedBytesExceedMaxRequestBodySize(long maxRequestBodySize)
    {
        _context.DisableKeepAlive(ConnectionEndReason.MaxRequestBodySizeExceeded);
        KestrelBadHttpRequestException.Throw(RequestRejectionReason.RequestBodyTooLarge, maxRequestBodySize.ToString(CultureInfo.InvariantCulture));
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
            var connectionOptions = HttpHeaders.ParseConnection(headers);

            upgrade = (connectionOptions & ConnectionOptions.Upgrade) != 0;
            keepAlive = keepAlive || (connectionOptions & ConnectionOptions.KeepAlive) != 0;
            keepAlive = keepAlive && (connectionOptions & ConnectionOptions.Close) == 0;
        }

        // Ignore upgrades if the request has a body. Technically it's possible to support, but we'd have to add a lot
        // more logic to allow reading/draining the normal body before the connection could be fully upgraded.
        // See https://tools.ietf.org/html/rfc7230#section-6.7, https://tools.ietf.org/html/rfc7540#section-3.2
        if (upgrade
            && headers.ContentLength.GetValueOrDefault() == 0
            && headers.HeaderTransferEncoding.Count == 0)
        {
            context.OnTrailersComplete(); // No trailers for these.
            return new Http1UpgradeMessageBody(context, keepAlive);
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
                KestrelBadHttpRequestException.Throw(RequestRejectionReason.FinalTransferCodingNotChunked, transferEncoding);
            }

            // https://datatracker.ietf.org/doc/html/rfc7230#section-3.3.2
            // A sender MUST NOT send a Content-Length header field in any message
            // that contains a Transfer-Encoding header field.
            // https://datatracker.ietf.org/doc/html/rfc7230#section-3.3.3
            // If a message is received with both a Transfer-Encoding and a
            // Content-Length header field, the Transfer-Encoding overrides the
            // Content-Length.  Such a message might indicate an attempt to
            // perform request smuggling (Section 9.5) or response splitting
            // (Section 9.4) and ought to be handled as an error.  A sender MUST
            // remove the received Content-Length field prior to forwarding such
            // a message downstream.
            // We should remove the Content-Length request header in this case, for compatibility
            // reasons, include x-Content-Length so that the original Content-Length is still available.
            if (headers.ContentLength.HasValue)
            {
                IHeaderDictionary headerDictionary = headers;
                headerDictionary.Add("X-Content-Length", headerDictionary[HeaderNames.ContentLength]);
                headers.ContentLength = null;
            }

            // TODO may push more into the wrapper rather than just calling into the message body
            // NBD for now.
            return new Http1ChunkedEncodingMessageBody(context, keepAlive);
        }

        if (headers.ContentLength.HasValue)
        {
            var contentLength = headers.ContentLength.Value;

            if (contentLength == 0)
            {
                return keepAlive ? MessageBody.ZeroContentLengthKeepAlive : MessageBody.ZeroContentLengthClose;
            }

            return new Http1ContentLengthMessageBody(context, contentLength, keepAlive);
        }

        // If we got here, request contains no Content-Length or Transfer-Encoding header.
        // Reject with Length Required for HTTP 1.0.
        if (httpVersion == HttpVersion.Http10 && (context.Method == HttpMethod.Post || context.Method == HttpMethod.Put))
        {
            KestrelMetrics.AddConnectionEndReason(context.MetricsContext, ConnectionEndReason.InvalidRequestHeaders);
            KestrelBadHttpRequestException.Throw(RequestRejectionReason.LengthRequiredHttp10, context.Method);
        }

        context.OnTrailersComplete(); // No trailers for these.
        return keepAlive ? MessageBody.ZeroContentLengthKeepAlive : MessageBody.ZeroContentLengthClose;
    }

    [StackTraceHidden]
    protected void ThrowIfReaderCompleted()
    {
        if (_readerCompleted)
        {
            throw new InvalidOperationException("Reading is not allowed after the reader was completed.");
        }
    }

    [StackTraceHidden]
    protected void ThrowUnexpectedEndOfRequestContent()
    {
        // Set before calling OnInputOrOutputCompleted.
        KestrelMetrics.AddConnectionEndReason(_context.MetricsContext, ConnectionEndReason.UnexpectedEndOfRequestContent);

        // OnInputOrOutputCompleted() is an idempotent method that closes the connection. Sometimes
        // input completion is observed here before the Input.OnWriterCompleted() callback is fired,
        // so we call OnInputOrOutputCompleted() now to prevent a race in our tests where a 400
        // response is written after observing the unexpected end of request content instead of just
        // closing the connection without a response as expected.
        ((IHttpOutputAborter)_context).OnInputOrOutputCompleted();

        KestrelBadHttpRequestException.Throw(RequestRejectionReason.UnexpectedEndOfRequestContent);
    }
}
