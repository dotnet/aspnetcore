// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Buffers;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// An <see cref="IHttpRequestStreamReaderFactory"/> that uses pooled buffers.
/// </summary>
internal sealed class MemoryPoolHttpRequestStreamReaderFactory : IHttpRequestStreamReaderFactory
{
    /// <summary>
    /// The default size of created char buffers.
    /// </summary>
    public const int DefaultBufferSize = 1024; // 1KB - results in a 4KB byte array for UTF8.

    private readonly ArrayPool<byte> _bytePool;
    private readonly ArrayPool<char> _charPool;

    /// <summary>
    /// Creates a new <see cref="MemoryPoolHttpRequestStreamReaderFactory"/>.
    /// </summary>
    /// <param name="bytePool">
    /// The <see cref="ArrayPool{Byte}"/> for creating <see cref="T:byte[]"/> buffers.
    /// </param>
    /// <param name="charPool">
    /// The <see cref="ArrayPool{Char}"/> for creating <see cref="T:char[]"/> buffers.
    /// </param>
    public MemoryPoolHttpRequestStreamReaderFactory(
        ArrayPool<byte> bytePool,
        ArrayPool<char> charPool)
    {
        ArgumentNullException.ThrowIfNull(bytePool);
        ArgumentNullException.ThrowIfNull(charPool);

        _bytePool = bytePool;
        _charPool = charPool;
    }

    /// <inheritdoc />
    public TextReader CreateReader(Stream stream, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(encoding);

        return new HttpRequestStreamReader(stream, encoding, DefaultBufferSize, _bytePool, _charPool);
    }
}
