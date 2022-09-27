// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
}
