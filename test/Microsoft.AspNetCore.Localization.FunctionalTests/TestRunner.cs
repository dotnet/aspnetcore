// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Xunit;

namespace Microsoft.AspNetCore.Localization.FunctionalTests
{
    public class TestRunner
    {
        private string _applicationPath;

        public TestRunner(string applicationPath)
        {
            _applicationPath = Path.Combine(ResolveRootFolder(PlatformServices.Default.Application.ApplicationBasePath), applicationPath);
        }

        private static string ResolveRootFolder(string projectFolder)
        {
            var di = new DirectoryInfo(projectFolder);

            while (di.Parent != null)
            {
                var globalJsonPath = Path.Combine(di.FullName, "version.props");

                if (File.Exists(globalJsonPath))
                {
                    return di.FullName;
                }

                di = di.Parent;
            }

            // If we don't find any files then make the project folder the root
            return projectFolder;
        }

        private async Task<string> RunTestAndGetResponse(
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture runtimeArchitecture,
            string applicationBaseUrl,
            string environmentName,
            string locale)
        {
            var logger = new LoggerFactory()
                            .AddConsole()
                            .CreateLogger(string.Format("Localization Test Site:{0}:{1}:{2}", ServerType.Kestrel, runtimeFlavor, runtimeArchitecture));

            using (logger.BeginScope("LocalizationTest"))
            {
                var deploymentParameters = new DeploymentParameters(_applicationPath, ServerType.Kestrel, runtimeFlavor, runtimeArchitecture)
                {
                    ApplicationBaseUriHint = applicationBaseUrl,
                    EnvironmentName = environmentName,
                    TargetFramework = runtimeFlavor == RuntimeFlavor.Clr ? "net46" : "netcoreapp2.0"
                };

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, logger))
                {
                    var deploymentResult = deployer.Deploy();

                    var cookie = new Cookie(CookieRequestCultureProvider.DefaultCookieName, "c=" + locale + "|uic=" + locale);
                    var cookieContainer = new CookieContainer();
                    cookieContainer.Add(new Uri(deploymentResult.ApplicationBaseUri), cookie);

                    var httpClientHandler = new HttpClientHandler();
                    httpClientHandler.CookieContainer = cookieContainer;

                    using (var httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(deploymentResult.ApplicationBaseUri) })
                    {
                        // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                        var response = await RetryHelper.RetryRequest(() =>
                        {
                            return httpClient.GetAsync(string.Empty);
                        }, logger, deploymentResult.HostShutdownToken);

                        return await response.Content.ReadAsStringAsync();
                    }
                }
            }
        }

        public async Task RunTestAndVerifyResponse(
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture runtimeArchitecture,
            string applicationBaseUrl,
            string environmentName,
            string locale,
            string expectedText)
        {
            var responseText = await RunTestAndGetResponse(runtimeFlavor, runtimeArchitecture, applicationBaseUrl, environmentName, locale);
            Console.WriteLine("Response Text " + responseText);
            Assert.Equal(expectedText, responseText);
        }

        public async Task RunTestAndVerifyResponseHeading(
            RuntimeFlavor runtimeFlavor,
            RuntimeArchitecture runtimeArchitecture,
            string applicationBaseUrl,
            string environmentName,
            string locale,
            string expectedHeadingText)
        {
            var responseText = await RunTestAndGetResponse(runtimeFlavor, runtimeArchitecture, applicationBaseUrl, environmentName, locale);
            var headingIndex = responseText.IndexOf(expectedHeadingText);
            Console.WriteLine("Response Header " + responseText);
            Assert.True(headingIndex >= 0);
        }
    }
}