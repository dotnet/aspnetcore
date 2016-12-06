// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Hosting
{
    /// <summary>
    /// Defines methods for objects that are managed by the host.
    /// </summary>
    public interface IHostedService
    {
        /// <summary>
        /// Triggered when the application host has fully started and the server is waiting
        /// for requests.
        /// </summary>
        void Start();

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// Requests may still be in flight. Shutdown will block until this event completes.
        /// </summary>
        void Stop();
    }
}
