// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

/// <summary>
/// Result of non-throwing HTTP parsing operations.
/// </summary>
internal readonly struct HttpParseResult
{
    private enum ParseStatus : byte { Incomplete, Complete, Error }

    private readonly ParseStatus _status;
    private readonly RequestRejectionReason _errorReason;
    private readonly int _errorOffset;
    private readonly int _errorLength;

    private HttpParseResult(ParseStatus status, RequestRejectionReason errorReason = default, int errorOffset = 0, int errorLength = 0)
    {
        _status = status;
        _errorReason = errorReason;
        _errorOffset = errorOffset;
        _errorLength = errorLength;
    }

    /// <summary>True if parsing completed successfully.</summary>
    public bool IsComplete => _status == ParseStatus.Complete;

    /// <summary>True if a parse error occurred.</summary>
    public bool HasError => _status == ParseStatus.Error;

    /// <summary>The reason for rejection, if HasError is true.</summary>
    public RequestRejectionReason ErrorReason => _errorReason;

    /// <summary>Offset into the buffer where the error was detected.</summary>
    public int ErrorOffset => _errorOffset;

    /// <summary>Length of the problematic data, for error reporting.</summary>
    public int ErrorLength => _errorLength;

    /// <summary>Parsing needs more data.</summary>
    public static HttpParseResult Incomplete => new(ParseStatus.Incomplete);

    /// <summary>Parsing completed successfully.</summary>
    public static HttpParseResult Complete => new(ParseStatus.Complete);

    /// <summary>Parsing failed with the specified error.</summary>
    public static HttpParseResult Error(RequestRejectionReason reason) => new(ParseStatus.Error, reason);

    /// <summary>Parsing failed with the specified error and location info for detailed error messages.</summary>
    public static HttpParseResult Error(RequestRejectionReason reason, int offset, int length) => new(ParseStatus.Error, reason, offset, length);
}

internal interface IHttpParser<TRequestHandler> where TRequestHandler : IHttpHeadersHandler, IHttpRequestLineHandler
{
    bool ParseRequestLine(TRequestHandler handler, ref SequenceReader<byte> reader);

    bool ParseHeaders(TRequestHandler handler, ref SequenceReader<byte> reader);
}
