// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class Helpers
    {
        public static TimeSpan DefaultTimeout = TimeSpan.FromSeconds(3);

        public static string GetTestWebSitePath(string name)
        {
            return Path.Combine(TestPathUtilities.GetSolutionRootDirectory("IISIntegration"),"test", "WebSites", name);
        }

        public static string GetInProcessTestSitesPath() => GetTestWebSitePath("InProcessWebSite");

        public static string GetOutOfProcessTestSitesPath() => GetTestWebSitePath("OutOfProcessWebSite");

        public static void ModifyAspNetCoreSectionInWebConfig(IISDeploymentResult deploymentResult, string key, string value)
            => ModifyAttributeInWebConfig(deploymentResult, key, value, section: "aspNetCore");

        public static void ModifyAttributeInWebConfig(IISDeploymentResult deploymentResult, string key, string value, string section)
        {
            var webConfigFile = GetWebConfigFile(deploymentResult);
            var config = XDocument.Load(webConfigFile);

            var element = config.Descendants(section).Single();
            element.SetAttributeValue(key, value);

            config.Save(webConfigFile);
        }

        public static void ModifyEnvironmentVariableCollectionInWebConfig(IISDeploymentResult deploymentResult, string key, string value)
        {
            var webConfigFile = GetWebConfigFile(deploymentResult);
            var config = XDocument.Load(webConfigFile);

            var envVarElement = new XElement("environmentVariable");
            envVarElement.SetAttributeValue("name", key);
            envVarElement.SetAttributeValue("value", value);

            config.Descendants("aspNetCore").Single()
                .Descendants("environmentVariables").Single()
                .Add(envVarElement);

            config.Save(webConfigFile);
        }

        public static void ModifyHandlerSectionInWebConfig(IISDeploymentResult deploymentResult, string handlerVersionValue)
        {
            var webConfigFile = GetWebConfigFile(deploymentResult);
            var config = XDocument.Load(webConfigFile);

            var handlerVersionElement = new XElement("handlerSetting");
            handlerVersionElement.SetAttributeValue("name", "handlerVersion");
            handlerVersionElement.SetAttributeValue("value", handlerVersionValue);

            config.Descendants("aspNetCore").Single()
                .Add(new XElement("handlerSettings", handlerVersionElement));

            config.Save(webConfigFile);
        }

        public static void AddDebugLogToWebConfig(string contentRoot, string filename)
        {
            var path = Path.Combine(contentRoot, "web.config");
            var webconfig = XDocument.Load(path);
            var xElement = webconfig.Descendants("aspNetCore").Single();

            var element = xElement.Descendants("handlerSettings").SingleOrDefault();
            if (element == null)
            {
                element = new XElement("handlerSettings");
                xElement.Add(element);
            }

            CreateOrSetElement(element, "debugLevel", "4", "handlerSetting");

            CreateOrSetElement(element, "debugFile", Path.Combine(contentRoot, filename), "handlerSetting");

            webconfig.Save(path);
        }

        public static void AddEnvironmentVariablesToWebConfig(string contentRoot, IDictionary<string, string> environmentVariables)
        {
            var path = Path.Combine(contentRoot, "web.config");
            var webconfig = XDocument.Load(path);
            var xElement = webconfig.Descendants("aspNetCore").Single();

            var element = xElement.Descendants("environmentVariables").SingleOrDefault();
            if (element == null)
            {
                element = new XElement("environmentVariables");
                xElement.Add(element);
            }

            foreach (var envVar in environmentVariables)
            {
                CreateOrSetElement(element, envVar.Key, envVar.Value, "environmentVariable");
            }

            webconfig.Save(path);
        }

        public static void CreateOrSetElement(XElement rootElement, string name, string value, string elementName)
        {
            if (rootElement.Descendants()
                .Attributes()
                .Where(attribute => attribute.Value == name)
                .Any())
            {
                return;
            }
            var element = new XElement(elementName);
            element.SetAttributeValue("name", name);
            element.SetAttributeValue("value", value);
            rootElement.Add(element);
        }

        // Defaults to inprocess specific deployment parameters
        public static DeploymentParameters GetBaseDeploymentParameters(string site = null, HostingModel hostingModel = HostingModel.InProcess, bool publish = false)
        {
            if (site == null)
            {
                site = hostingModel == HostingModel.InProcess ? "InProcessWebSite" : "OutOfProcessWebSite";
            }

            return new DeploymentParameters(GetTestWebSitePath(site), DeployerSelector.ServerType, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64)
            {
                TargetFramework = Tfm.NetCoreApp22,
                ApplicationType = ApplicationType.Portable,
                AncmVersion = AncmVersion.AspNetCoreModuleV2,
                HostingModel = hostingModel,
                PublishApplicationBeforeDeployment = publish,
            };
        }

        private static string GetWebConfigFile(IISDeploymentResult deploymentResult)
            => Path.Combine(deploymentResult.DeploymentResult.ContentRoot, "web.config");

        public static async Task AssertStarts(IISDeploymentResult deploymentResult, string path = "/HelloWorld")
        {
            var response = await deploymentResult.RetryingHttpClient.GetAsync(path);

            var responseText = await response.Content.ReadAsStringAsync();

            Assert.Equal("Hello World", responseText);
        }
    }
}
