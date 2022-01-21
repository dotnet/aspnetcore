// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RazorPagesWithEndpointRoutingTest : IClassFixture<MvcTestFixture<RazorPagesWebSite.Startup>>
{
    public RazorPagesWithEndpointRoutingTest(MvcTestFixture<RazorPagesWebSite.Startup> fixture)
    {
        Client = fixture.CreateDefaultClient();
    }

    public HttpClient Client { get; }

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
