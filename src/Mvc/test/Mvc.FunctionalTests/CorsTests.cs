// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class CorsTests : CorsTestsBase<CorsWebSite.StartupWithoutEndpointRouting>
{
    [Fact]
    public override async Task PreflightRequestOnNonCorsEnabledController_DoesNotMatchTheAction()
    {
        // Arrange
        var request = new HttpRequestMessage(new HttpMethod("OPTIONS"), "http://localhost/NonCors/Post");
        request.Headers.Add(CorsConstants.Origin, "http://example.com");
        request.Headers.Add(CorsConstants.AccessControlRequestMethod, "POST");

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
