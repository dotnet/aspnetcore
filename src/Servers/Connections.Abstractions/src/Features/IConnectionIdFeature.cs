// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Connections.Features
{
    /// <summary>
    /// The unique identifier for a given connection.
    /// </summary>
    public interface IConnectionIdFeature
    {
        /// <summary>
        /// Gets or sets the connection identifier.
        /// </summary>
        string ConnectionId { get; set; }
    }
}
