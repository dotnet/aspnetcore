// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.IntegrationTesting;

namespace FunctionalTests
{
    public class DesktopApplicationTestFixture<TStartup> : ApplicationTestFixture
    {
        public DesktopApplicationTestFixture()
            : this(typeof(TStartup).Assembly.GetName().Name, null)
        {
        }

        protected DesktopApplicationTestFixture(string applicationName, string applicationPath)
            : base(applicationName, applicationPath)
        {
        }

        protected override DeploymentParameters GetDeploymentParameters() => base.GetDeploymentParameters(RuntimeFlavor.Clr, "net461");
    }
}
