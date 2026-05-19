// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace AuthSamples.FunctionalTests;

public class DynamicSchemeTests : IClassFixture<WebApplicationFactory<DynamicSchemes.Startup>>
{
    public DynamicSchemeTests(WebApplicationFactory<DynamicSchemes.Startup> fixture)
    {
        Client = fixture.CreateClient();
    }

    public HttpClient Client { get; }

    [Fact]
    public async Task DefaultReturns200()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CanAddUpdateRemoveSchemes()
    {
        // Arrange & Act
        var response = await AddScheme("New1", "NewOne");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("New1", content);
        Assert.Contains("NewOne", content);

        response = await AddScheme("New2", "NewTwo");
        content = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("New1", content);
        Assert.Contains("NewOne", content);
        Assert.Contains("New2", content);
        Assert.Contains("NewTwo", content);

        response = await AddScheme("New2", "UpdateTwo");
        content = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("New1", content);
        Assert.Contains("NewOne", content);
        Assert.Contains("New2", content);
        Assert.DoesNotContain("NewTwo", content);
        Assert.Contains("UpdateTwo", content);

        // Now remove all the schemes one at a time
        response = await Client.GetAsync("/Auth/Remove?scheme=New1");
        content = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("New1", content);
        Assert.DoesNotContain("NewOne", content);
        Assert.Contains("New2", content);
        Assert.DoesNotContain("NewTwo", content);
        Assert.Contains("UpdateTwo", content);

        response = await Client.GetAsync("/Auth/Remove?scheme=New2");
        content = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("New1", content);
        Assert.DoesNotContain("NewOne", content);
        Assert.DoesNotContain("New2", content);
        Assert.DoesNotContain("NewTwo", content);
        Assert.DoesNotContain("UpdateTwo", content);
    }

    private async Task<HttpResponseMessage> AddScheme(string name, string message)
    {
        var goToSignIn = await Client.GetAsync("/");
        var signIn = await TestAssert.IsHtmlDocumentAsync(goToSignIn);

        var form = TestAssert.HasForm(signIn);
        return await Client.SendAsync(form, new Dictionary<string, string>()
        {
            ["scheme"] = name,
            ["OptionsMessage"] = message,
        });

    }

}
