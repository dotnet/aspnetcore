// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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

    public class ChannelConnection<T> : Channel<T>, IDisposable
    {
        public Channel<T> Input { get; }
        public Channel<T> Output { get; }

        public override ReadableChannel<T> In => Input;

        public override WritableChannel<T> Out => Output;

        public ChannelConnection(Channel<T> input, Channel<T> output)
        {
            Input = input;
            Output = output;
        }

        public void Dispose()
        {
            Input.Out.TryComplete();
            Output.Out.TryComplete();
        }
    }

    public class ChannelConnection<TIn, TOut> : Channel<TOut, TIn>, IDisposable
    {
        public Channel<TIn> Input { get; }
        public Channel<TOut> Output { get; }

        public override ReadableChannel<TIn> In => Input;

        public override WritableChannel<TOut> Out => Output;

        public ChannelConnection(Channel<TIn> input, Channel<TOut> output)
        {
            Input = input;
            Output = output;
        }

        public void Dispose()
        {
            Input.Out.TryComplete();
            Output.Out.TryComplete();
        }
    }
}
