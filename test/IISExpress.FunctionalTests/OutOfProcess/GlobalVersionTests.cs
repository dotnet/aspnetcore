// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    public class GlobalVersionTests : IISFunctionalTestBase
    {
        private const string _aspNetCoreDll = "aspnetcorev2_outofprocess.dll";
        private const string _handlerVersion20 = "2.0.0";
        private const string _helloWorldRequest = "HelloWorld";
        private const string _helloWorldResponse = "Hello World";
        private const string _outOfProcessVersionVariable = "/p:AspNetCoreModuleOutOfProcessVersion=";

        [ConditionalFact]
        public async Task GlobalVersion_DefaultWorks()
        {
            var deploymentParameters = GetGlobalVersionBaseDeploymentParameters();
            deploymentParameters.PublishApplicationBeforeDeployment = false;

            deploymentParameters.ServerConfigTemplateContent = GetServerConfig(
                element =>
                {
                    var handlerVersionElement = new XElement("handlerSetting");
                    handlerVersionElement.SetAttributeValue("name", "handlerVersion");
                    handlerVersionElement.SetAttributeValue("value", _handlerVersion20);

                    element.Descendants("aspNetCore").Single()
                        .Add(new XElement("handlerSettings", handlerVersionElement));
                });

            var deploymentResult = await DeployAsync(deploymentParameters);

            var response = await deploymentResult.RetryingHttpClient.GetAsync(_helloWorldRequest);
            var responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(_helloWorldResponse, responseText);
        }

        [ConditionalTheory]
        [InlineData("2.1.0")]
        [InlineData("2.1.0-preview")]
        public async Task GlobalVersion_NewVersionNumber_Fails(string version)
        {
            var deploymentParameters = GetGlobalVersionBaseDeploymentParameters();

            var deploymentResult = await DeployAsync(deploymentParameters);

            Helpers.ModifyHandlerSectionInWebConfig(deploymentResult, version);

            var response = await deploymentResult.RetryingHttpClient.GetAsync(_helloWorldRequest);
            Assert.False(response.IsSuccessStatusCode);
        }

        [ConditionalTheory]
        [InlineData("2.1.0")]
        [InlineData("2.1.0-preview")]
        public async Task GlobalVersion_NewVersionNumber(string version)
        {
            var deploymentParameters = GetGlobalVersionBaseDeploymentParameters();
            deploymentParameters.AdditionalPublishParameters = $"{_outOfProcessVersionVariable}{version}";

            var deploymentResult = await DeployAsync(deploymentParameters);

            Helpers.ModifyHandlerSectionInWebConfig(deploymentResult, version);

            var response = await deploymentResult.RetryingHttpClient.GetAsync(_helloWorldRequest);
            var responseText = await response.Content.ReadAsStringAsync();
            Assert.Equal(_helloWorldResponse, responseText);
            AssertLoadedVersion(version);
        }

        [ConditionalTheory]
        [InlineData("2.1.0")]
        [InlineData("2.1.0-preview")]
        public async Task GlobalVersion_MultipleRequestHandlers_PicksHighestOne(string version)
        {
            var deploymentParameters = GetGlobalVersionBaseDeploymentParameters();

            var deploymentResult = await DeployAsync(deploymentParameters);

            var originalANCMPath = GetANCMRequestHandlerPath(deploymentResult, _handlerVersion20);

            var newANCMPath = GetANCMRequestHandlerPath(deploymentResult, version);

            var di = Directory.CreateDirectory(Path.GetDirectoryName(newANCMPath));

            File.Copy(originalANCMPath, newANCMPath, true);

            deploymentResult.RetryingHttpClient.DefaultRequestHeaders.Add("ANCMRHPath", newANCMPath);
            var response = await deploymentResult.RetryingHttpClient.GetAsync("CheckRequestHandlerVersion");
            var responseText = await response.Content.ReadAsStringAsync();

            Assert.Equal(_helloWorldResponse, responseText);
            AssertLoadedVersion(version);
        }

        [ConditionalTheory]
        [InlineData("2.1.0")]
        [InlineData("2.1.0-preview")]
        public async Task GlobalVersion_MultipleRequestHandlers_UpgradeWorks(string version)
        {
            var deploymentParameters = GetGlobalVersionBaseDeploymentParameters();
            var deploymentResult = await DeployAsync(deploymentParameters);

            var originalANCMPath = GetANCMRequestHandlerPath(deploymentResult, _handlerVersion20);

            deploymentResult.RetryingHttpClient.DefaultRequestHeaders.Add("ANCMRHPath", originalANCMPath);
            var response = await deploymentResult.RetryingHttpClient.GetAsync("CheckRequestHandlerVersion");
            var responseText = await response.Content.ReadAsStringAsync();

            Assert.Equal(_helloWorldResponse, responseText);

            StopServer();

            deploymentResult = await DeployAsync(deploymentParameters);

            originalANCMPath = GetANCMRequestHandlerPath(deploymentResult, _handlerVersion20);

            var newANCMPath = GetANCMRequestHandlerPath(deploymentResult, version);

            var di = Directory.CreateDirectory(Path.GetDirectoryName(newANCMPath));

            File.Copy(originalANCMPath, newANCMPath, true);

            deploymentResult.RetryingHttpClient.DefaultRequestHeaders.Add("ANCMRHPath", newANCMPath);
            response = await deploymentResult.RetryingHttpClient.GetAsync("CheckRequestHandlerVersion");
            responseText = await response.Content.ReadAsStringAsync();

            Assert.Equal(_helloWorldResponse, responseText);
            AssertLoadedVersion(version);
        }

        private DeploymentParameters GetGlobalVersionBaseDeploymentParameters()
        {
            return new DeploymentParameters(Helpers.GetOutOfProcessTestSitesPath(), DeployerSelector.ServerType, RuntimeFlavor.CoreClr, RuntimeArchitecture.x64)
            {
                TargetFramework = Tfm.NetCoreApp22,
                ApplicationType = ApplicationType.Portable,
                AncmVersion = AncmVersion.AspNetCoreModuleV2,
                HostingModel = HostingModel.OutOfProcess,
                PublishApplicationBeforeDeployment = true,
                AdditionalPublishParameters = $"{_outOfProcessVersionVariable}{_handlerVersion20}"
            };
        }

        private string GetANCMRequestHandlerPath(IISDeploymentResult deploymentResult, string version)
        {
            return Path.Combine(deploymentResult.DeploymentResult.ContentRoot,
               deploymentResult.DeploymentResult.DeploymentParameters.RuntimeArchitecture.ToString(),
               version,
               _aspNetCoreDll);
        }

        private void AssertLoadedVersion(string version)
        {
            Assert.Contains(TestSink.Writes, context => context.Message.Contains(version + @"\aspnetcorev2_outofprocess.dll"));
        }
    }
}
