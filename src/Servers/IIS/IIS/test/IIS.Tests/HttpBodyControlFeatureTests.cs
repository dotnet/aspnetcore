// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;

[SkipIfHostableWebCoreNotAvailable]
[MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8, SkipReason = "https://github.com/aspnet/IISIntegration/issues/866")]
[SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;")]
public class HttpBodyControlFeatureTests : StrictTestServerTests
{
    [ConditionalFact]
    public async Task ThrowsOnSyncReadOrWrite()
    {
        Exception writeException = null;
        Exception readException = null;
        using (var testServer = await TestServer.Create(
            ctx =>
            {
                var bodyControl = ctx.Features.Get<IHttpBodyControlFeature>();
                Assert.False(bodyControl.AllowSynchronousIO);

                try
                {
                    ctx.Response.Body.Write(new byte[10]);
                }
                catch (Exception ex)
                {
                    writeException = ex;
                }

                try
                {
                    ctx.Request.Body.Read(new byte[10]);
                }
                catch (Exception ex)
                {
                    readException = ex;
                }

                return Task.CompletedTask;
            }, LoggerFactory))
        {
            await testServer.HttpClient.GetStringAsync("/");
        }

        Assert.IsType<InvalidOperationException>(readException);
        Assert.IsType<InvalidOperationException>(writeException);
    }
}
