// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Buffers;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

/// <summary>
/// An <see cref="IHttpResponseStreamWriterFactory"/> that uses pooled buffers.
/// </summary>
internal sealed class MemoryPoolHttpResponseStreamWriterFactory : IHttpResponseStreamWriterFactory
{
    /// <summary>
    /// The default size of buffers <see cref="HttpResponseStreamWriter"/>s will allocate.
    /// </summary>
    /// <value>
    /// 16K causes each <see cref="HttpResponseStreamWriter"/> to allocate one 16K
    /// <see langword="char"/> array and one 32K (for UTF8) <see langword="byte"/> array.
    /// </value>
    /// <remarks>
    /// <see cref="MemoryPoolHttpResponseStreamWriterFactory"/> maintains <see cref="ArrayPool{T}"/>s
    /// for these arrays.
    /// </remarks>
    public const int DefaultBufferSize = 16 * 1024;

    private readonly ArrayPool<byte> _bytePool;
    private readonly ArrayPool<char> _charPool;

    /// <summary>
    /// Creates a new <see cref="MemoryPoolHttpResponseStreamWriterFactory"/>.
    /// </summary>
    /// <param name="bytePool">
    /// The <see cref="ArrayPool{Byte}"/> for creating <see cref="byte"/> buffers.
    /// </param>
    /// <param name="charPool">
    /// The <see cref="ArrayPool{Char}"/> for creating <see cref="char"/> buffers.
    /// </param>
    public MemoryPoolHttpResponseStreamWriterFactory(
        ArrayPool<byte> bytePool,
        ArrayPool<char> charPool)
    {
        ArgumentNullException.ThrowIfNull(bytePool);
        ArgumentNullException.ThrowIfNull(charPool);

        _bytePool = bytePool;
        _charPool = charPool;
    }

    /// <inheritdoc />
    public TextWriter CreateWriter(Stream stream, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(encoding);

        return new HttpResponseStreamWriter(stream, encoding, DefaultBufferSize, _bytePool, _charPool);
    }
}
