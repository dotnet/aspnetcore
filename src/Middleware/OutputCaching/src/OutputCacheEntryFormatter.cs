// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.OutputCaching.Serialization;

namespace Microsoft.AspNetCore.OutputCaching;
/// <summary>
/// Formats <see cref="OutputCacheEntry"/> instance to match structures supported by the <see cref="IOutputCacheStore"/> implementations.
/// </summary>
internal class OutputCacheEntryFormatter
{
    public static async ValueTask<OutputCacheEntry?> GetAsync(string key, IOutputCacheStore store, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(key);

        var content = await store.GetAsync(key, token);

        if (content == null)
        {
            return null;
        }

        using var br = new MemoryStream(content);

        var formatter = await JsonSerializer.DeserializeAsync(br, FormatterEntrySerializerContext.Default.FormatterEntry, cancellationToken: token);

        if (formatter == null)
        {
            return null;
        }

        var outputCacheEntry = new OutputCacheEntry
        {
            StatusCode = formatter.StatusCode,
            Created = formatter.Created,
            Tags = formatter.Tags
        };

        if (formatter.Headers != null)
        {
            outputCacheEntry.Headers = new();

            foreach (var header in formatter.Headers)
            {
                outputCacheEntry.Headers.TryAdd(header.Key, header.Value);
            }
        }
        var cachedResponseBody = new CachedResponseBody(formatter.Body, formatter.Body.Sum(x => x.Length));
        outputCacheEntry.Body = cachedResponseBody;
        return outputCacheEntry;
    }

    public static async ValueTask StoreAsync(string key, OutputCacheEntry value, TimeSpan duration, IOutputCacheStore store, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(value);

        var formatterEntry = new FormatterEntry
        {
            StatusCode = value.StatusCode,
            Created = value.Created,
            Tags = value.Tags,
            Body = value.Body.Segments
        };

        if (value.Headers != null)
        {
            formatterEntry.Headers = new();
            foreach (var header in value.Headers)
            {
                formatterEntry.Headers.TryAdd(header.Key, header.Value.ToArray());
            }
        }

        using var br = new MemoryStream();

        await JsonSerializer.SerializeAsync(br, formatterEntry, FormatterEntrySerializerContext.Default.FormatterEntry, token);
        await store.SetAsync(key, br.ToArray(), value.Tags ?? Array.Empty<string>(), duration, token);
    }
}
