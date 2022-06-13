// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;
internal class WebTransportUnidirectionalStream<TContext> : Http3UnidirectionalStream<TContext> where TContext : notnull
{
    public WebTransportUnidirectionalStream(IHttpApplication<TContext> application, Http3StreamContext context)
        : base(application, context) { }

    protected override Task ProcessDataFrameAsync(in ReadOnlySequence<byte> payload)
    {
        ////DATA frame before headers is invalid.
        ////https://quicwg.org/base-drafts/draft-ietf-quic-http.html#section-4.1
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

        //if (InputRemaining.HasValue)
        //{
        //    // https://tools.ietf.org/html/rfc7540#section-8.1.2.6
        //    if (payload.Length > InputRemaining.Value)
        //    {
        //        throw new Http3StreamErrorException(CoreStrings.Http3StreamErrorMoreDataThanLength, Http3ErrorCode.ProtocolError);
        //    }

        //    InputRemaining -= payload.Length;
        //}

        foreach (var segment in payload)
        {
            RequestBodyPipe.Writer.Write(segment.Span);
        }

        return RequestBodyPipe.Writer.FlushAsync().GetAsTask();
    }
}
