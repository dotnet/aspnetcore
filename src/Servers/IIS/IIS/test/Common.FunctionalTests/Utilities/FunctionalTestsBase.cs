// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing;
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

        protected IISDeployerBase _deployer;

        protected ApplicationDeployer CreateDeployer(IISDeploymentParameters parameters)
        {
            if (parameters.ServerType == ServerType.IISExpress &&
                !parameters.EnvironmentVariables.ContainsKey(DebugEnvironmentVariable))
            {
                parameters.EnvironmentVariables[DebugEnvironmentVariable] = "console";
            }

            return IISApplicationDeployerFactory.Create(parameters, LoggerFactory);
        }

        protected virtual async Task<IISDeploymentResult> DeployAsync(IISDeploymentParameters parameters)
        {
            _deployer = (IISDeployerBase)CreateDeployer(parameters);
            return (IISDeploymentResult)await _deployer.DeployAsync();
        }

        protected virtual async Task<IISDeploymentResult> StartAsync(IISDeploymentParameters parameters)
        {
            var result = await DeployAsync(parameters);
            await result.AssertStarts();
            return result;
        }

        protected virtual async Task<string> GetStringAsync(IISDeploymentParameters parameters, string path)
        {
            var result = await DeployAsync(parameters);
            return await result.HttpClient.GetStringAsync(path);
        }

        public override void Dispose()
        {
            StopServer(false);
        }

        public void StopServer(bool gracefulShutdown = true)
        {
            _deployer?.Dispose(gracefulShutdown);
            _deployer = null;
        }
    }
}
