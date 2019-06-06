// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Threading.Channels
{
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
}
