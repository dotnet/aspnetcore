// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Connections.Features
{
    /// <summary>
    /// The direction of a connection stream
    /// </summary>
    public interface IStreamDirectionFeature
    {
        /// <summary>
        /// Gets whether or not the connection stream can be read.
        /// </summary>
        bool CanRead { get; }

        /// <summary>
        /// Gets whether or not the connection stream can be written.
        /// </summary>
        bool CanWrite { get; }
    }
}
