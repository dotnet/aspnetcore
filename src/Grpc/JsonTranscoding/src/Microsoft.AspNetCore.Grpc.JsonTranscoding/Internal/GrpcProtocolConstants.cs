// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal;

internal static class GrpcProtocolConstants
{
    internal const string TimeoutHeader = "grpc-timeout";
    internal const string MessageEncodingHeader = "grpc-encoding";
    internal const string MessageAcceptEncodingHeader = "grpc-accept-encoding";
    internal static readonly ReadOnlyMemory<byte> StreamingDelimiter = new byte[] { (byte)'\n' };

    internal static readonly HashSet<string> FilteredHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        MessageEncodingHeader,
        MessageAcceptEncodingHeader,
        TimeoutHeader,
        HeaderNames.ContentType,
        HeaderNames.TE,
        HeaderNames.Host,
        HeaderNames.AcceptEncoding
    };
}
