using System.Collections.Generic;

namespace System.Threading.Tasks.Channels
{
    internal static class ChannelExtensions
    {
        public static async Task<List<T>> ReadAllAsync<T>(this ReadableChannel<T> channel)
        {
            var list = new List<T>();
            while (await channel.WaitToReadAsync())
            {
                while (channel.TryRead(out var item))
                {
                    list.Add(item);
                }
            }

            // Manifest any error from channel.Completion (which should be completed now)
            await channel.Completion;

            return list;
        }
    }
}
