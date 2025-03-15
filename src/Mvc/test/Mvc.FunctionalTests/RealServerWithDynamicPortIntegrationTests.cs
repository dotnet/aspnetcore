// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class RealServerWithDynamicPortIntegrationTests : IClassFixture<KestrelBasedWapFactory>
{
    public KestrelBasedWapFactory Factory { get; }

    public RealServerWithDynamicPortIntegrationTests(KestrelBasedWapFactory factory)
    {
        Factory = factory;

        // Use dynamic port
        Factory.UseKestrel(0);
    }

    [Fact]
    public async Task ServerReachableViaGenericHttpClient()
    {
        // Arrange
        using var client = new HttpClient();
        using var factoryClient = Factory.CreateClient();
        client.BaseAddress = factoryClient.BaseAddress;

        // Act
        using var response = await client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
