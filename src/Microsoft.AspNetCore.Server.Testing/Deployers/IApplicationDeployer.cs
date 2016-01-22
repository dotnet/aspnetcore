// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Server.Testing
{
    /// <summary>
    /// Common operations on an application deployer.
    /// </summary>
    public interface IApplicationDeployer : IDisposable
    {
        /// <summary>
        /// Deploys the application to the target with specified <see cref="DeploymentParameters"/>.
        /// </summary>
        /// <returns></returns>
        DeploymentResult Deploy();
    }
}