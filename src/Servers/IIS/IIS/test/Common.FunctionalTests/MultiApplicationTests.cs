// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class MultiApplicationTests : IISFunctionalTestBase
    {
        private PublishedApplication _publishedApplication;
        private PublishedApplication _rootApplication;

        public MultiApplicationTests(PublishedSitesFixture fixture) : base(fixture)
        {
        }

        [ConditionalFact]
        public async Task RunsTwoOutOfProcessApps()
        {
            var parameters = Fixture.GetBaseDeploymentParameters(HostingModel.OutOfProcess);
            parameters.ServerConfigActionList.Add(DuplicateApplication);
            var result = await DeployAsync(parameters);
            var id1 = await result.HttpClient.GetStringAsync("/app1/ProcessId");
            var id2 = await result.HttpClient.GetStringAsync("/app2/ProcessId");
            Assert.NotEqual(id2, id1);
        }

        [ConditionalFact]
        public async Task FailsAndLogsWhenRunningTwoInProcessApps()
        {
            var parameters = Fixture.GetBaseDeploymentParameters(HostingModel.InProcess);
            parameters.ServerConfigActionList.Add(DuplicateApplication);

            var result = await DeployAsync(parameters);
            var result1 = await result.HttpClient.GetAsync("/app1/HelloWorld");
            var result2 = await result.HttpClient.GetAsync("/app2/HelloWorld");
            Assert.Equal(200, (int)result1.StatusCode);
            Assert.Equal(500, (int)result2.StatusCode);
            StopServer();

            if (DeployerSelector.HasNewShim)
            {
                Assert.Contains("500.35", await result2.Content.ReadAsStringAsync());
            }

            EventLogHelpers.VerifyEventLogEvent(result, EventLogHelpers.OnlyOneAppPerAppPool(), Logger);
        }

        [ConditionalTheory]
        [InlineData(HostingModel.OutOfProcess)]
        [InlineData(HostingModel.InProcess)]
        public async Task FailsAndLogsEventLogForMixedHostingModel(HostingModel firstApp)
        {
            var parameters = Fixture.GetBaseDeploymentParameters(firstApp);
            parameters.ServerConfigActionList.Add(DuplicateApplication);
            var result = await DeployAsync(parameters);

            // Modify hosting model of other app to be the opposite
            var otherApp = firstApp == HostingModel.InProcess ? HostingModel.OutOfProcess : HostingModel.InProcess;
            SetHostingModel(_publishedApplication.Path, otherApp);

            var result1 = await result.HttpClient.GetAsync("/app1/HelloWorld");
            var result2 = await result.HttpClient.GetAsync("/app2/HelloWorld");
            Assert.Equal(200, (int)result1.StatusCode);
            Assert.Equal(500, (int)result2.StatusCode);
            StopServer();

            if (DeployerSelector.HasNewShim)
            {
                Assert.Contains("500.34", await result2.Content.ReadAsStringAsync());
            }

            EventLogHelpers.VerifyEventLogEvent(result, "Mixed hosting model is not supported.", Logger);
        }

        private void SetHostingModel(string directory, HostingModel model)
        {
            var webConfigLocation = GetWebConfigLocation(directory);
            XDocument webConfig = XDocument.Load(webConfigLocation);
            webConfig.Root
                .Descendants("system.webServer")
                .Single()
                .GetOrAdd("aspNetCore")
                .SetAttributeValue("hostingModel", model.ToString());
            webConfig.Save(webConfigLocation);
        }

        private void DuplicateApplication(XElement config, string contentRoot)
        {
            var siteElement = config
                .RequiredElement("system.applicationHost")
                .RequiredElement("sites")
                .RequiredElement("site");

            var application = siteElement
                .RequiredElement("application");

            application.SetAttributeValue("path", "/app1");

            var source = new DirectoryInfo(contentRoot);

            var destination = new DirectoryInfo(contentRoot + "anotherApp");
            destination.Create();
            Helpers.CopyFiles(source, destination, Logger);

            _publishedApplication = new PublishedApplication(destination.FullName, Logger);

            var newApplication = new XElement(application);
            newApplication.SetAttributeValue("path", "/app2");
            newApplication.RequiredElement("virtualDirectory")
                .SetAttributeValue("physicalPath", destination.FullName);

            siteElement.Add(newApplication);

            // IIS Express requires root application to exist

            _rootApplication = new PublishedApplication(Helpers.CreateEmptyApplication(config, contentRoot), Logger);
        }

        private static string GetWebConfigLocation(string siteRoot)
        {
            return Path.Combine(siteRoot, "web.config");
        }

        public override void Dispose()
        {
            base.Dispose();
            _rootApplication.Dispose();
            _publishedApplication.Dispose();
        }
    }
}
