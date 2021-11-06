// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;

namespace Microsoft.AspNetCore.Connections.Features;

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
