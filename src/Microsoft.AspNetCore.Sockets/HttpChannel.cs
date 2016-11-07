// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Channels;

namespace Microsoft.AspNetCore.Sockets
{
    public class HttpChannel : IChannel
    {
        public HttpChannel(ChannelFactory factory)
        {
            Input = factory.CreateChannel();
            Output = factory.CreateChannel();
        }

        IReadableChannel IChannel.Input => Input;

        IWritableChannel IChannel.Output => Output;

        public Channel Input { get; }

        public Channel Output { get; }

        public void Dispose()
        {
            Input.CompleteReader();
            Input.CompleteWriter();

            Output.CompleteReader();
            Output.CompleteWriter();
        }
    }
}
