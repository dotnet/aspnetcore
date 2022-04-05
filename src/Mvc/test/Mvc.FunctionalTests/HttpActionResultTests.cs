// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using BasicWebSite.Models;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class HttpActionResultTests : IClassFixture<MvcTestFixture<BasicWebSite.StartupWithSystemTextJson>>
{
    public HttpActionResultTests(MvcTestFixture<BasicWebSite.StartupWithSystemTextJson> fixture)
    {
        Client = fixture.CreateDefaultClient();
    }

    public HttpClient Client { get; }

    [Fact]
    public async Task ActionCanReturnIResultWithContent()
    {
        // Arrange
        var id = 1;
        var url = $"/contact/{nameof(BasicWebSite.ContactApiController.ActionReturningObjectIResult)}/{id}";
        var response = await Client.GetAsync(url);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Contact>();
        Assert.NotNull(result);
        Assert.Equal(id, result.ContactId);
    }

    [Fact]
    public async Task ActionCanReturnIResultWithStatusCodeOnly()
    {
        // Arrange
        var url = $"/contact/{nameof(BasicWebSite.ContactApiController.ActionReturningStatusCodeIResult)}";
        var response = await Client.GetAsync(url);

        // Assert
        await response.AssertStatusCodeAsync(HttpStatusCode.NoContent);
    }
}
