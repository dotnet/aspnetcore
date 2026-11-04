// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;

[SkipIfHostableWebCoreNotAvailable]
[MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8, SkipReason = "https://github.com/aspnet/IISIntegration/issues/866")]
public class ReasonPhraseTests : StrictTestServerTests
{
    [ConditionalTheory]
    [InlineData("Injected\r\nHeader: value")]
    [InlineData("Has\rCarriageReturn")]
    [InlineData("Has\nLineFeed")]
    [InlineData("Has\0Null")]
    [InlineData("Control\u001FChar")]
    [InlineData("Del\u007FChar")]
    [InlineData("Non-ASCII\u0080Char")]
    [InlineData("Caf\u00E9")]
    public async Task ReasonPhraseWithControlCharacters_Throws(string reasonPhrase)
    {
        using (var testServer = await TestServer.Create(
            ctx =>
            {
                Assert.Throws<InvalidOperationException>(() =>
                    ctx.Features.Get<IHttpResponseFeature>().ReasonPhrase = reasonPhrase);
                return Task.CompletedTask;
            }, LoggerFactory))
        {
            var result = await testServer.HttpClient.GetAsync("/");
            Assert.Equal(200, (int)result.StatusCode);
        }
    }

    [ConditionalTheory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("OK")]
    [InlineData("Custom Reason")]
    [InlineData("Includes\tHTAB")]
    public async Task ValidReasonPhrase_Accepted(string reasonPhrase)
    {
        using (var testServer = await TestServer.Create(
            ctx =>
            {
                ctx.Features.Get<IHttpResponseFeature>().ReasonPhrase = reasonPhrase;
                return Task.CompletedTask;
            }, LoggerFactory))
        {
            var result = await testServer.HttpClient.GetAsync("/");
            Assert.Equal(200, (int)result.StatusCode);
        }
    }
}
