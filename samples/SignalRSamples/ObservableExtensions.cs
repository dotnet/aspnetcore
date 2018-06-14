// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Channels;

namespace SignalRSamples
{
    public static class ObservableExtensions
    {
        public static ChannelReader<T> AsChannelReader<T>(
            this IObservable<T> observable,
            CancellationToken connectionAborted,
            int? maxBufferSize = null
        )
        {
            // This sample shows adapting an observable to a ChannelReader without 
            // back pressure, if the connection is slower than the producer, memory will
            // start to increase.

            // If the channel is bounded, TryWrite will return false and effectively
            // drop items.

            // The other alternative is to use a bounded channel, and when the limit is reached
            // block on WaitToWriteAsync. This will block a thread pool thread and isn't recommended and isn't shown here.
            var channel = maxBufferSize != null ? Channel.CreateBounded<T>(maxBufferSize.Value) : Channel.CreateUnbounded<T>();

            var disposable = observable.Subscribe(
                                value => channel.Writer.TryWrite(value),
                                error => channel.Writer.TryComplete(error),
                                () => channel.Writer.TryComplete());
            var abortRegistration = connectionAborted.Register(() => channel.Writer.TryComplete());

            // Complete the subscription on the reader completing
            channel.Reader.Completion.ContinueWith(task =>
            {
                disposable.Dispose();
                abortRegistration.Dispose();
            });

            return channel.Reader;
        }
    }
}