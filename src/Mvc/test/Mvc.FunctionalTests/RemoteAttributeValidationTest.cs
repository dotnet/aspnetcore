// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RemoteAttributeValidationTest : LoggedTest
{
    private static readonly Assembly _resourcesAssembly =
        typeof(RemoteAttributeValidationTest).GetTypeInfo().Assembly;

    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<BasicWebSite.StartupWithoutEndpointRouting>(LoggerFactory);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public MvcTestFixture<BasicWebSite.StartupWithoutEndpointRouting> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Theory]
    [InlineData("Area1", "/Area1")]
    [InlineData("Root", "")]
    public async Task RemoteAttribute_LeadsToExpectedValidationAttributes(string areaName, string pathSegment)
    {
        // Arrange
        var outputFile = "compiler/resources/BasicWebSite." + areaName + ".RemoteAttribute_Home.Create.html";
        var expectedContent =
            await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);
        var url = "http://localhost" + pathSegment + "/RemoteAttribute_Home/Create";

        // Act
        var response = await Client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType.MediaType);
        Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

        var responseContent = await response.Content.ReadAsStringAsync();
        ResourceFile.UpdateOrVerify(_resourcesAssembly, outputFile, expectedContent, responseContent);
    }

    [Theory]
    [InlineData("", "\"/RemoteAttribute_Verify/IsIdAvailable rejects UserId1: 'Joe1'.\"")]
    [InlineData("/Area1", "false")]
    [InlineData("/Area2",
        "\"/Area2/RemoteAttribute_Verify/IsIdAvailable rejects 'Joe4' with 'Joe1', 'Joe2', and 'Joe3'.\"")]
    public async Task RemoteAttribute_VerificationAction_GetReturnsExpectedJson(
        string pathSegment,
        string expectedContent)
    {
        // Arrange
        var url = "http://localhost" + pathSegment +
            "/RemoteAttribute_Verify/IsIdAvailable?UserId1=Joe1&UserId2=Joe2&UserId3=Joe3&UserId4=Joe4";

        // Act
        var response = await Client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
        Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Equal(expectedContent, responseContent);
    }

    [Theory]
    [InlineData("", "\"/RemoteAttribute_Verify/IsIdAvailable rejects UserId1: 'Jane1'.\"")]
    [InlineData("/Area1", "false")]
    public async Task RemoteAttribute_VerificationAction_PostReturnsExpectedJson(
        string pathSegment,
        string expectedContent)
    {
        // Arrange
        var url = "http://localhost" + pathSegment + "/RemoteAttribute_Verify/IsIdAvailable";
        var contentDictionary = new Dictionary<string, string>
            {
                { "UserId1", "Jane1" },
                { "UserId2", "Jane2" },
                { "UserId3", "Jane3" },
                { "UserId4", "Jane4" },
            };
        var content = new FormUrlEncodedContent(contentDictionary);

        // Act
        var response = await Client.PostAsync(url, content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
        Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Equal(expectedContent, responseContent);
    }
}
