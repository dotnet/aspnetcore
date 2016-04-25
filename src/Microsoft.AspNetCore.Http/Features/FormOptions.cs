// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Http.Features
{
    public class FormOptions
    {
        public const int DefaultMemoryBufferThreshold = 1024 * 64;
        public const int DefaultBufferBodyLengthLimit = 1024 * 1024 * 128;
        public const int DefaultMultipartBoundaryLengthLimit = 128;
        public const long DefaultMultipartBodyLengthLimit = 1024 * 1024 * 128;

        public bool BufferBody { get; set; } = false;
        public int MemoryBufferThreshold { get; set; } = DefaultMemoryBufferThreshold;
        public long BufferBodyLengthLimit { get; set; } = DefaultBufferBodyLengthLimit;
        public int KeyCountLimit { get; set; } = FormReader.DefaultKeyCountLimit;
        public int KeyLengthLimit { get; set; } = FormReader.DefaultKeyLengthLimit;
        public int ValueLengthLimit { get; set; } = FormReader.DefaultValueLengthLimit;
        public int MultipartBoundaryLengthLimit { get; set; } = DefaultMultipartBoundaryLengthLimit;
        public int MultipartHeadersCountLimit { get; set; } = MultipartReader.DefaultHeadersCountLimit;
        public int MultipartHeadersLengthLimit { get; set; } = MultipartReader.DefaultHeadersLengthLimit;
        public long MultipartBodyLengthLimit { get; set; } = DefaultMultipartBodyLengthLimit;
    }
}
