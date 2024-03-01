// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.SpaProxy
{
    internal enum SpaDevelopmentServerStartStrategy
    {
        /// <summary>
        /// Start another process, leave any existing process running
        /// </summary>
        ReLaunch,
        /// <summary>
        /// Kill any existing process before starting a new one
        /// </summary>
        Restart,
        /// <summary>
        /// If a process is running already, assume it is active and don't start another instance
        /// </summary>
        Wait
    }
}
