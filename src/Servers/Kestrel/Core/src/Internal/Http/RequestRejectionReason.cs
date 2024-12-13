// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

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
    LengthRequiredHttp10,
    OptionsMethodRequired,
    ConnectMethodRequired,
    MissingHostHeader,
    MultipleHostHeaders,
    InvalidHostHeader
}
