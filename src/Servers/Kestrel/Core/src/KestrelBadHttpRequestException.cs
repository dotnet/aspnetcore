// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core
{
    internal class KestrelBadHttpRequestException : IOException
    {
        private KestrelBadHttpRequestException(string message, int statusCode)
            : this(message, statusCode, null)
        { }

        private KestrelBadHttpRequestException(string message, int statusCode, HttpMethod? requiredMethod)
            : base(message)
        {
            StatusCode = statusCode;

            if (requiredMethod.HasValue)
            {
                AllowedHeader = HttpUtilities.MethodToString(requiredMethod.Value);
            }
        }

        public int StatusCode { get; }

        public StringValues AllowedHeader { get; }
        
        [StackTraceHidden]
        internal static void Throw(RequestRejectionReason reason)
        {
            throw GetException(reason);
        }

        [StackTraceHidden]
        internal static void Throw(RequestRejectionReason reason, HttpMethod method)
            => throw GetException(reason, method.ToString().ToUpperInvariant());

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static KestrelBadHttpRequestException GetException(RequestRejectionReason reason)
        {
            KestrelBadHttpRequestException ex;
            switch (reason)
            {
                case RequestRejectionReason.InvalidRequestHeadersNoCRLF:
                    ex = new KestrelBadHttpRequestException(CoreStrings.BadRequest_InvalidRequestHeadersNoCRLF, StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.InvalidRequestLine:
                    ex = new KestrelBadHttpRequestException(CoreStrings.BadRequest_InvalidRequestLine, StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.MalformedRequestInvalidHeaders:
                    ex = new KestrelBadHttpRequestException(CoreStrings.BadRequest_MalformedRequestInvalidHeaders, StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.MultipleContentLengths:
                    ex = new KestrelBadHttpRequestException(CoreStrings.BadRequest_MultipleContentLengths, StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.UnexpectedEndOfRequestContent:
                    ex = new KestrelBadHttpRequestException(CoreStrings.BadRequest_UnexpectedEndOfRequestContent, StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.BadChunkSuffix:
                    ex = new KestrelBadHttpRequestException(CoreStrings.BadRequest_BadChunkSuffix, StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.BadChunkSizeData:
                    ex = new KestrelBadHttpRequestException(CoreStrings.BadRequest_BadChunkSizeData, StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.ChunkedRequestIncomplete:
                    ex = new KestrelBadHttpRequestException(CoreStrings.BadRequest_ChunkedRequestIncomplete, StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.InvalidCharactersInHeaderName:
                    ex = new KestrelBadHttpRequestException(CoreStrings.BadRequest_InvalidCharactersInHeaderName, StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.RequestLineTooLong:
                    ex = new KestrelBadHttpRequestException(CoreStrings.BadRequest_RequestLineTooLong, StatusCodes.Status414UriTooLong);
                    break;
                case RequestRejectionReason.HeadersExceedMaxTotalSize:
                    ex = new KestrelBadHttpRequestException(CoreStrings.BadRequest_HeadersExceedMaxTotalSize, StatusCodes.Status431RequestHeaderFieldsTooLarge);
                    break;
                case RequestRejectionReason.TooManyHeaders:
                    ex = new KestrelBadHttpRequestException(CoreStrings.BadRequest_TooManyHeaders, StatusCodes.Status431RequestHeaderFieldsTooLarge);
                    break;
                case RequestRejectionReason.RequestBodyTooLarge:
                    ex = new KestrelBadHttpRequestException(CoreStrings.BadRequest_RequestBodyTooLarge, StatusCodes.Status413PayloadTooLarge);
                    break;
                case RequestRejectionReason.RequestHeadersTimeout:
                    ex = new KestrelBadHttpRequestException(CoreStrings.BadRequest_RequestHeadersTimeout, StatusCodes.Status408RequestTimeout);
                    break;
                case RequestRejectionReason.RequestBodyTimeout:
                    ex = new KestrelBadHttpRequestException(CoreStrings.BadRequest_RequestBodyTimeout, StatusCodes.Status408RequestTimeout);
                    break;
                case RequestRejectionReason.OptionsMethodRequired:
                    ex = new KestrelBadHttpRequestException(CoreStrings.BadRequest_MethodNotAllowed, StatusCodes.Status405MethodNotAllowed, HttpMethod.Options);
                    break;
                case RequestRejectionReason.ConnectMethodRequired:
                    ex = new KestrelBadHttpRequestException(CoreStrings.BadRequest_MethodNotAllowed, StatusCodes.Status405MethodNotAllowed, HttpMethod.Connect);
                    break;
                case RequestRejectionReason.MissingHostHeader:
                    ex = new KestrelBadHttpRequestException(CoreStrings.BadRequest_MissingHostHeader, StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.MultipleHostHeaders:
                    ex = new KestrelBadHttpRequestException(CoreStrings.BadRequest_MultipleHostHeaders, StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.InvalidHostHeader:
                    ex = new KestrelBadHttpRequestException(CoreStrings.BadRequest_InvalidHostHeader, StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.UpgradeRequestCannotHavePayload:
                    ex = new KestrelBadHttpRequestException(CoreStrings.BadRequest_UpgradeRequestCannotHavePayload, StatusCodes.Status400BadRequest);
                    break;
                default:
                    ex = new KestrelBadHttpRequestException(CoreStrings.BadRequest, StatusCodes.Status400BadRequest);
                    break;
            }
            return ex;
        }

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

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static KestrelBadHttpRequestException GetException(RequestRejectionReason reason, string detail)
        {
            KestrelBadHttpRequestException ex;
            switch (reason)
            {
                case RequestRejectionReason.TlsOverHttpError:
                    ex = new KestrelBadHttpRequestException(CoreStrings.HttpParserTlsOverHttpError, StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.InvalidRequestLine:
                    ex = new KestrelBadHttpRequestException(CoreStrings.FormatBadRequest_InvalidRequestLine_Detail(detail), StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.InvalidRequestTarget:
                    ex = new KestrelBadHttpRequestException(CoreStrings.FormatBadRequest_InvalidRequestTarget_Detail(detail), StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.InvalidRequestHeader:
                    ex = new KestrelBadHttpRequestException(CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(detail), StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.InvalidContentLength:
                    ex = new KestrelBadHttpRequestException(CoreStrings.FormatBadRequest_InvalidContentLength_Detail(detail), StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.UnrecognizedHTTPVersion:
                    ex = new KestrelBadHttpRequestException(CoreStrings.FormatBadRequest_UnrecognizedHTTPVersion(detail), StatusCodes.Status505HttpVersionNotsupported);
                    break;
                case RequestRejectionReason.FinalTransferCodingNotChunked:
                    ex = new KestrelBadHttpRequestException(CoreStrings.FormatBadRequest_FinalTransferCodingNotChunked(detail), StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.LengthRequired:
                    ex = new KestrelBadHttpRequestException(CoreStrings.FormatBadRequest_LengthRequired(detail), StatusCodes.Status411LengthRequired);
                    break;
                case RequestRejectionReason.LengthRequiredHttp10:
                    ex = new KestrelBadHttpRequestException(CoreStrings.FormatBadRequest_LengthRequiredHttp10(detail), StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.InvalidHostHeader:
                    ex = new KestrelBadHttpRequestException(CoreStrings.FormatBadRequest_InvalidHostHeader_Detail(detail), StatusCodes.Status400BadRequest);
                    break;
                default:
                    ex = new KestrelBadHttpRequestException(CoreStrings.BadRequest, StatusCodes.Status400BadRequest);
                    break;
            }
            return ex;
        }
    }
}
