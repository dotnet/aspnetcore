// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;

namespace Microsoft.AspNetCore.Diagnostics.FunctionalTests;

public class WelcomePageSampleTest : IClassFixture<TestFixture<WelcomePageSample.Startup>>
{
    public WelcomePageSampleTest(TestFixture<WelcomePageSample.Startup> fixture)
    {
        Client = fixture.Client;
    }

    public HttpClient Client { get; }

    [Fact]
    public async Task WelcomePage_ShowsWelcome()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/");

        var response = await Client.SendAsync(request);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 1);
        Assert.NotEqual(0xEF, bytes[0]); // No leading UTF-8 BOM

        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Your ASP.NET Core application has been successfully started", body);
    }
}
