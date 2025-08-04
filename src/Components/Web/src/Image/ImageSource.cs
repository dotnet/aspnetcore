// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web.Image;

/// <summary>
/// Provides a unified source for image data that can be supplied as either byte arrays or streams.
/// </summary>
public class ImageSource
{
    private readonly Stream _stream;
    private readonly string _mimeType;
    private readonly string? _cacheKey;
    private long? _length;

    /// <summary>
    /// Gets the MIME type of the image.
    /// </summary>
    public string MimeType => _mimeType;

    /// <summary>
    /// Gets the cache key for the image. Null if no caching is desired.
    /// </summary>
    public string? CacheKey => _cacheKey;

    /// <summary>
    /// Gets a stream to read the image data.
    /// </summary>
    public Stream Stream => _stream;

    /// <summary>
    /// Gets or sets the length of the image data in bytes. May be null if the length cannot be determined.
    /// </summary>
    public long? Length
    {
        get => _length;
        set => _length = value;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ImageSource"/> with byte array data.
    /// </summary>
    /// <param name="data">The image data as a byte array.</param>
    /// <param name="mimeType">The MIME type of the image.</param>
    /// <param name="cacheKey">Optional cache key for memory caching. If not provided, no caching will be used.</param>
    public ImageSource(byte[] data, string mimeType, string? cacheKey)
    {
        _stream = new MemoryStream(data) ?? throw new ArgumentNullException(nameof(data));
        _mimeType = mimeType ?? throw new ArgumentNullException(nameof(mimeType));
        _cacheKey = cacheKey;

        try
        {
            _length = _stream.Length;
        }
        catch
        {
            _length = null;
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ImageSource"/> with a stream.
    /// </summary>
    /// <param name="stream">The stream containing the image data.</param>
    /// <param name="mimeType">The MIME type of the image.</param>
    /// <param name="cacheKey">Optional cache key for memory caching. If not provided, no caching will be used.</param>
    public ImageSource(Stream stream, string mimeType, string? cacheKey)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _mimeType = mimeType ?? throw new ArgumentNullException(nameof(mimeType));
        _cacheKey = cacheKey;

        try
        {
            _length = _stream.Length;
        }
        catch
        {
            _length = null;
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ImageSource"/> with byte array data.
    /// </summary>
    /// <param name="data">The image data as a byte array.</param>
    /// <param name="mimeType">The MIME type of the image.</param>
    public ImageSource(byte[] data, string mimeType) : this(data, mimeType, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ImageSource"/> with a stream.
    /// </summary>
    /// <param name="stream">The stream containing the image data.</param>
    /// <param name="mimeType">The MIME type of the image.</param>
    public ImageSource(Stream stream, string mimeType) : this(stream, mimeType, null)
    {
    }
}
