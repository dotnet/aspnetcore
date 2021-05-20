// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Connections.Features
{
    /// <summary>
    /// The error code for the protocol being used.
    /// </summary>
    public interface IProtocolErrorCodeFeature
    {
        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        long Error { get; set; }
    }
}
