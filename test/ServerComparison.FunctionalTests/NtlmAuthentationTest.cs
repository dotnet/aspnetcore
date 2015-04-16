// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DeploymentHelpers;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Framework.Logging;
using Xunit;

namespace ServerComparison.FunctionalTests
{
    // Uses ports ranging 5050 - 5060.
    public class NtlmAuthenticationTests
    {
        [ConditionalTheory, Trait("ServerComparison.FunctionalTests", "ServerComparison.FunctionalTests")]
        [OSSkipCondition(OperatingSystems.Unix | OperatingSystems.MacOSX)]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.coreclr, RuntimeArchitecture.x86, "http://localhost:5050/")]
        [InlineData(ServerType.IISExpress, RuntimeFlavor.clr, RuntimeArchitecture.x64, "http://localhost:5051/")]
        [InlineData(ServerType.WebListener, RuntimeFlavor.clr, RuntimeArchitecture.x86, "http://localhost:5052/")]
        [InlineData(ServerType.WebListener, RuntimeFlavor.coreclr, RuntimeArchitecture.x64, "http://localhost:5052/")]
        public async Task NtlmAuthentication(ServerType serverType, RuntimeFlavor runtimeFlavor, RuntimeArchitecture architecture, string applicationBaseUrl)
        {
            var logger = new LoggerFactory()
                            .AddConsole()
                            .CreateLogger(string.Format("Ntlm:{0}:{1}:{2}", serverType, runtimeFlavor, architecture));

            using (logger.BeginScope("NtlmAuthenticationTest"))
            {
                var stopwatch = Stopwatch.StartNew();

                logger.LogInformation("Variation Details : HostType = {hostType}, RuntimeFlavor = {flavor}, Architecture = {arch}, applicationBaseUrl = {appBase}",
                    serverType, runtimeFlavor, architecture, applicationBaseUrl);

                var deploymentParameters = new DeploymentParameters(Helpers.GetApplicationPath(), serverType, runtimeFlavor, architecture)
                {
                    ApplicationBaseUriHint = applicationBaseUrl,
                    EnvironmentName = "NtlmAuthentication", // Will pick the Start class named 'StartupNtlmAuthentication'
                    ApplicationHostConfigTemplateContent = (serverType == ServerType.IISExpress) ? File.ReadAllText("NtlmAuthentation.config") : null,
                    SiteName = "NtlmAuthenticationTestSite", // This is configured in the NtlmAuthentication.config
                };

                using (var deployer = ApplicationDeployerFactory.Create(deploymentParameters, logger))
                {
                    var deploymentResult = deployer.Deploy();
                    var httpClientHandler = new HttpClientHandler();
                    var httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(deploymentResult.ApplicationBaseUri) };

                    HttpResponseMessage response = null;

                    // Request to base address and check if various parts of the body are rendered & measure the cold startup time.
                    RetryHelper.RetryRequest(() =>
                    {
                        response = httpClient.GetAsync(string.Empty).Result;
                        return response;
                    }, logger: logger);

                    logger.LogInformation("[Time]: Approximate time taken for application initialization : '{t}' seconds", stopwatch.Elapsed.TotalSeconds);

                    var responseText = await response.Content.ReadAsStringAsync();
                    Assert.Equal("Hello World", responseText);

                    responseText = await httpClient.GetStringAsync("/Anonymous");
                    Assert.Equal("Anonymous?True", responseText);

                    response = await httpClient.GetAsync("/Restricted");
                    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                    Assert.Contains("NTLM", response.Headers.WwwAuthenticate.ToString());

                    httpClientHandler = new HttpClientHandler() { UseDefaultCredentials = true };
                    httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(deploymentResult.ApplicationBaseUri) };
                    responseText = await httpClient.GetStringAsync("/Restricted");
                    Assert.Equal("NotAnonymous", responseText);

                    stopwatch.Stop();
                    logger.LogInformation("[Time]: Total time taken for this test variation '{t}' seconds", stopwatch.Elapsed.TotalSeconds);
                }
            }
        }
    }
}