// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.ResponseCaching;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.NewResponseCaching.Tests;

public class ResponseCachingPolicyVaryTests
{
    [Fact]
    public async Task VaryHeaderAddedWhenResponseStarts_IsIgnored()
    {
        // Like Response Compression for requests which don't accept compression: Response Caching reads the Vary header before.
        var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching(contextAction: context => context.Response.OnStarting(() =>
        {
            context.Response.Headers.Vary = HeaderNames.From;
            return Task.CompletedTask;
        }));

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();

            var initialResponse = await SendAsync(client, from: "user@example.com", language: null);
            var subsequentResponse = await SendAsync(client, from: "user2@example.com", language: null);

            Assert.Equal(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task VaryByQueryKeysSetAfterResponseStarts_AreIgnored()
    {
        // Like Response Caching, which reads the query keys when the response starts.
        var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching(contextAction: context => context.Response.OnStarting(() =>
        {
            context.Features.Get<IResponseCachingFeature>().VaryByQueryKeys = ["query"];
            return Task.CompletedTask;
        }));

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();

            var initialResponse = await client.GetAsync("?query=a");
            var subsequentResponse = await client.GetAsync("?query=b");

            Assert.Equal(initialResponse.Headers.GetValues("X-Value"), subsequentResponse.Headers.GetValues("X-Value"));
        }
    }

    [Fact]
    public async Task VaryHeaderNames_AreCombinedWithDeclaredHeaderNames()
    {
        var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching(
            contextAction: context => context.Response.Headers.Vary = HeaderNames.From,
            configurePolicy: policy => policy.SetVaryByHeader(HeaderNames.AcceptLanguage));

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();

            var initialResponse = await SendAsync(client, from: "user@example.com", language: "de");
            var cachedResponse = await SendAsync(client, from: "user@example.com", language: "de");
            var otherLanguageResponse = await SendAsync(client, from: "user@example.com", language: "en");
            var otherFromResponse = await SendAsync(client, from: "user2@example.com", language: "de");
            var subsequentResponse = await SendAsync(client, from: "user@example.com", language: "de");

            Assert.Equal(initialResponse, cachedResponse);
            Assert.NotEqual(initialResponse, otherLanguageResponse);
            Assert.NotEqual(initialResponse, otherFromResponse);
            Assert.NotEqual(otherLanguageResponse, otherFromResponse);
            Assert.Equal(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task VaryHeaderNames_AreForgotten_WhenResponseNoLongerVaries()
    {
        var vary = HeaderNames.From;
        var builders = ResponseCachingTestUtils.CreateBuildersWithResponseCaching(contextAction: context => context.Response.Headers.Vary = vary);

        foreach (var builder in builders)
        {
            vary = HeaderNames.From;
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();

            var initialResponse = await SendAsync(client, from: "user@example.com", language: null, cacheControl: "no-cache");
            vary = null;
            var unvariedResponse = await SendAsync(client, from: "user2@example.com", language: null, cacheControl: "no-cache");
            var subsequentResponse = await SendAsync(client, from: "user3@example.com", language: null);

            Assert.NotEqual(initialResponse, unvariedResponse);
            Assert.Equal(unvariedResponse, subsequentResponse);
        }
    }

    private static async Task<string> SendAsync(HttpClient client, string from, string language, string cacheControl = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "");
        request.Headers.TryAddWithoutValidation(HeaderNames.From, from);
        if (language is not null)
        {
            request.Headers.TryAddWithoutValidation(HeaderNames.AcceptLanguage, language);
        }
        if (cacheControl is not null)
        {
            request.Headers.TryAddWithoutValidation(HeaderNames.CacheControl, cacheControl);
        }

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return response.Headers.GetValues("X-Value").Single();
    }
}
