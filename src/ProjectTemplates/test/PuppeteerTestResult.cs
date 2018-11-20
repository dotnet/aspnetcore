// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Server.IntegrationTesting;

namespace Templates.Test
{
    public readonly struct PuppeteerTestResult : IDisposable
    {
        public PuppeteerTestResult(ApplicationDeployer deployer, DeploymentResult result)
        {
            Deployer = deployer;
            Result = result;
        }

        public ApplicationDeployer Deployer { get; }

        public DeploymentResult Result { get; }

        public void Dispose()
        {
            Deployer.Dispose();
        }
    }
}
