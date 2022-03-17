// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.Kestrel.Core;

internal static class KestrelBadHttpRequestException
{
    [StackTraceHidden]
    internal static void Throw(RequestRejectionReason reason)
    {
        throw GetException(reason);
    }

    [StackTraceHidden]
    internal static void Throw(RequestRejectionReason reason, HttpMethod method)
        => throw GetException(reason, method.ToString().ToUpperInvariant());

    [MethodImpl(MethodImplOptions.NoInlining)]
#pragma warning disable CS0618 // Type or member is obsolete
    internal static BadHttpRequestException GetException(RequestRejectionReason reason)
    {
        BadHttpRequestException ex;
        switch (reason)
        {
            case RequestRejectionReason.InvalidRequestHeadersNoCRLF:
                ex = new BadHttpRequestException(CoreStrings.BadRequest_InvalidRequestHeadersNoCRLF, StatusCodes.Status400BadRequest, reason);
                break;
            case RequestRejectionReason.InvalidRequestLine:
                ex = new BadHttpRequestException(CoreStrings.BadRequest_InvalidRequestLine, StatusCodes.Status400BadRequest, reason);
                break;
            case RequestRejectionReason.MalformedRequestInvalidHeaders:
                ex = new BadHttpRequestException(CoreStrings.BadRequest_MalformedRequestInvalidHeaders, StatusCodes.Status400BadRequest, reason);
                break;
            case RequestRejectionReason.MultipleContentLengths:
                ex = new BadHttpRequestException(CoreStrings.BadRequest_MultipleContentLengths, StatusCodes.Status400BadRequest, reason);
                break;
            case RequestRejectionReason.UnexpectedEndOfRequestContent:
                ex = new BadHttpRequestException(CoreStrings.BadRequest_UnexpectedEndOfRequestContent, StatusCodes.Status400BadRequest, reason);
                break;
            case RequestRejectionReason.BadChunkSuffix:
                ex = new BadHttpRequestException(CoreStrings.BadRequest_BadChunkSuffix, StatusCodes.Status400BadRequest, reason);
                break;
            case RequestRejectionReason.BadChunkSizeData:
                ex = new BadHttpRequestException(CoreStrings.BadRequest_BadChunkSizeData, StatusCodes.Status400BadRequest, reason);
                break;
            case RequestRejectionReason.ChunkedRequestIncomplete:
                ex = new BadHttpRequestException(CoreStrings.BadRequest_ChunkedRequestIncomplete, StatusCodes.Status400BadRequest, reason);
                break;
            case RequestRejectionReason.InvalidCharactersInHeaderName:
                ex = new BadHttpRequestException(CoreStrings.BadRequest_InvalidCharactersInHeaderName, StatusCodes.Status400BadRequest, reason);
                break;
            case RequestRejectionReason.RequestLineTooLong:
                ex = new BadHttpRequestException(CoreStrings.BadRequest_RequestLineTooLong, StatusCodes.Status414UriTooLong, reason);
                break;
            case RequestRejectionReason.HeadersExceedMaxTotalSize:
                ex = new BadHttpRequestException(CoreStrings.BadRequest_HeadersExceedMaxTotalSize, StatusCodes.Status431RequestHeaderFieldsTooLarge, reason);
                break;
            case RequestRejectionReason.TooManyHeaders:
                ex = new BadHttpRequestException(CoreStrings.BadRequest_TooManyHeaders, StatusCodes.Status431RequestHeaderFieldsTooLarge, reason);
                break;
            case RequestRejectionReason.RequestHeadersTimeout:
                ex = new BadHttpRequestException(CoreStrings.BadRequest_RequestHeadersTimeout, StatusCodes.Status408RequestTimeout, reason);
                break;
            case RequestRejectionReason.RequestBodyTimeout:
                ex = new BadHttpRequestException(CoreStrings.BadRequest_RequestBodyTimeout, StatusCodes.Status408RequestTimeout, reason);
                break;
            case RequestRejectionReason.OptionsMethodRequired:
                ex = new BadHttpRequestException(CoreStrings.BadRequest_MethodNotAllowed, StatusCodes.Status405MethodNotAllowed, reason, HttpMethod.Options);
                break;
            case RequestRejectionReason.ConnectMethodRequired:
                ex = new BadHttpRequestException(CoreStrings.BadRequest_MethodNotAllowed, StatusCodes.Status405MethodNotAllowed, reason, HttpMethod.Connect);
                break;
            case RequestRejectionReason.MissingHostHeader:
                ex = new BadHttpRequestException(CoreStrings.BadRequest_MissingHostHeader, StatusCodes.Status400BadRequest, reason);
                break;
            case RequestRejectionReason.MultipleHostHeaders:
                ex = new BadHttpRequestException(CoreStrings.BadRequest_MultipleHostHeaders, StatusCodes.Status400BadRequest, reason);
                break;
            case RequestRejectionReason.InvalidHostHeader:
                ex = new BadHttpRequestException(CoreStrings.BadRequest_InvalidHostHeader, StatusCodes.Status400BadRequest, reason);
                break;
            default:
                ex = new BadHttpRequestException(CoreStrings.BadRequest, StatusCodes.Status400BadRequest, reason);
                break;
        }
        return ex;
    }
#pragma warning restore CS0618 // Type or member is obsolete

    [StackTraceHidden]
    internal static void Throw(RequestRejectionReason reason, string detail)
    {
        throw GetException(reason, detail);
    }

    [StackTraceHidden]
    internal static void Throw(RequestRejectionReason reason, StringValues detail)
    {
        throw GetException(reason, detail.ToString());
    }

#pragma warning disable CS0618 // Type or member is obsolete
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static BadHttpRequestException GetException(RequestRejectionReason reason, string detail)
    {
        BadHttpRequestException ex;
        switch (reason)
        {
            case RequestRejectionReason.TlsOverHttpError:
                ex = new BadHttpRequestException(CoreStrings.HttpParserTlsOverHttpError, StatusCodes.Status400BadRequest, reason);
                break;
            case RequestRejectionReason.InvalidRequestLine:
                ex = new BadHttpRequestException(CoreStrings.FormatBadRequest_InvalidRequestLine_Detail(detail), StatusCodes.Status400BadRequest, reason);
                break;
            case RequestRejectionReason.InvalidRequestTarget:
                ex = new BadHttpRequestException(CoreStrings.FormatBadRequest_InvalidRequestTarget_Detail(detail), StatusCodes.Status400BadRequest, reason);
                break;
            case RequestRejectionReason.InvalidRequestHeader:
                ex = new BadHttpRequestException(CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(detail), StatusCodes.Status400BadRequest, reason);
                break;
            case RequestRejectionReason.InvalidContentLength:
                ex = new BadHttpRequestException(CoreStrings.FormatBadRequest_InvalidContentLength_Detail(detail), StatusCodes.Status400BadRequest, reason);
                break;
            case RequestRejectionReason.UnrecognizedHTTPVersion:
                ex = new BadHttpRequestException(CoreStrings.FormatBadRequest_UnrecognizedHTTPVersion(detail), StatusCodes.Status505HttpVersionNotsupported, reason);
                break;
            case RequestRejectionReason.FinalTransferCodingNotChunked:
                ex = new BadHttpRequestException(CoreStrings.FormatBadRequest_FinalTransferCodingNotChunked(detail), StatusCodes.Status400BadRequest, reason);
                break;
            case RequestRejectionReason.LengthRequiredHttp10:
                ex = new BadHttpRequestException(CoreStrings.FormatBadRequest_LengthRequiredHttp10(detail), StatusCodes.Status400BadRequest, reason);
                break;
            case RequestRejectionReason.InvalidHostHeader:
                ex = new BadHttpRequestException(CoreStrings.FormatBadRequest_InvalidHostHeader_Detail(detail), StatusCodes.Status400BadRequest, reason);
                break;
            case RequestRejectionReason.RequestBodyTooLarge:
                ex = new BadHttpRequestException(CoreStrings.FormatBadRequest_RequestBodyTooLarge(detail), StatusCodes.Status413PayloadTooLarge, reason);
                break;
            default:
                ex = new BadHttpRequestException(CoreStrings.BadRequest, StatusCodes.Status400BadRequest, reason);
                break;
        }
        return ex;
    }
#pragma warning restore CS0618 // Type or member is obsolete
}
