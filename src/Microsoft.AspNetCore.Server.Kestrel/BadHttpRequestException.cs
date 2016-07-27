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

        internal static BadHttpRequestException GetException(RequestRejectionReason reason)
        {
            BadHttpRequestException ex;
            switch (reason)
            {
                case RequestRejectionReason.MissingMethod:
                    ex = new BadHttpRequestException("Missing method.");
                    break;
                case RequestRejectionReason.InvalidMethod:
                    ex = new BadHttpRequestException("Invalid method.");
                    break;
                case RequestRejectionReason.MissingRequestTarget:
                    ex = new BadHttpRequestException("Missing request target.");
                    break;
                case RequestRejectionReason.MissingHTTPVersion:
                    ex = new BadHttpRequestException("Missing HTTP version.");
                    break;
                case RequestRejectionReason.UnrecognizedHTTPVersion:
                    ex = new BadHttpRequestException("Unrecognized HTTP version.");
                    break;
                case RequestRejectionReason.MissingLFInRequestLine:
                    ex = new BadHttpRequestException("Missing LF in request line.");
                    break;
                case RequestRejectionReason.HeadersCorruptedInvalidHeaderSequence:
                    ex = new BadHttpRequestException("Headers corrupted, invalid header sequence.");
                    break;
                case RequestRejectionReason.HeaderLineMustNotStartWithWhitespace:
                    ex = new BadHttpRequestException("Header line must not start with whitespace.");
                    break;
                case RequestRejectionReason.NoColonCharacterFoundInHeaderLine:
                    ex = new BadHttpRequestException("No ':' character found in header line.");
                    break;
                case RequestRejectionReason.WhitespaceIsNotAllowedInHeaderName:
                    ex = new BadHttpRequestException("Whitespace is not allowed in header name.");
                    break;
                case RequestRejectionReason.HeaderLineMustEndInCRLFOnlyCRFound:
                    ex = new BadHttpRequestException("Header line must end in CRLF; only CR found.");
                    break;
                case RequestRejectionReason.HeaderValueLineFoldingNotSupported:
                    ex = new BadHttpRequestException("Header value line folding not supported.");
                    break;
                case RequestRejectionReason.MalformedRequestInvalidHeaders:
                    ex = new BadHttpRequestException("Malformed request: invalid headers.");
                    break;
                case RequestRejectionReason.UnexpectedEndOfRequestContent:
                    ex = new BadHttpRequestException("Unexpected end of request content.");
                    break;
                case RequestRejectionReason.BadChunkSuffix:
                    ex = new BadHttpRequestException("Bad chunk suffix.");
                    break;
                case RequestRejectionReason.BadChunkSizeData:
                    ex = new BadHttpRequestException("Bad chunk size data.");
                    break;
                case RequestRejectionReason.ChunkedRequestIncomplete:
                    ex = new BadHttpRequestException("Chunked request incomplete.");
                    break;
                case RequestRejectionReason.PathContainsNullCharacters:
                    ex = new BadHttpRequestException("The path contains null characters.");
                    break;
                case RequestRejectionReason.InvalidCharactersInHeaderName:
                    ex = new BadHttpRequestException("Invalid characters in header name.");
                    break;
                case RequestRejectionReason.NonAsciiOrNullCharactersInInputString:
                    ex = new BadHttpRequestException("The input string contains non-ASCII or null characters.");
                    break;
                case RequestRejectionReason.RequestLineTooLong:
                    ex = new BadHttpRequestException("Request line too long.");
                    break;
                case RequestRejectionReason.MissingSpaceAfterMethod:
                    ex = new BadHttpRequestException("No space character found after method in request line.");
                    break;
                case RequestRejectionReason.MissingSpaceAfterTarget:
                    ex = new BadHttpRequestException("No space character found after target in request line.");
                    break;
                case RequestRejectionReason.MissingCrAfterVersion:
                    ex = new BadHttpRequestException("Missing CR in request line.");
                    break;
                default:
                    ex = new BadHttpRequestException("Bad request.");
                    break;
            }
            return ex;
        }

        internal static BadHttpRequestException GetException(RequestRejectionReason reason, string value)
        {
            BadHttpRequestException ex;
            switch (reason)
            {
                case RequestRejectionReason.MalformedRequestLineStatus:
                    ex = new BadHttpRequestException($"Invalid request line: {value}");
                    break;
                case RequestRejectionReason.InvalidContentLength:
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
