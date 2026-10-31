// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// This is an internal API that supports the Microsoft.AspNetCore.Components.WebAssembly.Authentication
/// infrastructure and not subject to the same compatibility standards as public APIs.
/// It may be changed or removed without notice in any release.
/// </summary>
internal interface IRemoteAuthenticationPathsProvider
{
    /// <summary>
    /// This is an internal API that supports the Microsoft.AspNetCore.Components.WebAssembly.Authentication
    /// infrastructure and not subject to the same compatibility standards as public APIs.
    /// It may be changed or removed without notice in any release.
    /// </summary>
    RemoteAuthenticationApplicationPathsOptions ApplicationPaths { get; }
}
