// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Metadata;

namespace Microsoft.AspNetCore.Routing;

internal static class FormOptionsMetadataExtensions
{
    public static FormOptionsMetadata MergeWith(
        this IFormOptionsMetadata formOptionsMetadata,
        IFormOptionsMetadata otherFormOptionsMetadata)
    {
        var _bufferBody = otherFormOptionsMetadata.BufferBody != null ? otherFormOptionsMetadata.BufferBody : formOptionsMetadata.BufferBody;
        var _memoryBufferThreshold = otherFormOptionsMetadata.MemoryBufferThreshold != null ? otherFormOptionsMetadata.MemoryBufferThreshold : formOptionsMetadata.MemoryBufferThreshold;
        var _bufferBodyLengthLimit = otherFormOptionsMetadata.BufferBodyLengthLimit != null ? otherFormOptionsMetadata.BufferBodyLengthLimit : formOptionsMetadata.BufferBodyLengthLimit;
        var _valueCountLimit = otherFormOptionsMetadata.ValueCountLimit != null ? otherFormOptionsMetadata.ValueCountLimit : formOptionsMetadata.ValueCountLimit;
        var _keyLengthLimit = otherFormOptionsMetadata.KeyLengthLimit != null ? otherFormOptionsMetadata.KeyLengthLimit : formOptionsMetadata.KeyLengthLimit;
        var _valueLengthLimit = otherFormOptionsMetadata.ValueLengthLimit != null ? otherFormOptionsMetadata.ValueLengthLimit : formOptionsMetadata.ValueLengthLimit;
        var _multipartBoundaryLengthLimit = otherFormOptionsMetadata.MultipartBoundaryLengthLimit != null ? otherFormOptionsMetadata.MultipartBoundaryLengthLimit : formOptionsMetadata.MultipartBoundaryLengthLimit;
        var _multipartHeadersCountLimit = otherFormOptionsMetadata.MultipartHeadersCountLimit != null ? otherFormOptionsMetadata.MultipartHeadersCountLimit : formOptionsMetadata.MultipartHeadersCountLimit;
        var _multipartHeadersLengthLimit = otherFormOptionsMetadata.MultipartHeadersLengthLimit != null ? otherFormOptionsMetadata.MultipartHeadersLengthLimit : formOptionsMetadata.MultipartHeadersLengthLimit;
        var _multipartBodyLengthLimit = otherFormOptionsMetadata.MultipartBodyLengthLimit != null ? otherFormOptionsMetadata.MultipartBodyLengthLimit : formOptionsMetadata.MultipartBodyLengthLimit;
        return new FormOptionsMetadata(_bufferBody, _memoryBufferThreshold, _bufferBodyLengthLimit, _valueCountLimit, _keyLengthLimit, _valueLengthLimit, _multipartBoundaryLengthLimit, _multipartHeadersCountLimit, _multipartHeadersLengthLimit, _multipartBodyLengthLimit);
    }
}
