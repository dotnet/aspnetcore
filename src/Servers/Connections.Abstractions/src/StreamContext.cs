// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Connections
{
    public abstract class StreamContext : ConnectionContext
    {
        /// <summary>
        /// Gets the id assigned to the stream.
        /// </summary>
        public abstract string StreamId { get; }
    }
}
