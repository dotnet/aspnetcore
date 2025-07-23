// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Web.Image;

/// <summary>
/// Provides a source for an image.
/// </summary>
public interface IImageSource
{
    /// <summary>
    /// Gets the MIME type of the image.
    /// </summary>
    string MimeType { get; }

    /// <summary>
    /// Gets the cache key for the image.
    /// </summary>
    string CacheKey { get; }
}

/// <summary>
/// Provides a loadable source for an image that can return the entire image as bytes.
/// </summary>
public interface ILoadableImageSource : IImageSource
{
    /// <summary>
    /// Gets the bytes for the image.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> containing the image bytes.</returns>
    ValueTask<byte[]> GetBytesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides a streaming source for an image, allowing incremental access to image data.
/// </summary>
public interface IStreamingImageSource : IImageSource
{
    /// <summary>
    /// Gets the total size of the image in bytes without loading the entire image.
    /// </summary>
    ValueTask<long> GetSizeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a stream to read the image data incrementally.
    /// </summary>
    ValueTask<Stream> OpenReadStreamAsync(CancellationToken cancellationToken = default);
}
