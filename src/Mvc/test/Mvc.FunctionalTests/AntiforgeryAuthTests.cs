// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.InternalTesting;
using SecurityWebSite;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class AntiforgeryAuthTests : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<Startup>(LoggerFactory);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public MvcTestFixture<Startup> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public async Task AutomaticAuthenticationBeforeAntiforgery()
    {
        // Arrange & Act
        var response = await Client.PostAsync("http://localhost/Home/AutoAntiforgery", null);

        // Assert
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Home/Login", response.Headers.Location.AbsolutePath, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuthBeforeAntiforgery()
    {
        // Arrange & Act
        var response = await Client.GetAsync("http://localhost/Home/Antiforgery");

        // Assert
        // Redirected to login page, Antiforgery didn't fail yet
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Home/Login", response.Headers.Location.AbsolutePath, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IgnoreAntiforgeryOverridesAutoAntiforgery()
    {
        // Arrange & Act
        var response = await Client.PostAsync("http://localhost/Antiforgery/Index", content: null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AntiforgeryOverridesIgnoreAntiforgery()
    {
        // Arrange & Act
        var response = await Client.PostAsync("http://localhost/IgnoreAntiforgery/Index", content: null);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
