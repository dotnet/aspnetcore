// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.HttpSys
{
    public enum RequestQueueMode
    {
        /// <summary>
        /// Create a new queue. This will fail if there's an existing queue with the same name.
        /// </summary>
        Create = 0,
        /// <summary>
        /// Attach to an existing queue with the name given. This will fail if the queue does not already exist.
        /// Most configuration options are ignored when attaching to an existing queue.
        /// </summary>
        AttachToExisting,
        /// <summary>
        /// Attach to an existing queue with the given name if it exists, otherwise create it.
        /// </summary>
        AttachOrCreate
    }
}
