// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Web.Image;
namespace BlazorShared;

public class BinaryImageSource : IImageSource
{
    private readonly byte[]? _data;
    private readonly Func<Task<byte[]>>? _dataProvider;

    public string MimeType { get; }
    public string CacheKey { get; }

    public BinaryImageSource(string mimeType, byte[] data, string? cacheKey = null)
    {
        MimeType = mimeType ?? throw new ArgumentNullException(nameof(mimeType));
        _data = data ?? throw new ArgumentNullException(nameof(data));
        _dataProvider = null;
        CacheKey = cacheKey ?? ComputeCacheKey(data);
    }

    public BinaryImageSource(string mimeType, Func<Task<byte[]>> dataProvider, string cacheKey)
    {
        MimeType = mimeType ?? throw new ArgumentNullException(nameof(mimeType));
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        _data = null;
        CacheKey = cacheKey ?? throw new ArgumentNullException(nameof(cacheKey));
    }

    public async ValueTask<byte[]> GetBytesAsync(CancellationToken ct)
    {
        if (_data != null)
        {
            return _data;
        }

        return _dataProvider != null ? await _dataProvider() : throw new InvalidOperationException("No image data source available");
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

    public static BinaryImageSource FromRepository(int imageId, string mimeType, Func<Task<byte[]?>> dataProvider)
        => new BinaryImageSource(mimeType, async () => await dataProvider() ?? Array.Empty<byte>(), $"img-{imageId}");
}
