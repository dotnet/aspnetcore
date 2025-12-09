// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading.Channels;

public static class ChannelExtensions
{
    public static async Task<List<T>> ReadAndCollectAllAsync<T>(this ChannelReader<T> channel, bool suppressExceptions = false)
    {
        var list = new List<T>();
        try
        {
            while (await channel.WaitToReadAsync())
            {
                while (channel.TryRead(out var item))
                {
                    list.Add(item);
                }
            }

            // Manifest any error from channel.Completion (which should be completed now)
            if (!suppressExceptions)
            {
                await channel.Completion;
            }
        }
        catch (Exception) when (suppressExceptions)
        {
            // Suppress the exception
        }

        return list;
    }

    public static async Task<List<T>> ReadAtLeastAsync<T>(this ChannelReader<T> reader, int minimumCount, CancellationToken cancellationToken = default)
    {
        if (minimumCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumCount), "minimumCount must be greater than zero.");
        }

        var items = new List<T>();

        while (items.Count < minimumCount)
        {
            while (reader.TryRead(out var item))
            {
                items.Add(item);
                if (items.Count >= minimumCount)
                {
                    return items;
                }
            }

            try
            {
                var readTask = reader.WaitToReadAsync(cancellationToken).AsTask();
                if (!await readTask.ConfigureAwait(false))
                {
                    throw new InvalidOperationException($"Channel ended after writing {items.Count} items.");
                }
            }
            catch (OperationCanceledException)
            {
                throw new OperationCanceledException($"ReadAtLeastAsync canceled with {items.Count} of {minimumCount} items.");
            }
        }

        return items;
    }
}
