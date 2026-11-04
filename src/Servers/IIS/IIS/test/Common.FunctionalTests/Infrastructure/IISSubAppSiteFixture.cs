// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;

public class IISSubAppSiteFixture : IISTestSiteFixture
{
    public IISSubAppSiteFixture() : base(Configure)
    {
    }

    private static void Configure(IISDeploymentParameters deploymentParameters)
    {
        if (deploymentParameters.ServerType == IntegrationTesting.ServerType.IIS)
        {
            deploymentParameters.ServerConfigTemplateContent = File.ReadAllText("IIS.SubApp.Config");
        }
        else // IIS Express
        {
            using var stream = typeof(IISExpressDeployer).Assembly.GetManifestResourceStream("Microsoft.AspNetCore.Server.IntegrationTesting.IIS.Http.SubApp.config");
            using var reader = new StreamReader(stream);
            deploymentParameters.ServerConfigTemplateContent = reader.ReadToEnd();
        }
    }
}
