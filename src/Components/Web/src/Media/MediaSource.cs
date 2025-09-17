// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web.Media;

/// <summary>
/// Represents a single-use source for media data. A <see cref="MediaSource"/> corresponds to
/// exactly one load operation. It holds a single underlying <see cref="Stream"/> that will be
/// consumed by a media component. Reuse of an instance for multiple components or multiple
/// loads is not supported.
/// </summary>
public class MediaSource
{
    /// <summary>
    /// Gets the MIME type of the media.
    /// </summary>
    public string MimeType { get; }

    /// <summary>
    /// Gets the cache key for the media. Always non-null.
    /// </summary>
    public string CacheKey { get; }

    /// <summary>
    /// Gets the underlying stream.
    /// </summary>
    public Stream Stream { get; }

    /// <summary>
    /// Gets the length of the media data in bytes if known.
    /// </summary>
    public long? Length { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="MediaSource"/> with byte array data.
    /// A non-writable <see cref="MemoryStream"/> is created over the provided data. The byte
    /// array reference is not copied, so callers should not mutate it afterwards.
    /// </summary>
    /// <param name="data">The media data as a byte array.</param>
    /// <param name="mimeType">The media MIME type.</param>
    /// <param name="cacheKey">The cache key used for caching and re-use.</param>
    public MediaSource(byte[] data, string mimeType, string cacheKey)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(mimeType);
        ArgumentNullException.ThrowIfNull(cacheKey);

        MimeType = mimeType;
        CacheKey = cacheKey;
        Stream = new MemoryStream(data, writable: false);
        Length = data.LongLength;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="MediaSource"/> from an existing stream.
    /// The stream reference is retained (not copied). The caller retains ownership and is
    /// responsible for disposal after the media has loaded. The stream must remain readable
    /// for the duration of the load.
    /// </summary>
    /// <param name="stream">The readable stream positioned at the beginning.</param>
    /// <param name="mimeType">The media MIME type.</param>
    /// <param name="cacheKey">The cache key.</param>
    public MediaSource(Stream stream, string mimeType, string cacheKey)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(mimeType);
        ArgumentNullException.ThrowIfNull(cacheKey);

        Stream = stream;
        MimeType = mimeType;
        CacheKey = cacheKey;
        if (stream.CanSeek)
        {
            Length = stream.Length;
        }
        else
        {
            Length = null;
        }
    }
}
