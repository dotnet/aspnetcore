// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Indicates that API explorer data should not be emitted for this endpoint.
    /// </summary>
    public sealed class SuppressApiMetadata : ISuppressApiMetadata
    {
        /// <summary>
        /// Gets a value indicating whether API explorer 
        /// data should be emitted for this endpoint. 
        /// </summary>
        public bool SuppressApi => true;
    }
}
