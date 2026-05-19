// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class OutputFormatterTest : LoggedTest
{
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
    [InlineData("ReturnTaskOfString")]
    [InlineData("ReturnTaskOfObject_StringValue")]
    [InlineData("ReturnString")]
    [InlineData("ReturnObject_StringValue")]
    public async Task StringOutputFormatter_ForStringValues_GetsSelectedReturnsTextPlainContentType(string actionName)
    {
        // Arrange
        var expectedContentType = MediaTypeHeaderValue.Parse("text/plain;charset=utf-8");
        var expectedBody = actionName;

        // Act
        var response = await Client.GetAsync("http://localhost/TextPlain/" + actionName);

        // Assert
        Assert.Equal(expectedContentType, response.Content.Headers.ContentType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(expectedBody, body);
    }

    [Theory]
    [InlineData("ReturnTaskOfObject_ObjectValue")]
    [InlineData("ReturnObject_ObjectValue")]
    public async Task JsonOutputFormatter_ForNonStringValue_GetsSelected(string actionName)
    {
        // Arrange
        var expectedContentType = MediaTypeHeaderValue.Parse("application/json;charset=utf-8");

        // Act
        var response = await Client.GetAsync("http://localhost/TextPlain/" + actionName);

        // Assert
        Assert.Equal(expectedContentType, response.Content.Headers.ContentType);
    }

    [Theory]
    [InlineData("ReturnTask")]
    [InlineData("ReturnVoid")]
    public async Task NoContentFormatter_ForVoidAndTaskReturnType_DoesNotRun(string actionName)
    {
        // Arrange & Act
        var response = await Client.GetAsync("http://localhost/NoContent/" + actionName);

        // Assert
        Assert.Null(response.Content.Headers.ContentType);
        var body = await response.Content.ReadAsStringAsync();
        // Response body is empty instead of null.
        Assert.Empty(body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, response.Content.Headers.ContentLength);
    }

    [Theory]
    [InlineData("ReturnTaskOfString_NullValue")]
    [InlineData("ReturnTaskOfObject_NullValue")]
    [InlineData("ReturnObject_NullValue")]
    public async Task NoContentFormatter_ForNullValue_ByDefault_GetsSelectedAndWritesResponse(string actionName)
    {
        // Arrange & Act
        var response = await Client.GetAsync("http://localhost/NoContent/" + actionName);

        // Assert
        Assert.Null(response.Content.Headers.ContentType);
        var body = await response.Content.ReadAsStringAsync();
        // Response body is empty instead of null.
        Assert.Empty(body);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, response.Content.Headers.ContentLength);
    }

    [Theory]
    [InlineData("ReturnTaskOfString_NullValue")]
    [InlineData("ReturnTaskOfObject_NullValue")]
    [InlineData("ReturnObject_NullValue")]
    public async Task
        NoContentFormatter_ForNullValue_AndTreatNullAsNoContentFlagSetToFalse_DoesNotGetSelected(string actionName)
    {
        // Arrange & Act
        var response = await Client.GetAsync("http://localhost/NoContentDoNotTreatNullValueAsNoContent/" +
                                             actionName);

        // Assert
        Assert.Null(response.Content.Headers.ContentType);
        var body = await response.Content.ReadAsStringAsync();
        // Response body is empty instead of null.
        Assert.Empty(body);
        Assert.Equal(HttpStatusCode.NotAcceptable, response.StatusCode);
    }
}
