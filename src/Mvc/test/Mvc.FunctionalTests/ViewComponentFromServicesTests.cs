// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class ViewComponentFromServicesTest : IClassFixture<MvcTestFixture<ControllersFromServicesWebSite.Startup>>
{
    public ViewComponentFromServicesTest(MvcTestFixture<ControllersFromServicesWebSite.Startup> fixture)
    {
        Client = fixture.CreateDefaultClient();
    }

    public HttpClient Client { get; }

    [Fact]
    public async Task ViewComponentsWithConstructorInjectionAreCreatedAndActivated()
    {
        // Arrange
        var expected = "Value = 3";
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/another/InServicesViewComponent");

        // Act
        var response = await Client.SendAsync(request);
        var responseText = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(expected, responseText);
    }
}
