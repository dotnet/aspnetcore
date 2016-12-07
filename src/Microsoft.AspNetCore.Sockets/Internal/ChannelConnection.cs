// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Channels;

namespace Microsoft.AspNetCore.Sockets.Internal
{
    public class ChannelConnection<T> : IChannelConnection<T>
    {
        public IReadableChannel<T> Input { get; }
        public IWritableChannel<T> Output { get; }

        public ChannelConnection(IReadableChannel<T> input, IWritableChannel<T> output)
        {
            Input = input;
            Output = output;
        }

        public void Dispose()
        {
            Output.Complete();
            (Input as IDisposable)?.Dispose();
            (Output as IDisposable)?.Dispose();
        }
    }
}
