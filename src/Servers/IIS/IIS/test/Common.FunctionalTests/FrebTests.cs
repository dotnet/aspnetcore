// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests.Utilities;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

#if !IIS_FUNCTIONALS
using Microsoft.AspNetCore.Server.IIS.FunctionalTests;

#if IISEXPRESS_FUNCTIONALS
namespace Microsoft.AspNetCore.Server.IIS.IISExpress.FunctionalTests;
#elif NEWHANDLER_FUNCTIONALS
namespace Microsoft.AspNetCore.Server.IIS.NewHandler.FunctionalTests;
#elif NEWSHIM_FUNCTIONALS
namespace Microsoft.AspNetCore.Server.IIS.NewShim.FunctionalTests;
#endif

#else
namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;
#endif

[Collection(PublishedSitesCollection.Name)]
[SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;")]
public class FrebTests : IISFunctionalTestBase
{
    public FrebTests(PublishedSitesFixture fixture) : base(fixture)
    {
    }

    public static IList<FrebLogItem> FrebChecks()
    {
        var list = new List<FrebLogItem>();
        list.Add(new FrebLogItem("ANCM_INPROC_EXECUTE_REQUEST_START"));
        list.Add(new FrebLogItem("ANCM_INPROC_EXECUTE_REQUEST_COMPLETION", "1"));
        list.Add(new FrebLogItem("ANCM_INPROC_ASYNC_COMPLETION_START"));
        list.Add(new FrebLogItem("ANCM_INPROC_ASYNC_COMPLETION_COMPLETION", "0"));
        list.Add(new FrebLogItem("ANCM_INPROC_MANAGED_REQUEST_COMPLETION"));
        return list;
    }

    [ConditionalFact]
    [RequiresIIS(IISCapability.FailedRequestTracingModule)]
    public async Task CheckCommonFrebEvents()
    {
        var result = await SetupFrebApp();

        await result.HttpClient.GetAsync("HelloWorld");

        StopServer();

        AssertFrebLogs(result, FrebChecks());
    }

    [ConditionalFact]
    [RequiresNewShim]
    [RequiresIIS(IISCapability.FailedRequestTracingModule)]
    public async Task FrebIncludesHResultFailures()
    {
        var parameters = Fixture.GetBaseDeploymentParameters();
        parameters.TransformArguments((args, _) => string.Empty);
        var result = await SetupFrebApp(parameters);

        await result.HttpClient.GetAsync("HelloWorld");

        StopServer();

        AssertFrebLogs(result, new FrebLogItem("ANCM_HRESULT_FAILED"), new FrebLogItem("ANCM_EXCEPTION_CAUGHT"));
    }

    [ConditionalFact]
    [RequiresIIS(IISCapability.FailedRequestTracingModule)]
    public async Task CheckFailedRequestEvents()
    {
        var result = await SetupFrebApp();

        await result.HttpClient.GetAsync("Throw");

        StopServer();

        AssertFrebLogs(result, new FrebLogItem("ANCM_INPROC_ASYNC_COMPLETION_COMPLETION", "2"));
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

        // The order of freb logs is based on when the requests are complete.
        // This is non-deterministic here, so we need to check both freb files for a request that was disconnected.
        AssertFrebLogs(result, new FrebLogItem("ANCM_INPROC_REQUEST_DISCONNECT"), new FrebLogItem("ANCM_INPROC_MANAGED_REQUEST_COMPLETION"));
    }

    private async Task<IISDeploymentResult> SetupFrebApp(IISDeploymentParameters parameters = null)
    {
        parameters = parameters ?? Fixture.GetBaseDeploymentParameters();
        parameters.EnableFreb("Verbose", LogFolderPath);

        Directory.CreateDirectory(LogFolderPath);
        var result = await DeployAsync(parameters);
        return result;
    }

    private void AssertFrebLogs(IISDeploymentResult result, params FrebLogItem[] expectedFrebEvents)
    {
        AssertFrebLogs(result, (IEnumerable<FrebLogItem>)expectedFrebEvents);
    }

    private void AssertFrebLogs(IISDeploymentResult result, IEnumerable<FrebLogItem> expectedFrebEvents)
    {
        var frebEvent = GetFrebLogItems(result);
        foreach (var expectedEvent in expectedFrebEvents)
        {
            result.Logger.LogInformation($"Checking if {expectedEvent.ToString()} exists.");
            Assert.Contains(expectedEvent, frebEvent);
        }
    }

    private IEnumerable<FrebLogItem> GetFrebLogItems(IISDeploymentResult result)
    {
        var folderPath = Helpers.GetFrebFolder(LogFolderPath, result);
        var xmlFiles = Directory.GetFiles(folderPath).Where(f => f.EndsWith("xml", StringComparison.Ordinal)).ToList();
        var frebEvents = new List<FrebLogItem>();

        result.Logger.LogInformation($"Number of freb files available {xmlFiles.Count}.");
        foreach (var xmlFile in xmlFiles)
        {
            var xDocument = XDocument.Load(xmlFile).Root;
            var nameSpace = (XNamespace)"http://schemas.microsoft.com/win/2004/08/events/event";
            var eventElements = xDocument.Descendants(nameSpace + "Event");
            foreach (var eventElement in eventElements)
            {
                var eventElementWithOpCode = eventElement.Descendants(nameSpace + "RenderingInfo").Single().Descendants(nameSpace + "Opcode").Single();
                var requestStatus = eventElement.Element(nameSpace + "EventData").Descendants().Where(el => el.Attribute("Name").Value == "requestStatus").SingleOrDefault();
                frebEvents.Add(new FrebLogItem(eventElementWithOpCode.Value, requestStatus?.Value));
            }
        }

        return frebEvents;
    }

    public class FrebLogItem
    {
        private string _opCode;
        private string _requestStatus;

        public FrebLogItem(string opCode)
        {
            _opCode = opCode;
        }

        public FrebLogItem(string opCode, string requestStatus)
        {
            _opCode = opCode;
            _requestStatus = requestStatus;
        }

        public override bool Equals(object obj)
        {
            var item = obj as FrebLogItem;
            return item != null &&
                    _opCode == item._opCode &&
                    _requestStatus == item._requestStatus;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_opCode, _requestStatus);
        }

        public override string ToString()
        {
            return $"FrebLogItem: opCode: {_opCode}, requestStatus: {_requestStatus}";
        }
    }
}
