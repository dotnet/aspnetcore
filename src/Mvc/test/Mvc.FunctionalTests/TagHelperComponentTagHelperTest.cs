// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class TagHelperComponentTagHelperTest : LoggedTest
{
    private static readonly Assembly _resourcesAssembly = typeof(TagHelperComponentTagHelperTest).GetTypeInfo().Assembly;

    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<RazorWebSite.Startup>(LoggerFactory);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public MvcTestFixture<RazorWebSite.Startup> Factory { get; private set; }
    public HttpClient Client { get; private set; }

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
