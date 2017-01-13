// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks.Channels;

namespace Microsoft.AspNetCore.Sockets
{
    // REVIEW: These should probably move to Channels. Why not use IChannel? Because I think it's better to be clear that this is providing
    // access to two separate channels, the read end for one and the write end for the other.
    public interface IChannelConnection<T> : IDisposable
    {
        ReadableChannel<T> Input { get; }
        WritableChannel<T> Output { get; }
    }
}
