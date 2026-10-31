// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class UrlResolutionTest : LoggedTest
{
    private static readonly Assembly _resourcesAssembly = typeof(UrlResolutionTest).GetTypeInfo().Assembly;

    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<RazorWebSite.Startup>(LoggerFactory);
        EncodedFactory = new MvcEncodedTestFixture<RazorWebSite.Startup>(LoggerFactory);
        Client = Factory.CreateDefaultClient();
        EncodedClient = EncodedFactory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        EncodedFactory.Dispose();
        base.Dispose();
    }

    public MvcTestFixture<RazorWebSite.Startup> Factory { get; private set; }
    public MvcEncodedTestFixture<RazorWebSite.Startup> EncodedFactory { get; private set; }
    public HttpClient Client { get; private set; }

    public HttpClient EncodedClient { get; private set; }

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
