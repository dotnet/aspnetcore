// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;

[SkipIfHostableWebCoreNotAvailable]
[MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8, SkipReason = "https://github.com/aspnet/IISIntegration/issues/866")]
public class ConnectionEndPointFeatureTests : StrictTestServerTests
{
    [ConditionalFact]
    public async Task ProvidesLocalAndRemoteEndPoints()
    {
        EndPoint localEndPoint = null;
        EndPoint remoteEndPoint = null;
        using (var testServer = await TestServer.Create(ctx =>
        {
            var endPointFeature = ctx.Features.Get<IConnectionEndPointFeature>();
            localEndPoint = endPointFeature.LocalEndPoint;
            remoteEndPoint = endPointFeature.RemoteEndPoint;
            return Task.CompletedTask;
        }, LoggerFactory))
        {
            await testServer.HttpClient.GetStringAsync("/");
        }

        Assert.NotNull(localEndPoint);
        Assert.NotNull(remoteEndPoint);
        var localIPEndPoint = Assert.IsType<IPEndPoint>(localEndPoint);
        var remoteIPEndPoint = Assert.IsType<IPEndPoint>(remoteEndPoint);
        Assert.NotEqual(0, localIPEndPoint.Port);
        Assert.NotEqual(0, remoteIPEndPoint.Port);
    }
}
