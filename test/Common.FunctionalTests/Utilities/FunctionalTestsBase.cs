// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.Extensions.Logging.Testing;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    public class FunctionalTestsBase : LoggedTest
    {
        private const string DebugEnvironmentVariable = "ASPNETCORE_MODULE_DEBUG";

        public FunctionalTestsBase(ITestOutputHelper output = null) : base(output)
        {
        }

        protected ApplicationDeployer _deployer;

        protected ApplicationDeployer CreateDeployer(IISDeploymentParameters parameters)
        {
            if (parameters.ServerType == ServerType.IISExpress &&
                !parameters.EnvironmentVariables.ContainsKey(DebugEnvironmentVariable))
            {
                parameters.EnvironmentVariables[DebugEnvironmentVariable] = "console";
            }

            if (parameters.ApplicationPublisher == null)
            {
                throw new InvalidOperationException("All tests should use ApplicationPublisher");
            }

            return IISApplicationDeployerFactory.Create(parameters, LoggerFactory);
        }

        protected virtual async Task<IISDeploymentResult> DeployAsync(IISDeploymentParameters parameters)
        {
            _deployer = CreateDeployer(parameters);
            return (IISDeploymentResult)await _deployer.DeployAsync();
        }

        public override void Dispose()
        {
            StopServer();
        }

        public void StopServer()
        {
            _deployer?.Dispose();
            _deployer = null;
        }
    }
}
