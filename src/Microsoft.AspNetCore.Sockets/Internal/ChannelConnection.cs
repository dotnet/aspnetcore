// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks.Channels;

namespace Microsoft.AspNetCore.Sockets.Internal
{
    public class ChannelConnection<T> : IChannelConnection<T>
    {
        public IChannel<T> Input { get; }
        public IChannel<T> Output { get; }

        IReadableChannel<T> IChannelConnection<T>.Input => Input;
        IWritableChannel<T> IChannelConnection<T>.Output => Output;

        public ChannelConnection(IChannel<T> input, IChannel<T> output)
        {
            Input = input;
            Output = output;
        }

        public void Dispose()
        {
            Output.TryComplete();
            Input.TryComplete();
        }
    }
}
