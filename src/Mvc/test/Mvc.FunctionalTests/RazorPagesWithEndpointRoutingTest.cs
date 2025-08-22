// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RazorPagesWithEndpointRoutingTest : LoggedTest
{
    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<RazorPagesWebSite.Startup>(LoggerFactory);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public MvcTestFixture<RazorPagesWebSite.Startup> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public async Task Authorize_AppliedUsingConvention_Works()
    {
        // Act
        var response = await Client.GetAsync("/Conventions/AuthFolder");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.Redirect);
        Assert.Equal("/Login?ReturnUrl=%2FConventions%2FAuthFolder", response.Headers.Location.PathAndQuery);
    }

    [Fact]
    public async Task Authorize_AppliedUsingConvention_CanByOverridenByAllowAnonymousAppliedToModel()
    {
        // Act
        var response = await Client.GetAsync("/Conventions/AuthFolder/AnonymousViaModel");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Hello from Anonymous", content.Trim());
    }

    [Fact]
    public async Task Authorize_AppliedUsingAttributeOnModel_Works()
    {
        // Act
        var response = await Client.GetAsync("/ModelWithAuthFilter");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.Redirect);
        Assert.Equal("/Login?ReturnUrl=%2FModelWithAuthFilter", response.Headers.Location.PathAndQuery);
    }

    [Fact]
    public async Task Authorize_WithEndpointRouting_WorksForControllers()
    {
        // Act
        var response = await Client.GetAsync("/AuthorizedAction/Index");

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.Redirect);
        Assert.Equal("/Login?ReturnUrl=%2FAuthorizedAction%2FIndex", response.Headers.Location.PathAndQuery);
    }
}
