// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class TempDataInCookiesTest : TempDataTestBase<BasicWebSite.StartupWithoutEndpointRouting>
{
    private IServiceCollection _serviceCollection;

    protected override void ConfigureWebHostBuilder(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(serviceCollection => _serviceCollection = serviceCollection);
    }

    [Fact]
    public void VerifyNewtonsoftJsonTempDataSerializer()
    {
        // Arrange
        // This test could provide some diagnostics for the test failure reported in https://github.com/dotnet/aspnetcore-internal/issues/1803.
        // AddNewtonsoftJson attempts to replace the DefaultTempDataSerializer. The test failure indicates this failed but it's not clear why.
        // We'll capture the application's ServiceCollection and inspect the instance of ITempDataSerializer instances here. It might give us some
        // clues if the test fails again in the future.

        // Intentionally avoiding using Xunit.Assert to get more diagnostics.
        var tempDataSerializers = _serviceCollection.Where(f => f.ServiceType == typeof(TempDataSerializer)).ToList();
        if (tempDataSerializers.Count == 1 && tempDataSerializers[0].ImplementationType.FullName == "Microsoft.AspNetCore.Mvc.NewtonsoftJson.BsonTempDataSerializer")
        {
            return;
        }

        var builder = new StringBuilder();
        foreach (var serializer in tempDataSerializers)
        {
            var type = serializer.ImplementationType;
            builder.Append(serializer.ImplementationType.AssemblyQualifiedName);
        }

        throw new Exception($"Expected exactly one instance of TempDataSerializer based on NewtonsoftJson, but found {tempDataSerializers.Count} instance(s):" + Environment.NewLine + builder);
    }

    [Theory]
    [InlineData(ChunkingCookieManager.DefaultChunkSize)]
    [InlineData(ChunkingCookieManager.DefaultChunkSize * 1.5)]
    [InlineData(ChunkingCookieManager.DefaultChunkSize * 2)]
    [InlineData(ChunkingCookieManager.DefaultChunkSize * 3)]
    public async Task RoundTripLargeData_WorksWithChunkingCookies(int size)
    {
        // Arrange
        var character = 'a';
        var expected = new string(character, size);

        // Act 1
        var response = await Client.GetAsync($"/TempData/SetLargeValueInTempData?size={size}&character={character}");

        // Assert 1
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues(HeaderNames.SetCookie, out IEnumerable<string> setCookieValues));
        setCookieValues = setCookieValues.Where(cookie => cookie.Contains(CookieTempDataProvider.CookieName));
        Assert.NotEmpty(setCookieValues);
        // Verify that all the cookies from CookieTempDataProvider are within the maximum size
        foreach (var cookie in setCookieValues)
        {
            Assert.True(cookie.Length <= ChunkingCookieManager.DefaultChunkSize);
        }

        var cookieTempDataProviderCookies = setCookieValues
            .Select(setCookieValue => SetCookieHeaderValue.Parse(setCookieValue));
        foreach (var cookieTempDataProviderCookie in cookieTempDataProviderCookies)
        {
            Assert.NotNull(cookieTempDataProviderCookie.Value.Value);
            Assert.Equal("/", cookieTempDataProviderCookie.Path.AsSpan());
            Assert.Null(cookieTempDataProviderCookie.Domain.Value);
            Assert.False(cookieTempDataProviderCookie.Secure);
        }

        // Act 2
        response = await Client.SendAsync(GetRequest("/TempData/GetLargeValueFromTempData", response));

        // Assert 2
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(expected, body);
        Assert.True(response.Headers.TryGetValues(HeaderNames.SetCookie, out setCookieValues));
        var setCookieHeaderValue = setCookieValues
            .Select(setCookieValue => SetCookieHeaderValue.Parse(setCookieValue))
            .FirstOrDefault(setCookieHeader => setCookieHeader.Name == CookieTempDataProvider.CookieName);
        Assert.NotNull(setCookieHeaderValue);
        Assert.Equal(string.Empty, setCookieHeaderValue.Value.AsSpan());
        Assert.Equal("/", setCookieHeaderValue.Path.AsSpan());
        Assert.Null(setCookieHeaderValue.Domain.Value);
        Assert.NotNull(setCookieHeaderValue.Expires);
        Assert.True(setCookieHeaderValue.Expires < DateTimeOffset.Now); // expired cookie

        // Act 3
        response = await Client.SendAsync(GetRequest("/TempData/GetLargeValueFromTempData", response));

        // Assert 3
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Redirect_RetainsTempData_EvenIfAccessed_AndSetsAppropriateCookieValues()
    {
        // Arrange
        var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("value", "Foo"),
            };
        var content = new FormUrlEncodedContent(nameValueCollection);

        // Act 1
        var response = await Client.PostAsync("/TempData/SetTempData", content);

        // Assert 1
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues(HeaderNames.SetCookie, out IEnumerable<string> setCookieValues));
        var setCookieHeader = setCookieValues
            .Select(setCookieValue => SetCookieHeaderValue.Parse(setCookieValue))
            .FirstOrDefault(setCookieHeaderValue => setCookieHeaderValue.Name == CookieTempDataProvider.CookieName);
        Assert.NotNull(setCookieHeader);
        Assert.Equal("/", setCookieHeader.Path.AsSpan());
        Assert.Null(setCookieHeader.Domain.Value);
        Assert.False(setCookieHeader.Secure);
        Assert.Null(setCookieHeader.Expires);

        // Act 2
        var redirectResponse = await Client.SendAsync(GetRequest("/TempData/GetTempDataAndRedirect", response));

        // Assert 2
        Assert.Equal(HttpStatusCode.Redirect, redirectResponse.StatusCode);

        // Act 3
        response = await Client.SendAsync(GetRequest(redirectResponse.Headers.Location.ToString(), response));

        // Assert 3
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("Foo", body);
        Assert.True(response.Headers.TryGetValues(HeaderNames.SetCookie, out setCookieValues));
        setCookieHeader = setCookieValues
            .Select(setCookieValue => SetCookieHeaderValue.Parse(setCookieValue))
            .FirstOrDefault(setCookieHeaderValue => setCookieHeaderValue.Name == CookieTempDataProvider.CookieName);
        Assert.NotNull(setCookieHeader);
        Assert.Equal(string.Empty, setCookieHeader.Value.AsSpan());
        Assert.Equal("/", setCookieHeader.Path.AsSpan());
        Assert.Null(setCookieHeader.Domain.Value);
        Assert.NotNull(setCookieHeader.Expires);
        Assert.True(setCookieHeader.Expires < DateTimeOffset.Now); // expired cookie
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CookieTempDataProviderCookie_DoesNotSetsSecureAttributeOnCookie(bool secureRequest)
    {
        // Arrange
        var protocol = secureRequest ? "https" : "http";
        var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("value", "Foo"),
            };
        var content = new FormUrlEncodedContent(nameValueCollection);

        // Act
        var response = await Client.PostAsync($"{protocol}://localhost/TempData/SetTempData", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues(HeaderNames.SetCookie, out IEnumerable<string> setCookieValues));
        var setCookieHeader = setCookieValues
            .Select(setCookieValue => SetCookieHeaderValue.Parse(setCookieValue))
            .FirstOrDefault(setCookieHeaderValue => setCookieHeaderValue.Name == CookieTempDataProvider.CookieName);
        Assert.NotNull(setCookieHeader);
        Assert.Equal("/", setCookieHeader.Path.AsSpan());
        Assert.Null(setCookieHeader.Domain.Value);
        Assert.False(setCookieHeader.Secure);
        Assert.Null(setCookieHeader.Expires);
    }
}
