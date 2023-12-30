// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Quic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests;
using Microsoft.AspNetCore.Server.IntegrationTesting.Common;
using Microsoft.AspNetCore.Server.IntegrationTesting.IIS;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Microsoft.Win32;
using Xunit;

namespace Microsoft.AspNetCore.Server.IIS.FunctionalTests;

[MsQuicSupported]
[HttpSysHttp3Supported]
[Collection(IISHttpsTestSiteCollection.Name)]
[SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;")]
public class Http3Tests : FunctionalTestsBase
{
    public Http3Tests(IISTestSiteFixture fixture)
    {
        var port = TestPortHelper.GetNextSSLPort();
        fixture.DeploymentParameters.ApplicationBaseUriHint = $"https://localhost:{port}/";
        fixture.DeploymentParameters.AddHttpsToServerConfig();
        fixture.DeploymentParameters.SetWindowsAuth(false);
        Fixture = fixture;
    }

    public IISTestSiteFixture Fixture { get; }

    [ConditionalFact]
    public async Task Http3_Direct()
    {
        using var client = SetUpClient();
        client.DefaultRequestVersion = HttpVersion.Version30;
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
        var response = await client.GetAsync(Fixture.Client.BaseAddress.ToString() + "Http3_Direct");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpVersion.Version30, response.Version);
        Assert.Equal("HTTP/3", await response.Content.ReadAsStringAsync());
    }

    [ConditionalFact]
    public async Task Http3_AltSvcHeader_UpgradeFromHttp1()
    {
        var address = Fixture.Client.BaseAddress.ToString() + "Http3_AltSvcHeader_UpgradeFromHttp1";

        var altsvc = $@"h3="":{new Uri(address).Port}""";
        using var client = SetUpClient();
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

        // First request is HTTP/1.1, gets an alt-svc response
        var request = new HttpRequestMessage(HttpMethod.Get, address);
        request.Version = HttpVersion.Version11;
        request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
        var response1 = await client.SendAsync(request);
        response1.EnsureSuccessStatusCode();
        Assert.Equal("HTTP/1.1", await response1.Content.ReadAsStringAsync());
        Assert.Equal(altsvc, response1.Headers.GetValues(HeaderNames.AltSvc).SingleOrDefault());

        // Second request is HTTP/3
        var response3 = await client.GetAsync(address);
        Assert.Equal(HttpVersion.Version30, response3.Version);
        Assert.Equal("HTTP/3", await response3.Content.ReadAsStringAsync());
    }

    [ConditionalFact]
    public async Task Http3_AltSvcHeader_UpgradeFromHttp2()
    {
        var address = Fixture.Client.BaseAddress.ToString() + "Http3_AltSvcHeader_UpgradeFromHttp2";

        var altsvc = $@"h3="":{new Uri(address).Port}""";
        using var client = SetUpClient();
        client.DefaultRequestVersion = HttpVersion.Version20;
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

        // First request is HTTP/2, gets an alt-svc response
        var response2 = await client.GetAsync(address);
        response2.EnsureSuccessStatusCode();
        Assert.Equal(altsvc, response2.Headers.GetValues(HeaderNames.AltSvc).SingleOrDefault());
        Assert.Equal("HTTP/2", await response2.Content.ReadAsStringAsync());

        // Second request is HTTP/3
        var response3 = await client.GetStringAsync(address);
        Assert.Equal("HTTP/3", response3);
    }

    [ConditionalFact]
    public async Task Http3_ResponseTrailers()
    {
        var address = Fixture.Client.BaseAddress.ToString() + "Http3_ResponseTrailers";
        using var client = SetUpClient();
        client.DefaultRequestVersion = HttpVersion.Version30;
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
        var response = await client.GetAsync(address);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadAsStringAsync();
        Assert.Equal("HTTP/3", result);
        Assert.Equal("value", response.TrailingHeaders.GetValues("custom").SingleOrDefault());
    }

    [ConditionalFact]
    public async Task Http3_ResetBeforeHeaders()
    {
        var address = Fixture.Client.BaseAddress.ToString() + "Http3_ResetBeforeHeaders";
        using var client = SetUpClient();
        client.DefaultRequestVersion = HttpVersion.Version30;
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(address));
        var qex = Assert.IsType<QuicException>(ex.InnerException);
        Assert.Equal(QuicError.StreamAborted, qex.QuicError);
        Assert.Equal(0x010b, qex.ApplicationErrorCode.Value);
    }

    [ConditionalFact]
    public async Task Http3_ResetAfterHeaders()
    {
        var address = Fixture.Client.BaseAddress.ToString() + "Http3_ResetAfterHeaders";
        using var client = SetUpClient();
        client.DefaultRequestVersion = HttpVersion.Version30;
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;
        var response = await client.GetAsync(address, HttpCompletionOption.ResponseHeadersRead);
        await client.GetAsync(Fixture.Client.BaseAddress.ToString() + "Http3_ResetAfterHeaders_SetResult");
        response.EnsureSuccessStatusCode();
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => response.Content.ReadAsStringAsync());
        var qex = Assert.IsType<QuicException>(ex.InnerException?.InnerException?.InnerException);
        Assert.Equal(QuicError.StreamAborted, qex.QuicError);
        Assert.Equal(0x010c, qex.ApplicationErrorCode.Value); // H3_REQUEST_CANCELLED
    }

    [ConditionalFact]
    public async Task Http3_AppExceptionAfterHeaders_InternalError()
    {
        var address = Fixture.Client.BaseAddress.ToString() + "Http3_AppExceptionAfterHeaders_InternalError";
        using var client = SetUpClient();
        client.DefaultRequestVersion = HttpVersion.Version30;
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;

        var response = await client.GetAsync(address, HttpCompletionOption.ResponseHeadersRead);
        await client.GetAsync(Fixture.Client.BaseAddress.ToString() + "Http3_AppExceptionAfterHeaders_InternalError_SetResult");
        response.EnsureSuccessStatusCode();
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => response.Content.ReadAsStringAsync());
        var qex = Assert.IsType<QuicException>(ex.InnerException?.InnerException?.InnerException);
        Assert.Equal(QuicError.StreamAborted, qex.QuicError);
        Assert.Equal(0x0102, qex.ApplicationErrorCode.Value); // H3_INTERNAL_ERROR
    }

    [ConditionalFact]
    public async Task Http3_Abort_Cancel()
    {
        var address = Fixture.Client.BaseAddress.ToString() + "Http3_Abort_Cancel";
        using var client = SetUpClient();
        client.DefaultRequestVersion = HttpVersion.Version30;
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact;

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(address));
        var qex = Assert.IsType<QuicException>(ex.InnerException);
        Assert.Equal(QuicError.StreamAborted, qex.QuicError);
        Assert.Equal(0x010c, qex.ApplicationErrorCode.Value); // H3_REQUEST_CANCELLED
    }

    private HttpClient SetUpClient()
    {
        var handler = new HttpClientHandler();
        // Needed on CI, the IIS Express cert we use isn't trusted there.
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(200) };
    }
}
