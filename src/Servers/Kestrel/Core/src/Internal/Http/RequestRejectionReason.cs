// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal enum RequestRejectionReason
    {
        TlsOverHttpError,
        UnrecognizedHTTPVersion,
        InvalidRequestLine,
        InvalidRequestHeader,
        InvalidRequestHeadersNoCRLF,
        MalformedRequestInvalidHeaders,
        InvalidContentLength,
        MultipleContentLengths,
        UnexpectedEndOfRequestContent,
        BadChunkSuffix,
        BadChunkSizeData,
        ChunkedRequestIncomplete,
        InvalidRequestTarget,
        InvalidCharactersInHeaderName,
        RequestLineTooLong,
        HeadersExceedMaxTotalSize,
        TooManyHeaders,
        RequestBodyTooLarge,
        RequestHeadersTimeout,
        RequestBodyTimeout,
        FinalTransferCodingNotChunked,
        LengthRequired,
        LengthRequiredHttp10,
        OptionsMethodRequired,
        ConnectMethodRequired,
        MissingHostHeader,
        MultipleHostHeaders,
        InvalidHostHeader,
        UpgradeRequestCannotHavePayload,
        RequestBodyExceedsContentLength
    }
}
