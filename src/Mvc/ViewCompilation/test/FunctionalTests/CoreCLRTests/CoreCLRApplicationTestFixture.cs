// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.IntegrationTesting;

namespace FunctionalTests
{
    public class CoreCLRApplicationTestFixture<TStartup> : ApplicationTestFixture
    {
        private const string TargetFramework = "netcoreapp2.1";

        public CoreCLRApplicationTestFixture()
            : this(typeof(TStartup).Assembly.GetName().Name, null)
        {
        }

        protected CoreCLRApplicationTestFixture(string applicationName, string applicationPath)
            : base(applicationName, applicationPath)
        {
        }

        protected override DeploymentParameters GetDeploymentParameters() => base.GetDeploymentParameters(RuntimeFlavor.CoreClr, TargetFramework);
    }
}
