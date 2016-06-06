// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.AspNetCore.Server.Testing
{
    /// <summary>
    /// Result of a deployment.
    /// </summary>
    public class DeploymentResult
    {
        /// <summary>
        /// Base Uri of the deployment application.
        /// </summary>
        public string ApplicationBaseUri { get; set; }

        /// <summary>
        /// The folder where the application is hosted. This path can be different from the 
        /// original application source location if published before deployment.
        /// </summary>
        public string ContentRoot { get; set; }

        /// <summary>
        /// Original deployment parameters used for this deployment.
        /// </summary>
        public DeploymentParameters DeploymentParameters { get; set; }

        /// <summary>
        /// Triggered when the host process dies or pulled down.
        /// </summary>
        public CancellationToken HostShutdownToken { get; set; }
    }
}