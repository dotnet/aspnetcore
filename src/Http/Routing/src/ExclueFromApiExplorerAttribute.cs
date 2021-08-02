// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Indicates that this <see cref="Endpoint"/> should not be included in the generated API metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Delegate, AllowMultiple = false, Inherited = true)]
    public sealed class ExclueFromApiExplorerAttribute : Attribute, IExclueFromApiExplorerMetadata
    {
        /// <summary>
        /// Gets a value indicating whether API explorer 
        /// data should be excluded for this endpoint. If <see cref="true"/>,
        /// API metadata is not emitted.
        /// </summary>
        public bool ExclueFromApiExplorer => true;
    }
}
