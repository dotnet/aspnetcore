// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Http.Features;

/// <summary>
/// Options to configure reading the request body as a HTTP form.
/// </summary>
public class FormOptions
{
    internal static readonly FormOptions Default = new FormOptions();

    /// <summary>
    /// Default value for <see cref="MemoryBufferThreshold"/>.
    /// Defaults to 65,536 bytes, which is approximately 64KB.
    /// </summary>
    public const int DefaultMemoryBufferThreshold = 1024 * 64;

    /// <summary>
    /// Default value for <see cref="BufferBodyLengthLimit"/>.
    /// Defaults to 134,217,728 bytes, which is 128MB.
    /// </summary>
    public const int DefaultBufferBodyLengthLimit = 1024 * 1024 * 128;

    /// <summary>
    /// Default value for <see cref="MultipartBoundaryLengthLimit"/>.
    /// Defaults to 128 bytes.
    /// </summary>
    public const int DefaultMultipartBoundaryLengthLimit = 128;

    /// <summary>
    /// Default value for <see cref="MultipartBodyLengthLimit "/>.
    /// Defaults to 134,217,728 bytes, which is approximately 128MB.
    /// </summary>
    public const long DefaultMultipartBodyLengthLimit = 1024 * 1024 * 128;

    /// <summary>
    /// Enables full request body buffering. Use this if multiple components need to read the raw stream.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool BufferBody { get; set; }

    /// <summary>
    /// If <see cref="BufferBody"/> is enabled, this many bytes of the body will be buffered in memory.
    /// If this threshold is exceeded then the buffer will be moved to a temp file on disk instead.
    /// This also applies when buffering individual multipart section bodies.
    /// Defaults to 65,536 bytes, which is approximately 64KB.
    /// </summary>
    public int MemoryBufferThreshold { get; set; } = DefaultMemoryBufferThreshold;

    /// <summary>
    /// If <see cref="BufferBody"/> is enabled, this is the limit for the total number of bytes that will
    /// be buffered. Forms that exceed this limit will throw an <see cref="InvalidDataException"/> when parsed.
    /// Defaults to 134,217,728 bytes, which is approximately 128MB.
    /// </summary>
    public long BufferBodyLengthLimit { get; set; } = DefaultBufferBodyLengthLimit;

    /// <summary>
    /// A limit for the number of form entries to allow.
    /// Forms that exceed this limit will throw an <see cref="InvalidDataException"/> when parsed.
    /// Defaults to 1024.
    /// </summary>
    public int ValueCountLimit { get; set; } = FormReader.DefaultValueCountLimit;

    /// <summary>
    /// A limit on the length of individual keys. Forms containing keys that exceed this limit will
    /// throw an <see cref="InvalidDataException"/> when parsed.
    /// Defaults to 2,048 bytes, which is approximately 2KB.
    /// </summary>
    public int KeyLengthLimit { get; set; } = FormReader.DefaultKeyLengthLimit;

    /// <summary>
    /// A limit on the length of individual form values. Forms containing values that exceed this
    /// limit will throw an <see cref="InvalidDataException"/> when parsed.
    /// Defaults to 4,194,304 bytes, which is approximately 4MB.
    /// </summary>
    public int ValueLengthLimit { get; set; } = FormReader.DefaultValueLengthLimit;

    /// <summary>
    /// A limit for the length of the boundary identifier. Forms with boundaries that exceed this
    /// limit will throw an <see cref="InvalidDataException"/> when parsed.
    /// Defaults to 128 bytes.
    /// </summary>
    public int MultipartBoundaryLengthLimit { get; set; } = DefaultMultipartBoundaryLengthLimit;

    /// <summary>
    /// A limit for the number of headers to allow in each multipart section. Headers with the same name will
    /// be combined. Form sections that exceed this limit will throw an <see cref="InvalidDataException"/>
    /// when parsed.
    /// Defaults to 16.
    /// </summary>
    public int MultipartHeadersCountLimit { get; set; } = MultipartReader.DefaultHeadersCountLimit;

    /// <summary>
    /// A limit for the total length of the header keys and values in each multipart section.
    /// Form sections that exceed this limit will throw an <see cref="InvalidDataException"/> when parsed.
    /// Defaults to 16,384 bytes, which is approximately 16KB.
    /// </summary>
    public int MultipartHeadersLengthLimit { get; set; } = MultipartReader.DefaultHeadersLengthLimit;

    /// <summary>
    /// A limit for the length of each multipart body. Forms sections that exceed this limit will throw an
    /// <see cref="InvalidDataException"/> when parsed.
    /// Defaults to 134,217,728 bytes, which is approximately 128MB.
    /// </summary>
    public long MultipartBodyLengthLimit { get; set; } = DefaultMultipartBodyLengthLimit;
}
