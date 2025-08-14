// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web.Image;

/// <summary>
/// Provides a unified source for image data that can be supplied as either byte arrays or streams.
/// Internally stores the data as a byte array so that each consumer can obtain an independent
/// read-only <see cref="MemoryStream"/>. This avoids concurrency issues when the same image is
/// rendered simultaneously (e.g., thumbnail + modal) or when reloaded dynamically.
/// </summary>
public class ImageSource
{
    private readonly byte[] _data;
    private readonly string _mimeType;
    private readonly string _cacheKey;
    private readonly long? _length;

    /// <summary>
    /// Gets the MIME type of the image.
    /// </summary>
    public string MimeType => _mimeType;

    /// <summary>
    /// Gets the cache key for the image. Always non-null.
    /// </summary>
    public string CacheKey => _cacheKey;

    /// <summary>
    /// Gets a (fresh) stream to read the image data from the beginning. Each call returns a new
    /// non-writable <see cref="MemoryStream"/> positioned at 0.
    /// </summary>
    public Stream Stream => OpenRead();

    /// <summary>
    /// Gets the length of the image data in bytes.
    /// </summary>
    public long? Length => _length;

    /// <summary>
    /// Initializes a new instance of <see cref="ImageSource"/> with byte array data.
    /// The byte array reference is stored directly (no copy), so callers should not mutate it afterwards.
    /// </summary>
    public ImageSource(byte[] data, string mimeType, string cacheKey)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
        _mimeType = mimeType ?? throw new ArgumentNullException(nameof(mimeType));
        _cacheKey = cacheKey ?? throw new ArgumentNullException(nameof(cacheKey));
        _length = _data.LongLength;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ImageSource"/> by reading the provided stream fully into memory.
    /// The original stream is consumed (read to end) but not disposed here; the caller retains ownership.
    /// </summary>
    public ImageSource(Stream stream, string mimeType, string cacheKey)
    {
        ArgumentNullException.ThrowIfNull(stream);
        _mimeType = mimeType ?? throw new ArgumentNullException(nameof(mimeType));
        _cacheKey = cacheKey ?? throw new ArgumentNullException(nameof(cacheKey));

        // Copy stream contents once so future concurrent reads are safe and cheap.
        if (stream is MemoryStream ms && ms.TryGetBuffer(out var segment))
        {
            // Fast path: direct buffer copy
            _data = segment.Array![segment.Offset..(segment.Offset + segment.Count)];
        }
        else
        {
            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            _data = copy.ToArray();
        }
        _length = _data.LongLength;
    }

    /// <summary>
    /// Opens a new read-only memory stream over the underlying image data.
    /// </summary>
    public MemoryStream OpenRead() => new MemoryStream(_data, writable: false);
}
