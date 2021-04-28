// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Http.Features
{
    /// <summary>
    /// Options to configure reading the request body as a HTTP form.
    /// </summary>
    public class FormOptions
    {
        internal static readonly FormOptions Default = new FormOptions();

        /// <summary>
        /// Default value for <see cref="MemoryBufferThreshold"/>.
        /// </summary>
        /// <value>
        /// Defaults to <c>65,536 bytes‬</c>, which is approximately 0.52MB.
        /// </value>
        public const int DefaultMemoryBufferThreshold = 1024 * 64;

        /// <summary>
        /// Default value for <see cref="BufferBodyLengthLimit"/>.
        /// </summary>
        /// <value>
        /// Defaults to <c>134,217,728 bytes‬</c>, which is approximately 1.07GB.
        /// </value>
        public const int DefaultBufferBodyLengthLimit = 1024 * 1024 * 128;

        /// <summary>
        /// Default value for <see cref="MultipartBoundaryLengthLimit"/>.
        /// </summary>
        /// <value>
        /// Defaults to <c>128 bytes‬</c>.
        /// </value>
        public const int DefaultMultipartBoundaryLengthLimit = 128;

        /// <summary>
        /// Default value for <see cref="MultipartBodyLengthLimit "/>.
        /// </summary>
        /// <value>
        /// Defaults to <c>134,217,728 bytes‬</c>, which is approximately 1.07GB.
        /// </value>
        public const long DefaultMultipartBodyLengthLimit = 1024 * 1024 * 128;

        /// <summary>
        /// Enables full request body buffering. Use this if multiple components need to read the raw stream.
        /// </summary>
        /// <value>
        /// Defaults to <see langword="false" />.
        /// </value>
        public bool BufferBody { get; set; } = false;

        /// <summary>
        /// If <see cref="BufferBody"/> is enabled, this many bytes of the body will be buffered in memory.
        /// If this threshold is exceeded then the buffer will be moved to a temp file on disk instead.
        /// This also applies when buffering individual multipart section bodies.
        /// </summary>
        /// <value>
        /// Defaults to <c>65,536 bytes‬</c>, which is approximately 0.52MB.
        /// </value>
        public int MemoryBufferThreshold { get; set; } = DefaultMemoryBufferThreshold;

        /// <summary>
        /// If <see cref="BufferBody"/> is enabled, this is the limit for the total number of bytes that will
        /// be buffered. Forms that exceed this limit will throw an <see cref="InvalidDataException"/> when parsed.
        /// </summary>
        /// <value>
        /// Defaults to <c>134,217,728 bytes‬</c>, which is approximately 1.07GB.
        /// </value>
        public long BufferBodyLengthLimit { get; set; } = DefaultBufferBodyLengthLimit;

        /// <summary>
        /// A limit for the number of form entries to allow.
        /// Forms that exceed this limit will throw an <see cref="InvalidDataException"/> when parsed.
        /// </summary>
        /// <value>
        /// Defaults to <c>1024</c>.
        /// </value>
        public int ValueCountLimit { get; set; } = FormReader.DefaultValueCountLimit;

        /// <summary>
        /// A limit on the length of individual keys. Forms containing keys that exceed this limit will
        /// throw an <see cref="InvalidDataException"/> when parsed.
        /// </summary>
        /// <value>
        /// Defaults to <c>2,048 bytes‬</c>, which is approximately 16.38KB.
        /// </value>
        public int KeyLengthLimit { get; set; } = FormReader.DefaultKeyLengthLimit;

        /// <summary>
        /// A limit on the length of individual form values. Forms containing values that exceed this
        /// limit will throw an <see cref="InvalidDataException"/> when parsed.
        /// </summary>
        /// <value>
        /// Defaults to <c>4,194,304 bytes‬</c>, which is approximately 4.19MB.
        /// </value>
        public int ValueLengthLimit { get; set; } = FormReader.DefaultValueLengthLimit;

        /// <summary>
        /// A limit for the length of the boundary identifier. Forms with boundaries that exceed this
        /// limit will throw an <see cref="InvalidDataException"/> when parsed.
        /// </summary>
        /// <value>
        /// Defaults to <c>128 bytes‬</c>.
        /// </value>
        public int MultipartBoundaryLengthLimit { get; set; } = DefaultMultipartBoundaryLengthLimit;

        /// <summary>
        /// A limit for the number of headers to allow in each multipart section. Headers with the same name will
        /// be combined. Form sections that exceed this limit will throw an <see cref="InvalidDataException"/>
        /// when parsed.
        /// </summary>
        /// <value>
        /// Defaults to <c>16</c>.
        /// </value>
        public int MultipartHeadersCountLimit { get; set; } = MultipartReader.DefaultHeadersCountLimit;

        /// <summary>
        /// A limit for the total length of the header keys and values in each multipart section.
        /// Form sections that exceed this limit will throw an <see cref="InvalidDataException"/> when parsed.
        /// </summary>
        /// <value>
        /// Defaults to <c>16,384‬ bytes‬</c>, which is approximately 0.13MB.
        /// </value>
        public int MultipartHeadersLengthLimit { get; set; } = MultipartReader.DefaultHeadersLengthLimit;

        /// <summary>
        /// A limit for the length of each multipart body. Forms sections that exceed this limit will throw an
        /// <see cref="InvalidDataException"/> when parsed.
        /// </summary>
        /// <value>
        /// Defaults to <c>134,217,728 bytes‬</c>, which is approximately 1.07GB.
        /// </value>
        public long MultipartBodyLengthLimit { get; set; } = DefaultMultipartBodyLengthLimit;
    }
}
