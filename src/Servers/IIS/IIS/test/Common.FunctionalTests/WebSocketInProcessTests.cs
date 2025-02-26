// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Xunit.Abstractions;

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

[Collection(IISTestSiteCollectionInProc.Name)]
[MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8, SkipReason = "No WebSocket supported on Win7")]
#if IISEXPRESS_FUNCTIONALS
[SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open")]
#else
// These queues do not have websockets enabled currently for full IIS
[SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;Windows.Amd64.Server2022.Open")]
#endif
public class WebSocketsInProcessTests : WebSocketsTests
{
    public WebSocketsInProcessTests(IISTestSiteFixture fixture, ITestOutputHelper testOutput) : base(fixture, testOutput)
    {
        Fixture.DeploymentParameters.HostingModel = HostingModel.InProcess;
    }
}
