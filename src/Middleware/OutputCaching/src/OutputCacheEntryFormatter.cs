// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using Microsoft.AspNetCore.OutputCaching.Serialization;

namespace Microsoft.AspNetCore.OutputCaching;
/// <summary>
/// Formats <see cref="OutputCacheEntry"/> instance to match structures supported by the <see cref="IOutputCacheStore"/> implementations.
/// </summary>
internal static class OutputCacheEntryFormatter
{
    private const byte SerializationRevision = 1;

    public static async ValueTask<OutputCacheEntry?> GetAsync(string key, IOutputCacheStore store, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);

        var content = await store.GetAsync(key, cancellationToken);

        if (content == null)
        {
            return null;
        }

        var formatter = Deserialize(new MemoryStream(content));

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

        Serialize(bufferStream, formatterEntry);

        await store.SetAsync(key, bufferStream.ToArray(), value.Tags ?? Array.Empty<string>(), duration, cancellationToken);
    }

    // Format:
    // Serialization revision:
    //   7-bit encoded int
    // Creation date:
    //   Ticks: 7-bit encoded long
    //   Offset.TotalMinutes: 7-bit encoded long
    // Status code:
    //   7-bit encoded int
    // Headers:
    //   Headers count: 7-bit encoded int
    //   For each header:
    //     key name byte length: 7-bit encoded int
    //     UTF-8 encoded key name byte[]
    //     Values count: 7-bit encoded int
    //     For each header value:
    //       data byte length: 7-bit encoded int
    //       UTF-8 encoded byte[]
    // Body:
    //   Segments count: 7-bit encoded int
    //   For each segment:
    //     data byte length: 7-bit encoded int
    //     data byte[]
    // Tags:
    //   Tags count: 7-bit encoded int
    //   For each tag:
    //     data byte length: 7-bit encoded int
    //     UTF-8 encoded byte[]

    private static void Serialize(Stream output, FormatterEntry entry)
    {
        using var writer = new BinaryWriter(output);

        // Serialization revision:
        //   7-bit encoded int
        writer.Write7BitEncodedInt(SerializationRevision);

        // Creation date:
        //   Ticks: 7-bit encoded long
        //   Offset.TotalMinutes: 7-bit encoded long

        writer.Write7BitEncodedInt64(entry.Created.Ticks);
        writer.Write7BitEncodedInt64((long)entry.Created.Offset.TotalMinutes);

        // Status code:
        //   7-bit encoded int
        writer.Write7BitEncodedInt(entry.StatusCode);

        // Headers:
        //   Headers count: 7-bit encoded int

        writer.Write7BitEncodedInt(entry.Headers.Count);

        //   For each header:
        //     key name byte length: 7-bit encoded int
        //     UTF-8 encoded key name byte[]

        foreach (var header in entry.Headers)
        {
            writer.Write(header.Key);

            //     Values count: 7-bit encoded int

            if (header.Value == null)
            {
                writer.Write7BitEncodedInt(0);
                continue;
            }
            else
            {
                writer.Write7BitEncodedInt(header.Value.Length);
            }

            //     For each header value:
            //       data byte length: 7-bit encoded int
            //       UTF-8 encoded byte[]

            foreach (var value in header.Value)
            {
                writer.Write(value ?? "");
            }
        }

        // Body:
        //   Segments count: 7-bit encoded int
        //   For each segment:
        //     data byte length: 7-bit encoded int
        //     data byte[]

        writer.Write7BitEncodedInt(entry.Body.Count);

        foreach (var segment in entry.Body)
        {
            writer.Write7BitEncodedInt(segment.Length);
            writer.Write(segment);
        }

        // Tags:
        //   Tags count: 7-bit encoded int
        //   For each tag:
        //     data byte length: 7-bit encoded int
        //     UTF-8 encoded byte[]

        writer.Write7BitEncodedInt(entry.Tags.Length);

        foreach (var tag in entry.Tags)
        {
            writer.Write(tag ?? "");
        }
    }

    private static FormatterEntry? Deserialize(Stream content)
    {
        using var reader = new BinaryReader(content);

        // Serialization revision:
        //   7-bit encoded int

        var revision = reader.Read7BitEncodedInt();

        if (revision != SerializationRevision)
        {
            // In future versions, also support the previous revision format.

            return null;
        }

        var result = new FormatterEntry();

        // Creation date:
        //   Ticks: 7-bit encoded long
        //   Offset.TotalMinutes: 7-bit encoded long

        var ticks = reader.Read7BitEncodedInt64();
        var offsetMinutes = reader.Read7BitEncodedInt64();

        result.Created = new DateTimeOffset(ticks, TimeSpan.FromMinutes(offsetMinutes));

        // Status code:
        //   7-bit encoded int

        result.StatusCode = reader.Read7BitEncodedInt();

        // Headers:
        //   Headers count: 7-bit encoded int

        var headersCount = reader.Read7BitEncodedInt();

        //   For each header:
        //     key name byte length: 7-bit encoded int
        //     UTF-8 encoded key name byte[]
        //     Values count: 7-bit encoded int

        result.Headers = new Dictionary<string, string?[]>(headersCount);

        for (var i = 0; i < headersCount; i++)
        {
            var key = reader.ReadString();

            var valuesCount = reader.Read7BitEncodedInt();

            //     For each header value:
            //       data byte length: 7-bit encoded int
            //       UTF-8 encoded byte[]

            var values = new string[valuesCount];

            for (var j = 0; j < valuesCount; j++)
            {
                values[j] = reader.ReadString();
            }

            result.Headers[key] = values;
        }

        // Body:
        //   Segments count: 7-bit encoded int

        var segmentsCount = reader.Read7BitEncodedInt();

        //   For each segment:
        //     data byte length: 7-bit encoded int
        //     data byte[]

        var segments = new List<byte[]>(segmentsCount);

        for (var i = 0; i < segmentsCount; i++)
        {
            var segmentLength = reader.Read7BitEncodedInt();
            var segment = reader.ReadBytes(segmentLength);

            segments.Add(segment);
        }

        result.Body = segments;

        // Tags:
        //   Tags count: 7-bit encoded int

        var tagsCount = reader.Read7BitEncodedInt();

        //   For each tag:
        //     data byte length: 7-bit encoded int
        //     UTF-8 encoded byte[]

        var tags = new string[tagsCount];

        for (var i = 0; i < tagsCount; i++)
        {
            var tagLength = reader.Read7BitEncodedInt();
            var tagData = reader.ReadBytes(tagLength);
            var tag = Encoding.UTF8.GetString(tagData);

            tags[i] = tag;
        }

        result.Tags = tags;
        return result;
    }
}
