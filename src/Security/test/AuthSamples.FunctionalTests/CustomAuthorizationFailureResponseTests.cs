// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AuthSamples.FunctionalTests;

public class CustomAuthorizationFailureResponseTests : IClassFixture<WebApplicationFactory<CustomAuthorizationFailureResponse.Startup>>
{
    private HttpClient Client { get; }

    public CustomAuthorizationFailureResponseTests(WebApplicationFactory<CustomAuthorizationFailureResponse.Startup> fixture)
    {
        Client = fixture.CreateClient();
    }

    [Fact]
    public async Task SampleGetWithCustomPolicyWithCustomForbiddenMessage_Returns403WithCustomMessage()
    {
        var response = await Client.GetAsync("api/Sample/customPolicyWithCustomForbiddenMessage");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(CustomAuthorizationFailureResponse.Startup.CustomForbiddenMessage, content);
    }

    [Fact]
    public async Task SampleGetWithCustomPolicy_Returns404WithCustomMessage()
    {
        var response = await Client.GetAsync("api/Sample/customPolicy");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(CustomAuthorizationFailureResponse.Startup.CustomForbiddenMessage, content);
    }
}
