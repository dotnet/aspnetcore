// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.Kestrel.Core
{
    public sealed class BadHttpRequestException : IOException
    {
        private BadHttpRequestException(string message, int statusCode)
            : this(message, statusCode, null)
        { }

        private BadHttpRequestException(string message, int statusCode, HttpMethod? requiredMethod)
            : base(message)
        {
            StatusCode = statusCode;

            if (requiredMethod.HasValue)
            {
                AllowedHeader = HttpUtilities.MethodToString(requiredMethod.Value);
            }
        }

        internal int StatusCode { get; }

        internal StringValues AllowedHeader { get; }

        internal static BadHttpRequestException GetException(RequestRejectionReason reason)
        {
            BadHttpRequestException ex;
            switch (reason)
            {
                case RequestRejectionReason.InvalidRequestHeadersNoCRLF:
                    ex = new BadHttpRequestException(CoreStrings.BadRequest_InvalidRequestHeadersNoCRLF, StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.InvalidRequestLine:
                    ex = new BadHttpRequestException(CoreStrings.BadRequest_InvalidRequestLine, StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.MalformedRequestInvalidHeaders:
                    ex = new BadHttpRequestException(CoreStrings.BadRequest_MalformedRequestInvalidHeaders, StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.MultipleContentLengths:
                    ex = new BadHttpRequestException(CoreStrings.BadRequest_MultipleContentLengths, StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.UnexpectedEndOfRequestContent:
                    ex = new BadHttpRequestException(CoreStrings.BadRequest_UnexpectedEndOfRequestContent, StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.BadChunkSuffix:
                    ex = new BadHttpRequestException(CoreStrings.BadRequest_BadChunkSuffix, StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.BadChunkSizeData:
                    ex = new BadHttpRequestException(CoreStrings.BadRequest_BadChunkSizeData, StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.ChunkedRequestIncomplete:
                    ex = new BadHttpRequestException(CoreStrings.BadRequest_ChunkedRequestIncomplete, StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.InvalidCharactersInHeaderName:
                    ex = new BadHttpRequestException(CoreStrings.BadRequest_InvalidCharactersInHeaderName, StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.RequestLineTooLong:
                    ex = new BadHttpRequestException(CoreStrings.BadRequest_RequestLineTooLong, StatusCodes.Status414UriTooLong);
                    break;
                case RequestRejectionReason.HeadersExceedMaxTotalSize:
                    ex = new BadHttpRequestException(CoreStrings.BadRequest_HeadersExceedMaxTotalSize, StatusCodes.Status431RequestHeaderFieldsTooLarge);
                    break;
                case RequestRejectionReason.TooManyHeaders:
                    ex = new BadHttpRequestException(CoreStrings.BadRequest_TooManyHeaders, StatusCodes.Status431RequestHeaderFieldsTooLarge);
                    break;
                case RequestRejectionReason.RequestBodyTooLarge:
                    ex = new BadHttpRequestException(CoreStrings.BadRequest_RequestBodyTooLarge, StatusCodes.Status413PayloadTooLarge);
                    break;
                case RequestRejectionReason.RequestTimeout:
                    ex = new BadHttpRequestException(CoreStrings.BadRequest_RequestTimeout, StatusCodes.Status408RequestTimeout);
                    break;
                case RequestRejectionReason.OptionsMethodRequired:
                    ex = new BadHttpRequestException(CoreStrings.BadRequest_MethodNotAllowed, StatusCodes.Status405MethodNotAllowed, HttpMethod.Options);
                    break;
                case RequestRejectionReason.ConnectMethodRequired:
                    ex = new BadHttpRequestException(CoreStrings.BadRequest_MethodNotAllowed, StatusCodes.Status405MethodNotAllowed, HttpMethod.Connect);
                    break;
                case RequestRejectionReason.MissingHostHeader:
                    ex = new BadHttpRequestException(CoreStrings.BadRequest_MissingHostHeader, StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.MultipleHostHeaders:
                    ex = new BadHttpRequestException(CoreStrings.BadRequest_MultipleHostHeaders, StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.InvalidHostHeader:
                    ex = new BadHttpRequestException(CoreStrings.BadRequest_InvalidHostHeader, StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.UpgradeRequestCannotHavePayload:
                    ex = new BadHttpRequestException(CoreStrings.BadRequest_UpgradeRequestCannotHavePayload, StatusCodes.Status400BadRequest);
                    break;
                default:
                    ex = new BadHttpRequestException(CoreStrings.BadRequest, StatusCodes.Status400BadRequest);
                    break;
            }
            return ex;
        }

        internal static BadHttpRequestException GetException(RequestRejectionReason reason, string detail)
        {
            BadHttpRequestException ex;
            switch (reason)
            {
                case RequestRejectionReason.InvalidRequestLine:
                    ex = new BadHttpRequestException(CoreStrings.FormatBadRequest_InvalidRequestLine_Detail(detail), StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.InvalidRequestTarget:
                    ex = new BadHttpRequestException(CoreStrings.FormatBadRequest_InvalidRequestTarget_Detail(detail), StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.InvalidRequestHeader:
                    ex = new BadHttpRequestException(CoreStrings.FormatBadRequest_InvalidRequestHeader_Detail(detail), StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.InvalidContentLength:
                    ex = new BadHttpRequestException(CoreStrings.FormatBadRequest_InvalidContentLength_Detail(detail), StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.UnrecognizedHTTPVersion:
                    ex = new BadHttpRequestException(CoreStrings.FormatBadRequest_UnrecognizedHTTPVersion(detail), StatusCodes.Status505HttpVersionNotsupported);
                    break;
                case RequestRejectionReason.FinalTransferCodingNotChunked:
                    ex = new BadHttpRequestException(CoreStrings.FormatBadRequest_FinalTransferCodingNotChunked(detail), StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.LengthRequired:
                    ex = new BadHttpRequestException(CoreStrings.FormatBadRequest_LengthRequired(detail), StatusCodes.Status411LengthRequired);
                    break;
                case RequestRejectionReason.LengthRequiredHttp10:
                    ex = new BadHttpRequestException(CoreStrings.FormatBadRequest_LengthRequiredHttp10(detail), StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.InvalidHostHeader:
                    ex = new BadHttpRequestException(CoreStrings.FormatBadRequest_InvalidHostHeader_Detail(detail), StatusCodes.Status400BadRequest);
                    break;
                default:
                    ex = new BadHttpRequestException(CoreStrings.BadRequest, StatusCodes.Status400BadRequest);
                    break;
            }
            return ex;
        }
    }
}
