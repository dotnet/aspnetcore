// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks.Channels;

namespace Microsoft.AspNetCore.Sockets.Internal
{
    public static class ChannelConnection
    {
        public static ChannelConnection<TIn, TOut> Create<TIn, TOut>(Channel<TIn> input, Channel<TOut> output)
        {
            return new ChannelConnection<TIn, TOut>(input, output);
        }

        public static ChannelConnection<T> Create<T>(Channel<T> input, Channel<T> output)
        {
            return new ChannelConnection<T>(input, output);
        }
    }

    public class ChannelConnection<T> : ChannelConnection<T, T>, IChannelConnection<T>
    {
        public ChannelConnection(Channel<T> input, Channel<T> output)
            : base(input, output)
        { }
    }

    public class ChannelConnection<TIn, TOut> : IChannelConnection<TIn, TOut>
    {
        public Channel<TIn> Input { get; }
        public Channel<TOut> Output { get; }

        ReadableChannel<TIn> IChannelConnection<TIn, TOut>.Input => Input;
        WritableChannel<TOut> IChannelConnection<TIn, TOut>.Output => Output;

        public ChannelConnection(Channel<TIn> input, Channel<TOut> output)
        {
            Input = input;
            Output = output;
        }

        public void Dispose()
        {
            Output.Out.TryComplete();
            Input.Out.TryComplete();
        }
    }
}
