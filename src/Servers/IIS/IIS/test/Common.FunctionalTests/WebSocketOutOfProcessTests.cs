// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.IIS.FunctionalTests;
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

[Collection(IISTestSiteCollectionOutOfProc.Name)]
[MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8, SkipReason = "No WebSocket supported on Win7")]
[SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;")]
public class WebSocketsOutOfProcessTests : WebSocketsTests
{
    public WebSocketsOutOfProcessTests(IISTestSiteFixture fixture, ITestOutputHelper testOutput) : base(fixture, testOutput)
    {
        Fixture.DeploymentParameters.HostingModel = HostingModel.OutOfProcess;
    }
}
