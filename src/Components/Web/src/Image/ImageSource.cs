// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web.Image;

/// <summary>
/// Represents a single-use source for image data. An <see cref="ImageSource"/> corresponds to
/// exactly one image load. It holds a single underlying <see cref="Stream"/> that will be
/// consumed by the image component. Reuse of an instance for multiple components or multiple
/// loads is not supported.
/// </summary>
public class ImageSource
{
    /// <summary>
    /// Gets the MIME type of the image.
    /// </summary>
    public string MimeType { get; }

    /// <summary>
    /// Gets the cache key for the image. Always non-null.
    /// </summary>
    public string CacheKey { get; }

    /// <summary>
    /// Gets the underlying stream.
    /// </summary>
    public Stream Stream { get; }

    /// <summary>
    /// Gets the length of the image data in bytes if known.
    /// </summary>
    public long? Length { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="ImageSource"/> with byte array data.
    /// A non-writable <see cref="MemoryStream"/> is created over the provided data. The byte
    /// array reference is not copied, so callers should not mutate it afterwards.
    /// </summary>
    public ImageSource(byte[] data, string mimeType, string cacheKey)
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
    /// Initializes a new instance of <see cref="ImageSource"/> from an existing stream.
    /// The stream reference is retained (not copied). The caller retains ownership and is
    /// responsible for disposal after the image has loaded. The stream must remain readable
    /// for the duration of the load.
    /// </summary>
    /// <param name="stream">The readable stream positioned at the beginning.</param>
    /// <param name="mimeType">The image MIME type.</param>
    /// <param name="cacheKey">The cache key.</param>
    public ImageSource(Stream stream, string mimeType, string cacheKey)
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
