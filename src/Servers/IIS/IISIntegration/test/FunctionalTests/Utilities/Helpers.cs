// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Server.IntegrationTesting;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class Helpers
    {
        public static string GetTestWebSitePath(string name)
        {
            return Path.GetFullPath(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    "..", // tfm
                    "..", // debug
                    "..", // obj
                    "..", // projectfolder
                    "IIS",
                    "IISIntegration",
                    "test",
                    "testassets",
                    name));
        }

        public static string GetOutOfProcessTestSitesPath() => GetTestWebSitePath("OutOfProcessWebSite");

        public static void ModifyAspNetCoreSectionInWebConfig(DeploymentResult deploymentResult, string key, string value)
        {
            // modify the web.config after publish
            var root = deploymentResult.ContentRoot;
            var webConfigFile = $"{root}/web.config";
            var config = XDocument.Load(webConfigFile);
            var element = config.Descendants("aspNetCore").FirstOrDefault();
            element.SetAttributeValue(key, value);
            config.Save(webConfigFile);
        }
    }
}
