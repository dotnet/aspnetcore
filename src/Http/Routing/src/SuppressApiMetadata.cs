// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Specifies an endpoint name in <see cref="Endpoint.Metadata"/>.
    /// </summary>
    /// <remarks>
    /// Endpoint names must be unique within an application, and can be used to unambiguously
    /// identify a desired endpoint for URI generation using <see cref="LinkGenerator"/>.
    /// </remarks>
    public sealed class SuppressApiMetadata : ISuppressApiMetadata
    {
        /// <summary>
        /// Gets the endpoint name.
        /// </summary>
        public bool SuppressApi => true;
    }
}
