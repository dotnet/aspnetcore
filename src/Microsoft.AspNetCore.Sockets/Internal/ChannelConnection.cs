// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks.Channels;

namespace Microsoft.AspNetCore.Sockets.Internal
{
    public static class ChannelConnection
    {
        public static ChannelConnection<T> Create<T>(Channel<T> input, Channel<T> output)
        {
            return new ChannelConnection<T>(input, output);
        }
    }

    public class ChannelConnection<T> : IChannelConnection<T>
    {
        public Channel<T> Input { get; }
        public Channel<T> Output { get; }

        ReadableChannel<T> IChannelConnection<T>.Input => Input;
        WritableChannel<T> IChannelConnection<T>.Output => Output;

        public ChannelConnection(Channel<T> input, Channel<T> output)
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
