// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
{
    public enum RequestRejectionReason
    {
        MissingMethod,
        InvalidMethod,
        MissingRequestTarget,
        MissingHTTPVersion,
        UnrecognizedHTTPVersion,
        MissingLFInRequestLine,
        HeadersCorruptedInvalidHeaderSequence,
        HeaderLineMustNotStartWithWhitespace,
        NoColonCharacterFoundInHeaderLine,
        WhitespaceIsNotAllowedInHeaderName,
        HeaderLineMustEndInCRLFOnlyCRFound,
        HeaderValueLineFoldingNotSupported,
        MalformedRequestLineStatus,
        MalformedRequestInvalidHeaders,
        InvalidContentLength,
        UnexpectedEndOfRequestContent,
        BadChunkSuffix,
        BadChunkSizeData,
        ChunkedRequestIncomplete,
        PathContainsNullCharacters,
        InvalidCharactersInHeaderName,
        NonAsciiOrNullCharactersInInputString,
        RequestLineTooLong,
        MissingSpaceAfterMethod,
        MissingSpaceAfterTarget,
        MissingCrAfterVersion,
    }
}
