// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    public sealed class BadHttpRequestException : IOException
    {
        private BadHttpRequestException(string message, int statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }

        internal int StatusCode { get; }

        internal static BadHttpRequestException GetException(RequestRejectionReason reason)
        {
            BadHttpRequestException ex;
            switch (reason)
            {
                case RequestRejectionReason.HeadersCorruptedInvalidHeaderSequence:
                    ex = new BadHttpRequestException("Headers corrupted, invalid header sequence.", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.NoColonCharacterFoundInHeaderLine:
                    ex = new BadHttpRequestException("No ':' character found in header line.", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.WhitespaceIsNotAllowedInHeaderName:
                    ex = new BadHttpRequestException("Whitespace is not allowed in header name.", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.HeaderValueMustNotContainCR:
                    ex = new BadHttpRequestException("Header value must not contain CR characters.", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.HeaderValueLineFoldingNotSupported:
                    ex = new BadHttpRequestException("Header value line folding not supported.", StatusCodes.Status400BadRequest);
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
                case RequestRejectionReason.PathContainsNullCharacters:
                    ex = new BadHttpRequestException("The path contains null characters.", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.InvalidCharactersInHeaderName:
                    ex = new BadHttpRequestException("Invalid characters in header name.", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.NonAsciiOrNullCharactersInInputString:
                    ex = new BadHttpRequestException("The input string contains non-ASCII or null characters.", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.RequestLineTooLong:
                    ex = new BadHttpRequestException("Request line too long.", StatusCodes.Status414UriTooLong);
                    break;
                case RequestRejectionReason.HeadersExceedMaxTotalSize:
                    ex = new BadHttpRequestException("Request headers too long.", StatusCodes.Status431RequestHeaderFieldsTooLarge);
                    break;
                case RequestRejectionReason.MissingCRInHeaderLine:
                    ex = new BadHttpRequestException("No CR character found in header line.", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.TooManyHeaders:
                    ex = new BadHttpRequestException("Request contains too many headers.", StatusCodes.Status431RequestHeaderFieldsTooLarge);
                    break;
                case RequestRejectionReason.RequestTimeout:
                    ex = new BadHttpRequestException("Request timed out.", StatusCodes.Status408RequestTimeout);
                    break;
                default:
                    ex = new BadHttpRequestException("Bad request.", StatusCodes.Status400BadRequest);
                    break;
            }
            return ex;
        }

        internal static BadHttpRequestException GetException(RequestRejectionReason reason, string value)
        {
            BadHttpRequestException ex;
            switch (reason)
            {
                case RequestRejectionReason.InvalidRequestLine:
                    ex = new BadHttpRequestException($"Invalid request line: {value}", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.InvalidContentLength:
                    ex = new BadHttpRequestException($"Invalid content length: {value}", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.UnrecognizedHTTPVersion:
                    ex = new BadHttpRequestException($"Unrecognized HTTP version: {value}", StatusCodes.Status505HttpVersionNotsupported);
                    break;
                case RequestRejectionReason.FinalTransferCodingNotChunked:
                    ex = new BadHttpRequestException($"Final transfer coding is not \"chunked\": \"{value}\"", StatusCodes.Status400BadRequest);
                    break;
                case RequestRejectionReason.LengthRequired:
                    ex = new BadHttpRequestException($"{value} request contains no Content-Length or Transfer-Encoding header", StatusCodes.Status411LengthRequired);
                    break;
                case RequestRejectionReason.LengthRequiredHttp10:
                    ex = new BadHttpRequestException($"{value} request contains no Content-Length header", StatusCodes.Status400BadRequest);
                    break;
                default:
                    ex = new BadHttpRequestException("Bad request.", StatusCodes.Status400BadRequest);
                    break;
            }
            return ex;
        }
    }
}
