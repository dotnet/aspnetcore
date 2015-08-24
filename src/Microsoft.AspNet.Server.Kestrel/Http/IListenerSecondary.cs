// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    /// <summary>
    /// A secondary listener is delegated requests from a primary listener via a named pipe or 
    /// UNIX domain socket.
    /// </summary>
    public interface IListenerSecondary : IDisposable
    {
        Task StartAsync(
            string pipeName,
            KestrelThread thread,
            Func<Frame, Task> application);
    }
}