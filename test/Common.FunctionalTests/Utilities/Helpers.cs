// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public static class Helpers
    {
        public static string GetTestWebSitePath(string name)
        {
            return Path.Combine(TestPathUtilities.GetSolutionRootDirectory("IISIntegration"),"test", "WebSites", name);
        }

        public static string GetInProcessTestSitesPath() => GetTestWebSitePath("InProcessWebSite");

        public static string GetOutOfProcessTestSitesPath() => GetTestWebSitePath("OutOfProcessWebSite");
        
        // Defaults to inprocess specific deployment parameters
        public static IISDeploymentParameters GetBaseDeploymentParameters(string site = null, HostingModel hostingModel = HostingModel.InProcess, bool publish = false)
        {
            if (site == null)
            {
                site = hostingModel == HostingModel.InProcess ? "InProcessWebSite" : "OutOfProcessWebSite";
            }

            return new IISDeploymentParameters(GetTestWebSitePath(site), DeployerSelector.ServerType, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64)
            {
                TargetFramework = Tfm.NetCoreApp22,
                ApplicationType = ApplicationType.Portable,
                AncmVersion = AncmVersion.AspNetCoreModuleV2,
                HostingModel = hostingModel,
                PublishApplicationBeforeDeployment = publish,
            };
        }

        public static async Task AssertStarts(IISDeploymentResult deploymentResult, string path = "/HelloWorld")
        {
            var response = await deploymentResult.RetryingHttpClient.GetAsync(path);

            var responseText = await response.Content.ReadAsStringAsync();

            Assert.Equal("Hello World", responseText);
        }
    }
}
