// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Connections.Features
{
    /// <summary>
    /// Represents the identifier for the stream.
    /// </summary>
    public interface IStreamIdFeature
    {
        /// <summary>
        /// Gets the stream identifier.
        /// </summary>
        long StreamId { get; }
    }
}
