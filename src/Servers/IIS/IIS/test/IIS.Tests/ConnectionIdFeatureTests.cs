// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;

[SkipIfHostableWebCoreNotAvailable]
[MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8, SkipReason = "https://github.com/aspnet/IISIntegration/issues/866")]
[SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;")]
public class ConnectionIdFeatureTests : StrictTestServerTests
{
    [ConditionalFact]
    public async Task ProvidesConnectionId()
    {
        string connectionId = null;
        using (var testServer = await TestServer.Create(ctx =>
        {
            var connectionIdFeature = ctx.Features.Get<IHttpConnectionFeature>();
            connectionId = connectionIdFeature.ConnectionId;
            return Task.CompletedTask;
        }, LoggerFactory))
        {
            await testServer.HttpClient.GetStringAsync("/");
        }

        Assert.NotNull(connectionId);
    }
}
