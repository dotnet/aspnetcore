// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    /// <summary>
    /// A primary listener waits for incoming connections on a specified socket. Incoming 
    /// connections may be passed to a secondary listener to handle.
    /// </summary>
    public interface IListenerPrimary : IListener
    {
        Task StartAsync(
            string pipeName,
            string scheme,
            string host,
            int port,
            KestrelThread thread,
            Func<Frame, Task> application);
    }
}