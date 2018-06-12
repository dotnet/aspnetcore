// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class Helpers
    {
        public static string GetTestWebSitePath(string name)
        {
            return Path.Combine(TestPathUtilities.GetSolutionRootDirectory("IISIntegration"),"test", "WebSites", name);
        }

        public static string GetInProcessTestSitesPath() => GetTestWebSitePath("InProcessWebSite");

        public static string GetOutOfProcessTestSitesPath() => GetTestWebSitePath("OutOfProcessWebSite");

        public static void ModifyAspNetCoreSectionInWebConfig(IISDeploymentResult deploymentResult, string key, string value)
            => ModifySectionInWebConfig(deploymentResult, key, value, section: "aspNetCore", 0);

        public static void ModifySectionInWebConfig(IISDeploymentResult deploymentResult, string key, string value, string section, int index)
        {
            // modify the web.config after publish
            var root = deploymentResult.DeploymentResult.ContentRoot;
            var webConfigFile = $"{root}/web.config";
            var config = XDocument.Load(webConfigFile);
            var element = config.Descendants(section).ToList()[index];
            element.SetAttributeValue(key, value);
            config.Save(webConfigFile);
        }

        // Defaults to inprocess specific deployment parameters
        public static DeploymentParameters GetBaseDeploymentParameters(string site = "InProcessWebSite")
        {
            return new DeploymentParameters(Helpers.GetTestWebSitePath(site), ServerType.IISExpress, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64)
            {
                TargetFramework = Tfm.NetCoreApp22,
                ApplicationType = ApplicationType.Portable,
                AncmVersion = AncmVersion.AspNetCoreModuleV2,
                HostingModel = HostingModel.InProcess,
                PublishApplicationBeforeDeployment = site == "InProcessWebSite",
            };
        }
    }
}
