// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.Metadata;

/// <summary>
/// Interface marking attributes that specify limits associated with reading a form.
/// </summary>
public interface IFormOptionsMetadata
{
    /// <summary>
    /// Enables full request body buffering. Use this if multiple components need to read the raw stream. Defaults to false.
    /// </summary>
    bool? BufferBody { get; }

    /// <summary>
    /// If BufferBody is enabled, this many bytes of the body will be buffered in memory.
    /// If this threshold is exceeded then the buffer will be moved to a temp file on disk instead.
    /// This also applies when buffering individual multipart section bodies. Defaults to 65,536 bytes, which is approximately 64KB.
    /// </summary>
    int? MemoryBufferThreshold { get; }

    /// <summary>
    /// If BufferBody is enabled, this is the limit for the total number of bytes that will be buffered.
    /// Forms that exceed this limit will throw an InvalidDataException when parsed. Defaults to 134,217,728 bytes, which is approximately 128MB.
    /// </summary>
    long? BufferBodyLengthLimit { get; }

    /// <summary>
    /// A limit for the number of form entries to allow. Forms that exceed this limit will throw an InvalidDataException when parsed. Defaults to 1024.
    /// </summary>
    int? ValueCountLimit { get; }

    /// <summary>
    /// A limit on the length of individual keys. Forms containing keys that
    /// exceed this limit will throw an InvalidDataException when parsed.
    /// Defaults to 2,048 bytes, which is approximately 2KB.
    /// </summary>
    int? KeyLengthLimit { get; }

    /// <summary>
    /// A limit on the length of individual form values. Forms containing
    /// values that exceed this limit will throw an InvalidDataException
    /// when parsed. Defaults to 4,194,304 bytes, which is approximately 4MB.
    /// </summary>
    int? ValueLengthLimit { get; }

    /// <summary>
    /// A limit for the length of the boundary identifier. Forms with boundaries
    /// that exceed this limit will throw an InvalidDataException when parsed.
    /// Defaults to 128 bytes.
    /// </summary>
    int? MultipartBoundaryLengthLimit { get; }

    /// <summary>
    /// A limit for the number of headers to allow in each multipart section.
    /// Headers with the same name will be combined. Form sections that exceed
    /// this limit will throw an InvalidDataException when parsed. Defaults to 16.
    /// </summary>
    int? MultipartHeadersCountLimit { get; }

    /// <summary>
    /// A limit for the total length of the header keys and values in each
    /// multipart section. Form sections that exceed this limit will throw
    /// an InvalidDataException when parsed. Defaults to 16,384 bytes,
    /// which is approximately 16KB.
    /// </summary>
    int? MultipartHeadersLengthLimit { get; }

    /// <summary>
    /// /A limit for the length of each multipart body. Forms sections that
    /// exceed this limit will throw an InvalidDataException when parsed.
    /// Defaults to 134,217,728 bytes, which is approximately 128MB.
    /// </summary>
    long? MultipartBodyLengthLimit { get; }
}
