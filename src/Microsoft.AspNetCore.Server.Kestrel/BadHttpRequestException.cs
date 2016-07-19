// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Server.Kestrel.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel
{
    public sealed class BadHttpRequestException : IOException
    {
        private BadHttpRequestException(string message)
            : base(message)
        {

        }

        internal static BadHttpRequestException GetException(RequestRejectionReasons reason)
        {
            BadHttpRequestException ex;
            switch (reason)
            {
                case RequestRejectionReasons.MissingMethod:
                    ex = new BadHttpRequestException("Missing method.");
                    break;
                case RequestRejectionReasons.InvalidMethod:
                    ex = new BadHttpRequestException("Invalid method.");
                    break;
                case RequestRejectionReasons.MissingRequestTarget:
                    ex = new BadHttpRequestException("Missing request target.");
                    break;
                case RequestRejectionReasons.MissingHTTPVersion:
                    ex = new BadHttpRequestException("Missing HTTP version.");
                    break;
                case RequestRejectionReasons.UnrecognizedHTTPVersion:
                    ex = new BadHttpRequestException("Unrecognized HTTP version.");
                    break;
                case RequestRejectionReasons.MissingLFInRequestLine:
                    ex = new BadHttpRequestException("Missing LF in request line.");
                    break;
                case RequestRejectionReasons.HeadersCorruptedInvalidHeaderSequence:
                    ex = new BadHttpRequestException("Headers corrupted, invalid header sequence.");
                    break;
                case RequestRejectionReasons.HeaderLineMustNotStartWithWhitespace:
                    ex = new BadHttpRequestException("Header line must not start with whitespace.");
                    break;
                case RequestRejectionReasons.NoColonCharacterFoundInHeaderLine:
                    ex = new BadHttpRequestException("No ':' character found in header line.");
                    break;
                case RequestRejectionReasons.WhitespaceIsNotAllowedInHeaderName:
                    ex = new BadHttpRequestException("Whitespace is not allowed in header name.");
                    break;
                case RequestRejectionReasons.HeaderLineMustEndInCRLFOnlyCRFound:
                    ex = new BadHttpRequestException("Header line must end in CRLF; only CR found.");
                    break;
                case RequestRejectionReasons.HeaderValueLineFoldingNotSupported:
                    ex = new BadHttpRequestException("Header value line folding not supported.");
                    break;
                case RequestRejectionReasons.MalformedRequestInvalidHeaders:
                    ex = new BadHttpRequestException("Malformed request: invalid headers.");
                    break;
                case RequestRejectionReasons.UnexpectedEndOfRequestContent:
                    ex = new BadHttpRequestException("Unexpected end of request content");
                    break;
                case RequestRejectionReasons.BadChunkSuffix:
                    ex = new BadHttpRequestException("Bad chunk suffix");
                    break;
                case RequestRejectionReasons.BadChunkSizeData:
                    ex = new BadHttpRequestException("Bad chunk size data");
                    break;
                case RequestRejectionReasons.ChunkedRequestIncomplete:
                    ex = new BadHttpRequestException("Chunked request incomplete");
                    break;
                case RequestRejectionReasons.PathContainsNullCharacters:
                    ex = new BadHttpRequestException("The path contains null characters.");
                    break;
                case RequestRejectionReasons.InvalidCharactersInHeaderName:
                    ex = new BadHttpRequestException("Invalid characters in header name.");
                    break;
                case RequestRejectionReasons.NonAsciiOrNullCharactersInInputString:
                    ex = new BadHttpRequestException("The input string contains non-ASCII or null characters.");
                    break;
                default:
                    ex = new BadHttpRequestException("Bad request.");
                    break;
            }
            return ex;
        }

        internal static BadHttpRequestException GetException(RequestRejectionReasons reason, string value)
        {
            BadHttpRequestException ex;
            switch (reason)
            {
                case RequestRejectionReasons.MalformedRequestLineStatus:
                    ex = new BadHttpRequestException($"Malformed request: {value}");
                    break;
                case RequestRejectionReasons.InvalidContentLength:
                    ex = new BadHttpRequestException($"Invalid content length: {value}");
                    break;
                default:
                    ex = new BadHttpRequestException("Bad request.");
                    break;
            }
            return ex;
        }
    }
}
