// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.AspNetCore.Server.IISIntegration.FunctionalTests
{
    [Collection(PublishedSitesCollection.Name)]
    public class FrebTests : LogFileTestBase
    {
        private readonly PublishedSitesFixture _fixture;
        public FrebTests(PublishedSitesFixture fixture)
        {
            _fixture = fixture;
        }

        public static ISet<string[]> FrebChecks()
        {
            var set = new HashSet<string[]>();
            set.Add(new string[] { "ANCM_INPROC_EXECUTE_REQUEST_START" });
            set.Add(new string[] { "ANCM_INPROC_EXECUTE_REQUEST_COMPLETION", "1" });
            set.Add(new string[] { "ANCM_INPROC_ASYNC_COMPLETION_START" });
            set.Add(new string[] { "ANCM_INPROC_ASYNC_COMPLETION_COMPLETION", "0" });
            set.Add(new string[] { "ANCM_INPROC_MANAGED_REQUEST_COMPLETION" });
            return set;
        }

        [ConditionalFact]
        [RequiresIIS(IISCapability.FailedRequestTracingModule)]
        public async Task CheckCommonFrebEvents()
        {
            var result = await SetupFrebApp();

            await result.HttpClient.GetAsync("HelloWorld");

            StopServer();

            foreach (var data in FrebChecks())
            {
                AssertFrebLogs(result, data);
            }
        }

        [ConditionalFact]
        [RequiresIIS(IISCapability.FailedRequestTracingModule)]
        public async Task CheckFailedRequestEvents()
        {
            var result = await SetupFrebApp();

            await result.HttpClient.GetAsync("Throw");

            StopServer();

            AssertFrebLogs(result, "ANCM_INPROC_ASYNC_COMPLETION_COMPLETION", "2");
        }

        [ConditionalFact]
        [RequiresIIS(IISCapability.FailedRequestTracingModule)]
        public async Task CheckFrebDisconnect()
        {
            var result = await SetupFrebApp();

            using (var connection = new TestConnection(result.HttpClient.BaseAddress.Port))
            {
                await connection.Send(
                    "GET /WaitForAbort HTTP/1.1",
                    "Host: localhost",
                    "Connection: close",
                    "",
                    "");
                await result.HttpClient.RetryRequestAsync("/WaitingRequestCount", async message => await message.Content.ReadAsStringAsync() == "1");
            }

            StopServer();

            AssertFrebLogs(result, "ANCM_INPROC_REQUEST_DISCONNECT");
        }

        private async Task<IISDeploymentResult> SetupFrebApp()
        {
            var parameters = _fixture.GetBaseDeploymentParameters(publish: true);
            parameters.EnableFreb("Verbose", _logFolderPath);

            Directory.CreateDirectory(_logFolderPath);
            var result = await DeployAsync(parameters);
            return result;
        }

        private void AssertFrebLogs(IISDeploymentResult result, params string[] data)
        {
            var folderPath = Helpers.GetFrebFolder(_logFolderPath, result);
            var fileString = Directory.GetFiles(folderPath).Where(f => f.EndsWith("xml")).OrderBy(x => x).Last();

            var xDocument = XDocument.Load(fileString).Root;
            var nameSpace = (XNamespace)"http://schemas.microsoft.com/win/2004/08/events/event";
            var elements = xDocument.Descendants(nameSpace + "Event");
            var element = elements.Where(el => el.Descendants(nameSpace + "RenderingInfo").Single().Descendants(nameSpace + "Opcode").Single().Value == data[0]);

            Assert.Single(element);

            if (data.Length > 1)
            {
                var requestStatus = element.Single().Element(nameSpace + "EventData").Descendants().Where(el => el.Attribute("Name").Value == "requestStatus").Single();
                Assert.Equal(data[1], requestStatus.Value);
            }
        }
    }
}
