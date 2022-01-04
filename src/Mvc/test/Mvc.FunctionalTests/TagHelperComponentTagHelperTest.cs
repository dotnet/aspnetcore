// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class TagHelperComponentTagHelperTest : IClassFixture<MvcTestFixture<RazorWebSite.Startup>>
{
    private static readonly Assembly _resourcesAssembly = typeof(TagHelperComponentTagHelperTest).GetTypeInfo().Assembly;

    public TagHelperComponentTagHelperTest(MvcTestFixture<RazorWebSite.Startup> fixture)
    {
        Client = fixture.CreateDefaultClient();
    }

    public HttpClient Client { get; }

    [Fact]
    public async Task InjectsTestHeadTagHelperComponent()
    {
        // Arrange
        var url = "http://localhost/TagHelperComponent/GetHead";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var outputFile = "compiler/resources/RazorWebSite.TagHelperComponent.Head.html";
        var expectedContent =
            await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

        // Act
        var response = await Client.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ResourceFile.UpdateOrVerify(_resourcesAssembly, outputFile, expectedContent, responseContent);
    }

    [Fact]
    public async Task InjectsTestBodyTagHelperComponent()
    {
        // Arrange
        var url = "http://localhost/TagHelperComponent/GetBody";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var outputFile = "compiler/resources/RazorWebSite.TagHelperComponent.Body.html";
        var expectedContent =
            await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

        // Act
        var response = await Client.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ResourceFile.UpdateOrVerify(_resourcesAssembly, outputFile, expectedContent, responseContent);
    }

    [Fact]
    public async Task AddTestTagHelperComponent_FromController()
    {
        // Arrange
        var url = "http://localhost/AddTagHelperComponent/AddComponent";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var outputFile = "compiler/resources/RazorWebSite.AddTagHelperComponent.AddComponent.html";
        var expectedContent =
            await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

        // Act
        var response = await Client.SendAsync(request);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ResourceFile.UpdateOrVerify(_resourcesAssembly, outputFile, expectedContent, responseContent);
    }
}
