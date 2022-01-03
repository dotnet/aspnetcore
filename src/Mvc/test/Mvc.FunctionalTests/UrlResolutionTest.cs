// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class UrlResolutionTest :
    IClassFixture<MvcTestFixture<RazorWebSite.Startup>>,
    IClassFixture<MvcEncodedTestFixture<RazorWebSite.Startup>>
{
    private static readonly Assembly _resourcesAssembly = typeof(UrlResolutionTest).GetTypeInfo().Assembly;

    public UrlResolutionTest(
        MvcTestFixture<RazorWebSite.Startup> fixture,
        MvcEncodedTestFixture<RazorWebSite.Startup> encodedFixture)
    {
        Client = fixture.CreateDefaultClient();
        EncodedClient = encodedFixture.CreateDefaultClient();
    }

    public HttpClient Client { get; }

    public HttpClient EncodedClient { get; }

    [Fact]
    public async Task AppRelativeUrlsAreResolvedCorrectly()
    {
        // Arrange
        var outputFile = "compiler/resources/RazorWebSite.UrlResolution.Index.html";
        var expectedContent =
            await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

        // Act
        var response = await Client.GetAsync("http://localhost/UrlResolution/Index");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        responseContent = responseContent.Trim();
        ResourceFile.UpdateOrVerify(_resourcesAssembly, outputFile, expectedContent, responseContent);
    }

    [Fact]
    public async Task AppRelativeUrlsAreResolvedAndEncodedCorrectly()
    {
        // Arrange
        var outputFile = "compiler/resources/RazorWebSite.UrlResolution.Index.Encoded.html";
        var expectedContent =
            await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

        // Act
        var response = await EncodedClient.GetAsync("http://localhost/UrlResolution/Index");
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        responseContent = responseContent.Trim();
        ResourceFile.UpdateOrVerify(_resourcesAssembly, outputFile, expectedContent, responseContent);
    }
}
