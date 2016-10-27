// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
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
                    ex = new BadHttpRequestException("Headers corrupted, invalid header sequence.", 400);
                    break;
                case RequestRejectionReason.HeaderLineMustNotStartWithWhitespace:
                    ex = new BadHttpRequestException("Header line must not start with whitespace.", 400);
                    break;
                case RequestRejectionReason.NoColonCharacterFoundInHeaderLine:
                    ex = new BadHttpRequestException("No ':' character found in header line.", 400);
                    break;
                case RequestRejectionReason.WhitespaceIsNotAllowedInHeaderName:
                    ex = new BadHttpRequestException("Whitespace is not allowed in header name.", 400);
                    break;
                case RequestRejectionReason.HeaderValueMustNotContainCR:
                    ex = new BadHttpRequestException("Header value must not contain CR characters.", 400);
                    break;
                case RequestRejectionReason.HeaderValueLineFoldingNotSupported:
                    ex = new BadHttpRequestException("Header value line folding not supported.", 400);
                    break;
                case RequestRejectionReason.MalformedRequestInvalidHeaders:
                    ex = new BadHttpRequestException("Malformed request: invalid headers.", 400);
                    break;
                case RequestRejectionReason.UnexpectedEndOfRequestContent:
                    ex = new BadHttpRequestException("Unexpected end of request content.", 400);
                    break;
                case RequestRejectionReason.BadChunkSuffix:
                    ex = new BadHttpRequestException("Bad chunk suffix.", 400);
                    break;
                case RequestRejectionReason.BadChunkSizeData:
                    ex = new BadHttpRequestException("Bad chunk size data.", 400);
                    break;
                case RequestRejectionReason.ChunkedRequestIncomplete:
                    ex = new BadHttpRequestException("Chunked request incomplete.", 400);
                    break;
                case RequestRejectionReason.PathContainsNullCharacters:
                    ex = new BadHttpRequestException("The path contains null characters.", 400);
                    break;
                case RequestRejectionReason.InvalidCharactersInHeaderName:
                    ex = new BadHttpRequestException("Invalid characters in header name.", 400);
                    break;
                case RequestRejectionReason.NonAsciiOrNullCharactersInInputString:
                    ex = new BadHttpRequestException("The input string contains non-ASCII or null characters.", 400);
                    break;
                case RequestRejectionReason.RequestLineTooLong:
                    ex = new BadHttpRequestException("Request line too long.", 414);
                    break;
                case RequestRejectionReason.HeadersExceedMaxTotalSize:
                    ex = new BadHttpRequestException("Request headers too long.", 431);
                    break;
                case RequestRejectionReason.MissingCRInHeaderLine:
                    ex = new BadHttpRequestException("No CR character found in header line.", 400);
                    break;
                case RequestRejectionReason.TooManyHeaders:
                    ex = new BadHttpRequestException("Request contains too many headers.", 431);
                    break;
                case RequestRejectionReason.RequestTimeout:
                    ex = new BadHttpRequestException("Request timed out.", 408);
                    break;
                default:
                    ex = new BadHttpRequestException("Bad request.", 400);
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
                    ex = new BadHttpRequestException($"Invalid request line: {value}", 400);
                    break;
                case RequestRejectionReason.InvalidContentLength:
                    ex = new BadHttpRequestException($"Invalid content length: {value}", 400);
                    break;
                case RequestRejectionReason.UnrecognizedHTTPVersion:
                    ex = new BadHttpRequestException($"Unrecognized HTTP version: {value}", 505);
                    break;
                case RequestRejectionReason.FinalTransferCodingNotChunked:
                    ex = new BadHttpRequestException($"Final transfer coding is not \"chunked\": \"{value}\"", 400);
                    break;
                default:
                    ex = new BadHttpRequestException("Bad request.", 400);
                    break;
            }
            return ex;
        }
    }
}
