// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.OutputCaching.Serialization;

namespace Microsoft.AspNetCore.OutputCaching;
/// <summary>
/// Formats <see cref="OutputCacheEntry"/> instance to match structures supported by the <see cref="IOutputCacheStore"/> implementations.
/// </summary>
internal static class OutputCacheEntryFormatter
{
    public static async ValueTask<OutputCacheEntry?> GetAsync(string key, IOutputCacheStore store, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);

        var content = await store.GetAsync(key, cancellationToken);

        if (content == null)
        {
            return null;
        }

        var formatter = JsonSerializer.Deserialize(content, FormatterEntrySerializerContext.Default.FormatterEntry);

        if (formatter == null)
        {
            return null;
        }

        var outputCacheEntry = new OutputCacheEntry
        {
            StatusCode = formatter.StatusCode,
            Created = formatter.Created,
            Tags = formatter.Tags,
            Headers = new(),
            Body = new CachedResponseBody(formatter.Body, formatter.Body.Sum(x => x.Length))
        };

        if (formatter.Headers != null)
        {
            foreach (var header in formatter.Headers)
            {
                outputCacheEntry.Headers.TryAdd(header.Key, header.Value);
            }
        }

        return outputCacheEntry;
    }

    public static async ValueTask StoreAsync(string key, OutputCacheEntry value, TimeSpan duration, IOutputCacheStore store, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(value.Body);
        ArgumentNullException.ThrowIfNull(value.Headers);

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

        using var bufferStream = new MemoryStream();

        JsonSerializer.Serialize(bufferStream, formatterEntry, FormatterEntrySerializerContext.Default.FormatterEntry);

        await store.SetAsync(key, bufferStream.ToArray(), value.Tags ?? Array.Empty<string>(), duration, cancellationToken);
    }
}
