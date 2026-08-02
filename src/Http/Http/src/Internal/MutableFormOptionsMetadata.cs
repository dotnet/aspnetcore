// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.AspNetCore.Http.Metadata;

namespace Microsoft.AspNetCore.Http.Features;

internal struct MutableFormOptionsMetadata(IFormOptionsMetadata formOptionsMetadata) : IFormOptionsMetadata
{
    internal FormOptions ResolveFormOptions(FormOptions baseFormOptions) => new FormOptions
    {
        BufferBody = BufferBody ?? baseFormOptions.BufferBody,
        MemoryBufferThreshold = MemoryBufferThreshold ?? baseFormOptions.MemoryBufferThreshold,
        BufferBodyLengthLimit = BufferBodyLengthLimit ?? baseFormOptions.BufferBodyLengthLimit,
        ValueCountLimit = ValueCountLimit ??  baseFormOptions.ValueCountLimit,
        KeyLengthLimit = KeyLengthLimit ?? baseFormOptions.KeyLengthLimit,
        ValueLengthLimit = ValueLengthLimit ?? baseFormOptions.ValueLengthLimit,
        MultipartBoundaryLengthLimit = MultipartBoundaryLengthLimit ?? baseFormOptions.MultipartBoundaryLengthLimit,
        MultipartHeadersCountLimit = MultipartHeadersCountLimit ?? baseFormOptions.MultipartHeadersCountLimit,
        MultipartHeadersLengthLimit = MultipartHeadersLengthLimit ?? baseFormOptions.MultipartHeadersLengthLimit,
        MultipartBodyLengthLimit = MultipartBodyLengthLimit ?? baseFormOptions.MultipartBodyLengthLimit
    };

    public bool? BufferBody { get; set; } = formOptionsMetadata.BufferBody;
    public int? MemoryBufferThreshold { get; set; } = formOptionsMetadata.MemoryBufferThreshold;
    public long? BufferBodyLengthLimit { get; set; } = formOptionsMetadata.BufferBodyLengthLimit;
    public int? ValueCountLimit { get; set; } = formOptionsMetadata.ValueCountLimit;
    public int? KeyLengthLimit { get; set; } = formOptionsMetadata.KeyLengthLimit;
    public int? ValueLengthLimit { get; set; } = formOptionsMetadata.ValueLengthLimit;
    public int? MultipartBoundaryLengthLimit { get; set; } = formOptionsMetadata.MultipartBoundaryLengthLimit;
    public int? MultipartHeadersCountLimit { get; set; } = formOptionsMetadata.MultipartHeadersCountLimit;
    public int? MultipartHeadersLengthLimit { get; set; } = formOptionsMetadata.MultipartHeadersLengthLimit;
    public long? MultipartBodyLengthLimit { get; set; } = formOptionsMetadata.MultipartBodyLengthLimit;
}
