// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
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

        private ApplicationDeployer _deployer;

        protected virtual async Task<IISDeploymentResult> DeployAsync(DeploymentParameters parameters)
        {
            if (!parameters.EnvironmentVariables.ContainsKey(DebugEnvironmentVariable))
            {
                parameters.EnvironmentVariables[DebugEnvironmentVariable] = "4";
            }

            if (parameters.ServerType == ServerType.IISExpress)
            {
                parameters.ServerConfigTemplateContent = parameters.ServerConfigTemplateContent ?? File.ReadAllText("IISExpress.config");
            }

            _deployer = IISApplicationDeployerFactory.Create(parameters, LoggerFactory);

            var result = await _deployer.DeployAsync();

            return new IISDeploymentResult(result, Logger);
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
