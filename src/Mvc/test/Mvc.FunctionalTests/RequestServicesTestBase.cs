// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Net.Http.Headers;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

// Each of these tests makes two requests, because we want each test to verify that the data is
// PER-REQUEST and does not linger around to impact the next request.
public abstract class RequestServicesTestBase<TStartup> : LoggedTest where TStartup : class
{
    private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
        builder.UseStartup<TStartup>();

    protected override void Initialize(TestContext context, MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper)
    {
        base.Initialize(context, methodInfo, testMethodArguments, testOutputHelper);
        Factory = new MvcTestFixture<TStartup>(LoggerFactory).WithWebHostBuilder(ConfigureWebHostBuilder);
        Client = Factory.CreateDefaultClient();
    }

    public override void Dispose()
    {
        Factory.Dispose();
        base.Dispose();
    }

    public WebApplicationFactory<TStartup> Factory { get; private set; }
    public HttpClient Client { get; private set; }

    [Fact]
    public abstract Task HasEndpointMatch();

    [Theory]
    [InlineData("http://localhost/RequestScopedService/FromFilter")]
    [InlineData("http://localhost/RequestScopedService/FromView")]
    [InlineData("http://localhost/RequestScopedService/FromViewComponent")]
    [InlineData("http://localhost/RequestScopedService/FromActionArgument")]
    [InlineData("http://localhost/RequestScopedService/FromProperty")]
    public async Task RequestServices(string url)
    {
        for (var i = 0; i < 2; i++)
        {
            // Arrange
            var requestId = Guid.NewGuid().ToString();
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation(HeaderNames.RequestId, requestId);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            response.EnsureSuccessStatusCode();
            var body = (await response.Content.ReadAsStringAsync()).Trim();
            Assert.Equal(requestId, body);
        }
    }

    [Fact]
    public async Task RequestServices_TagHelper()
    {
        // Arrange
        var url = "http://localhost/RequestScopedService/FromTagHelper";

        // Act & Assert
        for (var i = 0; i < 2; i++)
        {
            var requestId = Guid.NewGuid().ToString();
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation(HeaderNames.RequestId, requestId);

            var response = await Client.SendAsync(request);

            var body = (await response.Content.ReadAsStringAsync()).Trim();

            var expected = "<request-scoped>" + requestId + "</request-scoped>";
            Assert.Equal(expected, body);
        }
    }

    [Fact]
    public async Task RequestServices_Constraint()
    {
        // Arrange
        var url = "http://localhost/RequestScopedService/FromConstraint";

        // Act & Assert
        var requestId1 = "b40f6ec1-8a6b-41c1-b3fe-928f581ebaf5";
        var request1 = new HttpRequestMessage(HttpMethod.Get, url);
        request1.Headers.TryAddWithoutValidation(HeaderNames.RequestId, requestId1);

        var response1 = await Client.SendAsync(request1);

        var body1 = (await response1.Content.ReadAsStringAsync()).Trim();
        Assert.Equal(requestId1, body1);

        var requestId2 = Guid.NewGuid().ToString();
        var request2 = new HttpRequestMessage(HttpMethod.Get, url);
        request2.Headers.TryAddWithoutValidation(HeaderNames.RequestId, requestId2);

        var response2 = await Client.SendAsync(request2);
        Assert.Equal(HttpStatusCode.NotFound, response2.StatusCode);
    }
}
