// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.IntegrationTesting;

public enum ApplicationType
{
    /// <summary>
    /// Does not target a specific platform. Requires the matching runtime to be installed.
    /// </summary>
    Portable,

    /// <summary>
    /// All dlls are published with the app for x-copy deploy. Net462 requires this because ASP.NET Core is not in the GAC.
    /// </summary>
    Standalone
}
