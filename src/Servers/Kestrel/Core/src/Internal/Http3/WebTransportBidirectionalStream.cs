// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
internal class WebTransportBidirectionalStream<TContext> : Http3BidirectionalStream<TContext> where TContext : notnull
{
    public WebTransportBidirectionalStream(IHttpApplication<TContext> application, Http3StreamContext context)
        : base(application, context) { }

    protected override Task ProcessHttp3Stream<TContext>(IHttpApplication<TContext> application, in ReadOnlySequence<byte> payload, bool isCompleted)
    {
        if (((long)_incomingFrame.Type) != ((long)Http3StreamType.WebTransportBidirectional))
        {
            throw new Http3ConnectionErrorException($"Stream type must be {((long)Http3StreamType.WebTransportBidirectional):X} on bidirectional webtransport streams", Http3ErrorCode.ConnectError);
        }

        return ProcessDataAsync(payload);
    }

    private Task ProcessDataAsync(in ReadOnlySequence<byte> payload)
    {
        //// DATA frame before headers is invalid.
        //// https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-4.1
        //if (_requestHeaderParsingState == RequestHeaderParsingState.Ready)
        //{
        //    throw new Http3ConnectionErrorException(CoreStrings.Http3StreamErrorDataReceivedBeforeHeaders, Http3ErrorCode.UnexpectedFrame);
        //}

        //// DATA frame after trailing headers is invalid.
        //// https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-4.1
        //if (_requestHeaderParsingState == RequestHeaderParsingState.Trailers)
        //{
        //    var message = CoreStrings.FormatHttp3StreamErrorFrameReceivedAfterTrailers(Http3Formatting.ToFormattedType(Http3FrameType.Data));
        //    throw new Http3ConnectionErrorException(message, Http3ErrorCode.UnexpectedFrame);
        //}

        if (InputRemaining.HasValue)
        {
            // https://tools.ietf.org/html/rfc7540#section-8.1.2.6
            if (payload.Length > InputRemaining.Value)
            {
                throw new Http3StreamErrorException(CoreStrings.Http3StreamErrorMoreDataThanLength, Http3ErrorCode.ProtocolError);
            }

            InputRemaining -= payload.Length;
        }

        foreach (var segment in payload)
        {
            RequestBodyPipe.Writer.Write(segment.Span);
        }

        return RequestBodyPipe.Writer.FlushAsync().GetAsTask();
    }
}
