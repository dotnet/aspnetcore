// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    public enum ApplicationType
    {
        /// <summary>
        /// Does not target a specific platform. Requires the matching runtime to be installed.
        /// </summary>
        Portable,

        /// <summary>
        /// All dlls are published with the app for x-copy deploy. Net461 requires this because ASP.NET Core is not in the GAC.
        /// </summary>
        Standalone
    }
}
