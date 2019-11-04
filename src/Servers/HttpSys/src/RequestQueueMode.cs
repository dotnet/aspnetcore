// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.HttpSys
{
    /// <summary>
    /// Used to indicate if this server instance should create a new Http.Sys request queue
    /// or attach to an existing one.
    /// </summary>
    public enum RequestQueueMode
    {
        /// <summary>
        /// Create a new queue. This will fail if there's an existing queue with the same name.
        /// </summary>
        Create = 0,
        /// <summary>
        /// Attach to an existing queue with the name given. This will fail if the queue does not already exist.
        /// Most configuration options do not apply when attaching to an existing queue.
        /// </summary>
        Attach,
        /// <summary>
        /// Create a queue with the given name if it does not already exist, otherwise attach to the existing queue.
        /// Most configuration options do not apply when attaching to an existing queue.
        /// </summary>
        CreateOrAttach
    }
}
