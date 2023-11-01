// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Server.IIS.FunctionalTests;
using Microsoft.AspNetCore.InternalTesting;

namespace IIS.Tests;

[SkipIfHostableWebCoreNotAvailable]
[MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8, SkipReason = "https://github.com/aspnet/IISIntegration/issues/866")]
[SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;")]
public class ResponseBodySizeTests : LoggedTest
{
    [ConditionalFact]
    public async Task WriteAsyncShouldCorrectlyHandleBigBuffers()
    {
        const int bufferSize = 256 * 1024 * 1024;

        using (var testServer = await TestServer.Create(
            async ctx =>
            {
                var buffer = new byte[bufferSize];
                await ctx.Response.Body.WriteAsync(buffer, 0, buffer.Length);

            }, LoggerFactory))
        {
            var response = await testServer.HttpClient.GetAsync("/");
            var content = await response.Content.ReadAsByteArrayAsync();

            Assert.Equal(bufferSize, content.Length);
        }
    }
}
