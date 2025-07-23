// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web.Image;

namespace BlazorShared;

public class StreamingImageSource : IStreamingImageSource
{
    private readonly byte[]? _data;
    private readonly Func<Task<byte[]>>? _dataProvider;

    public string MimeType { get; }
    public string CacheKey { get; }

    public StreamingImageSource(string mimeType, byte[] data, string? cacheKey = null)
    {
        MimeType = mimeType ?? throw new ArgumentNullException(nameof(mimeType));
        _data = data ?? throw new ArgumentNullException(nameof(data));
        _dataProvider = null;
        CacheKey = cacheKey ?? ComputeCacheKey(data);
    }

    public StreamingImageSource(string mimeType, Func<Task<byte[]>> dataProvider, string cacheKey)
    {
        MimeType = mimeType ?? throw new ArgumentNullException(nameof(mimeType));
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _data = null;
        CacheKey = cacheKey ?? throw new ArgumentNullException(nameof(cacheKey));
    }

    public async ValueTask<long> GetSizeAsync(CancellationToken cancellationToken = default)
    {
        if (_data != null)
        {
            return _data.Length;
        }

        if (_dataProvider != null)
        {
            var data = await _dataProvider();
            return data?.Length ?? 0;
        }

        throw new InvalidOperationException("No image data source available");
    }

    public async ValueTask<Stream> OpenReadStreamAsync(CancellationToken cancellationToken = default)
    {
        if (_data != null)
        {
            return new MemoryStream(_data);
        }

        if (_dataProvider != null)
        {
            var data = await _dataProvider();
            return new MemoryStream(data ?? Array.Empty<byte>());
        }

        throw new InvalidOperationException("No image data source available");
    }

    private static string ComputeCacheKey(byte[] data)
    {
        unchecked
        {
            int hash = 17;
            foreach (var b in data)
            {
                hash = hash * 31 + b;
            }
            return hash.ToString("x", System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    public static StreamingImageSource FromRepository(int imageId, string mimeType, Func<Task<byte[]?>> dataProvider)
        => new StreamingImageSource(mimeType, async () => await dataProvider() ?? Array.Empty<byte>(), $"streaming-img-{imageId}");
}
