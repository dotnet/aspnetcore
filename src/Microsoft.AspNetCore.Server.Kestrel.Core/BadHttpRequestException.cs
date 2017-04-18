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
                    ex = new BadHttpRequestException("Invalid request headers: missing final CRLF in header fields.", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.InvalidRequestLine:
                    ex = new BadHttpRequestException("Invalid request line.", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.MalformedRequestInvalidHeaders:
                    ex = new BadHttpRequestException("Malformed request: invalid headers.", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.MultipleContentLengths:
                    ex = new BadHttpRequestException("Multiple Content-Length headers.", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.UnexpectedEndOfRequestContent:
                    ex = new BadHttpRequestException("Unexpected end of request content.", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.BadChunkSuffix:
                    ex = new BadHttpRequestException("Bad chunk suffix.", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.BadChunkSizeData:
                    ex = new BadHttpRequestException("Bad chunk size data.", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.ChunkedRequestIncomplete:
                    ex = new BadHttpRequestException("Chunked request incomplete.", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.InvalidCharactersInHeaderName:
                    ex = new BadHttpRequestException("Invalid characters in header name.", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.RequestLineTooLong:
                    ex = new BadHttpRequestException("Request line too long.", StatusCodes.Status414UriTooLong);
                    break;
                case RequestRejectionReason.HeadersExceedMaxTotalSize:
                    ex = new BadHttpRequestException("Request headers too long.", StatusCodes.Status431RequestHeaderFieldsTooLarge);
                    break;
                case RequestRejectionReason.TooManyHeaders:
                    ex = new BadHttpRequestException("Request contains too many headers.", StatusCodes.Status431RequestHeaderFieldsTooLarge);
                    break;
                case RequestRejectionReason.RequestTimeout:
                    ex = new BadHttpRequestException("Request timed out.", StatusCodes.Status408RequestTimeout);
                    break;
                case RequestRejectionReason.OptionsMethodRequired:
                    ex = new BadHttpRequestException("Method not allowed.", StatusCodes.Status405MethodNotAllowed, HttpMethod.Options);
                    break;
                case RequestRejectionReason.ConnectMethodRequired:
                    ex = new BadHttpRequestException("Method not allowed.", StatusCodes.Status405MethodNotAllowed, HttpMethod.Connect);
                    break;
                case RequestRejectionReason.MissingHostHeader:
                    ex = new BadHttpRequestException("Request is missing Host header.", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.MultipleHostHeaders:
                    ex = new BadHttpRequestException("Multiple Host headers.", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.InvalidHostHeader:
                    ex = new BadHttpRequestException("Invalid Host header.", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.UpgradeRequestCannotHavePayload:
                    ex = new BadHttpRequestException("Requests with 'Connection: Upgrade' cannot have content in the request body.", StatusCodes.Status400BadRequest);
                    break;
                default:
                    ex = new BadHttpRequestException("Bad request.", StatusCodes.Status400BadRequest);
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
                    ex = new BadHttpRequestException($"Invalid request line: '{detail}'", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.InvalidRequestTarget:
                    ex = new BadHttpRequestException($"Invalid request target: '{detail}'", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.InvalidRequestHeader:
                    ex = new BadHttpRequestException($"Invalid request header: '{detail}'", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.InvalidContentLength:
                    ex = new BadHttpRequestException($"Invalid content length: {detail}", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.UnrecognizedHTTPVersion:
                    ex = new BadHttpRequestException($"Unrecognized HTTP version: '{detail}'", StatusCodes.Status505HttpVersionNotsupported);
                    break;
                case RequestRejectionReason.FinalTransferCodingNotChunked:
                    ex = new BadHttpRequestException($"Final transfer coding is not \"chunked\": \"{detail}\"", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.LengthRequired:
                    ex = new BadHttpRequestException($"{detail} request contains no Content-Length or Transfer-Encoding header", StatusCodes.Status411LengthRequired);
                    break;
                case RequestRejectionReason.LengthRequiredHttp10:
                    ex = new BadHttpRequestException($"{detail} request contains no Content-Length header", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.InvalidHostHeader:
                    ex = new BadHttpRequestException($"Invalid Host header: '{detail}'", StatusCodes.Status400BadRequest);
                    break;
                default:
                    ex = new BadHttpRequestException("Bad request.", StatusCodes.Status400BadRequest);
                    break;
            }
            return ex;
        }
    }
}
