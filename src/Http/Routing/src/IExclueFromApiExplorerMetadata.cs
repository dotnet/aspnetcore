// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Indicates wheter or not that API explorer data should be emitted for this endpoint.
    /// </summary>
    public interface IExclueFromApiExplorerMetadata
    {
        /// <summary>
        /// Gets a value indicating whether API explorer 
        /// data should be emitted for this endpoint. 
        /// </summary>
        bool ExclueFromApiExplorer { get; }
    }
}
