// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal abstract class RequestDispatcher
    {
        /// <summary>
        /// Default time the server will stay alive after the last request disconnects.
        /// </summary>
        public static readonly TimeSpan DefaultServerKeepAlive = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Time to delay after the last connection before initiating a garbage collection
        /// in the server.
        /// </summary>
        public static readonly TimeSpan GCTimeout = TimeSpan.FromSeconds(30);

        public abstract void Run();

        public static RequestDispatcher Create(ConnectionHost connectionHost, CompilerHost compilerHost, CancellationToken cancellationToken, EventBus eventBus, TimeSpan? keepAlive = null)
        {
            return new DefaultRequestDispatcher(connectionHost, compilerHost, cancellationToken, eventBus, keepAlive);
        }
    }
}
