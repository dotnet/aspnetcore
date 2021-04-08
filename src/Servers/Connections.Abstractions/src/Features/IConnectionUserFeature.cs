// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;

namespace Microsoft.AspNetCore.Connections.Features
{
    /// <summary>
    /// The user associated with the connection.
    /// </summary>
    public interface IConnectionUserFeature
    {
        /// <summary>
        /// Gets or sets the user associated with the connection.
        /// </summary>
        ClaimsPrincipal? User { get; set; }
    }
}
