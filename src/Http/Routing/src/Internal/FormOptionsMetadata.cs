// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Metadata;

internal class FormOptionsMetadata(
    bool? bufferBody = null,
    int? memoryBufferThreshold = null,
    long? bufferBodyLengthLimit = null,
    int? valueCountLimit = null,
    int? keyLengthLimit = null,
    int? valueLengthLimit = null,
    int? multipartBoundaryLengthLimit = null,
    int? multipartHeadersCountLimit = null,
    int? multipartHeadersLengthLimit = null,
    long? multipartBodyLengthLimit = null) : IFormOptionsMetadata
{
    public bool? BufferBody { get; } = bufferBody;
    public int? MemoryBufferThreshold { get; } = memoryBufferThreshold;
    public long? BufferBodyLengthLimit { get; } = bufferBodyLengthLimit;
    public int? ValueCountLimit { get; } = valueCountLimit;
    public int? KeyLengthLimit { get; } = keyLengthLimit;
    public int? ValueLengthLimit { get; } = valueLengthLimit;
    public int? MultipartBoundaryLengthLimit { get; } = multipartBoundaryLengthLimit;
    public int? MultipartHeadersCountLimit { get; } = multipartHeadersCountLimit;
    public int? MultipartHeadersLengthLimit { get; } = multipartHeadersLengthLimit;
    public long? MultipartBodyLengthLimit { get; } = multipartBodyLengthLimit;
}
