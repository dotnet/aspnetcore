// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reactive.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace SignalRSamples.Hubs
{
    public class Streaming : Hub
    {
        public ChannelReader<int> ObservableCounter(int count, int delay)
        {
            var observable = Observable.Interval(TimeSpan.FromMilliseconds(delay))
                             .Select((_, index) => index)
                             .Take(count);

            return observable.AsChannelReader(Context.ConnectionAborted);
        }

        public ChannelReader<int> ChannelCounter(int count, int delay)
        {
            var channel = Channel.CreateUnbounded<int>();

            Task.Run(async () =>
            {
                for (var i = 0; i < count; i++)
                {
                    await channel.Writer.WriteAsync(i);
                    await Task.Delay(delay);
                }

                channel.Writer.TryComplete();
            });

            return channel.Reader;
        }
    }
}
