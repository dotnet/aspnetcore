// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Metadata;

namespace Microsoft.AspNetCore.Http.Features;

internal static class FormOptionsMetadataExtensions
{
    public static void MergeWith(
        this IFormOptionsMetadata formOptionsMetadata,
        ref MutableFormOptionsMetadata otherFormOptionsMetadata)
    {
        otherFormOptionsMetadata.BufferBody ??= formOptionsMetadata.BufferBody;
        otherFormOptionsMetadata.MemoryBufferThreshold ??= formOptionsMetadata.MemoryBufferThreshold;
        otherFormOptionsMetadata.BufferBodyLengthLimit ??= formOptionsMetadata.BufferBodyLengthLimit;
        otherFormOptionsMetadata.ValueCountLimit ??= formOptionsMetadata.ValueCountLimit;
        otherFormOptionsMetadata.KeyLengthLimit ??= formOptionsMetadata.KeyLengthLimit;
        otherFormOptionsMetadata.ValueLengthLimit ??= formOptionsMetadata.ValueLengthLimit;
        otherFormOptionsMetadata.MultipartBoundaryLengthLimit ??= formOptionsMetadata.MultipartBoundaryLengthLimit;
        otherFormOptionsMetadata.MultipartHeadersCountLimit ??= formOptionsMetadata.MultipartHeadersCountLimit;
        otherFormOptionsMetadata.MultipartHeadersLengthLimit ??= formOptionsMetadata.MultipartHeadersLengthLimit;
        otherFormOptionsMetadata.MultipartBodyLengthLimit ??= formOptionsMetadata.MultipartBodyLengthLimit;
    }
}
