// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    public static class ChannelReaderExtensions
    {
        /// <summary>Asynchronously reads an item from the channel.</summary>
        /// <param name="channel">The channel</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> used to cancel the read operation.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> that represents the asynchronous read operation.</returns>
        public static ValueTask<T> ReadAsync<T>(this ChannelReader<T> channel, CancellationToken cancellationToken = default)
        {
            try
            {
                return
                    cancellationToken.IsCancellationRequested
                        ? new ValueTask<T>(Task.FromCanceled<T>(cancellationToken))
                        : channel.TryRead(out T item)
                            ? new ValueTask<T>(item)
                            : ReadAsyncCore(cancellationToken);
            }
            catch (Exception e)
            {
                return new ValueTask<T>(Task.FromException<T>(e));
            }

            async ValueTask<T> ReadAsyncCore(CancellationToken ct)
            {
                while (await channel.WaitToReadAsync(ct).ConfigureAwait(false))
                {
                    if (channel.TryRead(out T item))
                    {
                        return item;
                    }
                }

                throw new ChannelClosedException();
            }
        }
    }
}
