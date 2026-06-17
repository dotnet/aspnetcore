// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using FormatterWebSite;
using Microsoft.AspNetCore.InternalTesting;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class JsonPatchSampleTest : LoggedTest
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
    public async Task AddOperation_Works()
    {
        // Arrange
        var input = "[{ 'op': 'add', 'path': 'Reviews/-', 'value': { 'Rating': 3.5 }}]".Replace("'", "\"");
        var request = GetPatchRequest(input);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var product = JsonConvert.DeserializeObject<Product>(body);
        Assert.NotNull(product);
        Assert.NotNull(product.Reviews);
        Assert.Equal(3, product.Reviews.Count);
        Assert.Equal(3.5, product.Reviews[2].Rating);
    }

    [Fact]
    public async Task ReplaceOperation_Works()
    {
        // Arrange
        var input = "[{ 'op': 'replace', 'path': 'Reviews/0/Rating', 'value': 5 }]".Replace("'", "\"");
        var request = GetPatchRequest(input);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var product = JsonConvert.DeserializeObject<Product>(body);
        Assert.NotNull(product);
        Assert.NotNull(product.Reviews);
        Assert.Equal(2, product.Reviews.Count);
        Assert.Equal(5, product.Reviews[0].Rating);
    }

    [Fact]
    public async Task CopyOperation_Works()
    {
        // Arrange
        var input = "[{ 'op': 'copy', 'path': 'Reviews/1/Rating', 'from': 'Reviews/0/Rating'}]".Replace("'", "\"");
        var request = GetPatchRequest(input);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var product = JsonConvert.DeserializeObject<Product>(body);
        Assert.NotNull(product);
        Assert.NotNull(product.Reviews);
        Assert.Equal(2, product.Reviews.Count);
        Assert.Equal(4, product.Reviews[1].Rating);
        Assert.Equal(4, product.Reviews[0].Rating);
    }

    [Fact]
    public async Task MoveOperation_Works()
    {
        // Arrange
        var input = "[{ 'op': 'move', 'path': 'Reviews/1/Rating', 'from': 'Reviews/0/Rating'}]".Replace("'", "\"");
        var request = GetPatchRequest(input);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var product = JsonConvert.DeserializeObject<Product>(body);
        Assert.NotNull(product);
        Assert.NotNull(product.Reviews);
        Assert.Equal(2, product.Reviews.Count);
        Assert.Equal(4, product.Reviews[1].Rating);
        Assert.Equal(0, product.Reviews[0].Rating);
    }

    [Fact]
    public async Task RemoveOperation_Works()
    {
        // Arrange
        var input = "[{ 'op': 'remove', 'path': 'Reviews/0/Rating'}]".Replace("'", "\"");
        var request = GetPatchRequest(input);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var product = JsonConvert.DeserializeObject<Product>(body);
        Assert.NotNull(product);
        Assert.NotNull(product.Reviews);
        Assert.Equal(2, product.Reviews.Count);
        Assert.Equal(0, product.Reviews[0].Rating);
    }

    [Fact]
    public async Task AddOperation_InvalidValueForProperty_AddsErrorToModelState()
    {
        // Arrange
        var input = "[{ 'op': 'add', 'path': 'Reviews/-', 'value': { 'Rating': 'not-a-double' }}]".Replace("'", "\"");
        var request = GetPatchRequest(input);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InvalidOperation_AddsErrorToModelState()
    {
        // Arrange
        var input = "[{ 'op': 'invalid', 'path': 'Reviews/1/Rating', 'from': 'Reviews/0/Rating'}]".Replace("'", "\"");
        var request = GetPatchRequest(input);

        // Act
        var response = await Client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private HttpRequestMessage GetPatchRequest(string body)
    {
        return new HttpRequestMessage
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json-patch+json"),
            Method = new HttpMethod("PATCH"),
            RequestUri = new Uri("http://localhost/jsonpatch/PatchProduct")
        };
    }
}
