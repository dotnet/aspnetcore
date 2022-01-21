// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Hosting.Server;

/// <summary>
/// Used by servers to advertise if they support integrated Windows authentication, if it's enabled, and it's scheme.
/// </summary>
public interface IServerIntegratedAuth
{
    /// <summary>
    /// Indicates if integrated Windows authentication is enabled for the current application instance.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// The name of the authentication scheme for the server authentication handler.
    /// </summary>
    string AuthenticationScheme { get; }
}
