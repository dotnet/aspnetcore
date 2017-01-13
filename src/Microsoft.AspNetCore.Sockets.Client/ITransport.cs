// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Sockets.Client
{
    public interface ITransport : IDisposable
    {
        Task StartAsync(Uri url, IChannelConnection<Message> application);
    }
}
