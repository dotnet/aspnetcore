// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.OutputCaching.Tests;

public class OutputCacheTests
{
    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    public async Task ServesCachedContent_IfAvailable(string method)
    {
        var builders = TestUtils.CreateBuildersWithOutputCaching();

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.SendAsync(TestUtils.CreateRequest(method, ""));
            var subsequentResponse = await client.SendAsync(TestUtils.CreateRequest(method, ""));

            await AssertCachedResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    public async Task ServesFreshContent_IfNotAvailable(string method)
    {
        var builders = TestUtils.CreateBuildersWithOutputCaching();

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.SendAsync(TestUtils.CreateRequest(method, ""));
            var subsequentResponse = await client.SendAsync(TestUtils.CreateRequest(method, "different"));

            await AssertFreshResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task ServesFreshContent_Post()
    {
        var builders = TestUtils.CreateBuildersWithOutputCaching();

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.PostAsync("", new StringContent(string.Empty));
            var subsequentResponse = await client.PostAsync("", new StringContent(string.Empty));

            await AssertFreshResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task ServesFreshContent_Head_Get()
    {
        var builders = TestUtils.CreateBuildersWithOutputCaching();

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var subsequentResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, ""));
            var initialResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, ""));

            await AssertFreshResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task ServesFreshContent_Get_Head()
    {
        var builders = TestUtils.CreateBuildersWithOutputCaching();

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, ""));
            var subsequentResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, ""));

            await AssertFreshResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    public async Task ServesCachedContent_If_CacheControlNoCache(string method)
    {
        var builders = TestUtils.CreateBuildersWithOutputCaching();

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();

            var initialResponse = await client.SendAsync(TestUtils.CreateRequest(method, ""));

            // verify the response is cached
            var cachedResponse = await client.SendAsync(TestUtils.CreateRequest(method, ""));
            await AssertCachedResponseAsync(initialResponse, cachedResponse);

            // assert cached response still served
            client.DefaultRequestHeaders.CacheControl =
                new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };
            var subsequentResponse = await client.SendAsync(TestUtils.CreateRequest(method, ""));

            await AssertCachedResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    public async Task ServesCachedContent_If_PragmaNoCache(string method)
    {
        var builders = TestUtils.CreateBuildersWithOutputCaching();

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();

            var initialResponse = await client.SendAsync(TestUtils.CreateRequest(method, ""));

            // verify the response is cached
            var cachedResponse = await client.SendAsync(TestUtils.CreateRequest(method, ""));
            await AssertCachedResponseAsync(initialResponse, cachedResponse);

            // assert cached response still served
            client.DefaultRequestHeaders.Pragma.Clear();
            client.DefaultRequestHeaders.Pragma.Add(new System.Net.Http.Headers.NameValueHeaderValue("no-cache"));
            var subsequentResponse = await client.SendAsync(TestUtils.CreateRequest(method, ""));

            await AssertCachedResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    public async Task ServesCachedContent_If_PathCasingDiffers(string method)
    {
        var builders = TestUtils.CreateBuildersWithOutputCaching();

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.SendAsync(TestUtils.CreateRequest(method, "path"));
            var subsequentResponse = await client.SendAsync(TestUtils.CreateRequest(method, "PATH"));

            await AssertCachedResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    public async Task ServesFreshContent_If_PathCasingDiffers(string method)
    {
        var options = new OutputCacheOptions { UseCaseSensitivePaths = true };
        var builders = TestUtils.CreateBuildersWithOutputCaching(options: options);

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.SendAsync(TestUtils.CreateRequest(method, "path"));
            var subsequentResponse = await client.SendAsync(TestUtils.CreateRequest(method, "PATH"));

            await AssertFreshResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    public async Task ServesFreshContent_If_ResponseExpired(string method)
    {
        var options = new OutputCacheOptions
        {
            DefaultExpirationTimeSpan = TimeSpan.FromMicroseconds(100)
        };

        var builders = TestUtils.CreateBuildersWithOutputCaching(options: options);

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.SendAsync(TestUtils.CreateRequest(method, ""));
            await Task.Delay(1);
            var subsequentResponse = await client.SendAsync(TestUtils.CreateRequest(method, ""));

            await AssertFreshResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    public async Task ServesFreshContent_If_Authorization_HeaderExists(string method)
    {
        var builders = TestUtils.CreateBuildersWithOutputCaching();

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("abc");
            var initialResponse = await client.SendAsync(TestUtils.CreateRequest(method, ""));
            var subsequentResponse = await client.SendAsync(TestUtils.CreateRequest(method, ""));

            await AssertFreshResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    public async Task ServesCachedContent_If_Authorization_HeaderExists(string method)
    {
        var options = new OutputCacheOptions();

        var builders = TestUtils.CreateBuildersWithOutputCaching(options: options);

        // This is added after the DefaultPolicy which disables caching for authenticated requests
        options.AddBasePolicy(b => b.AddPolicy<AllowTestPolicy>());

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("abc");
            var initialResponse = await client.SendAsync(TestUtils.CreateRequest(method, ""));
            var subsequentResponse = await client.SendAsync(TestUtils.CreateRequest(method, ""));

            await AssertCachedResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task ServesCachedContent_IfVaryHeader_Matches()
    {
        var builders = TestUtils.CreateBuildersWithOutputCaching(contextAction: context => context.Response.Headers.Vary = HeaderNames.From);

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            client.DefaultRequestHeaders.From = "user@example.com";
            var initialResponse = await client.GetAsync("");
            var subsequentResponse = await client.GetAsync("");

            await AssertCachedResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task ServesFreshContent_IfVaryHeader_Mismatches()
    {
        var options = new OutputCacheOptions();
        options.AddBasePolicy(b => b.SetVaryByHeader(HeaderNames.From).Build());

        var builders = TestUtils.CreateBuildersWithOutputCaching(options: options);

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            client.DefaultRequestHeaders.From = "user@example.com";
            var initialResponse = await client.GetAsync("");
            client.DefaultRequestHeaders.From = "user2@example.com";
            var subsequentResponse = await client.GetAsync("");

            await AssertFreshResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task ServesCachedContent_IfVaryQueryKeys_Matches()
    {
        var options = new OutputCacheOptions();
        options.AddBasePolicy(b => b.SetVaryByQuery("query"));

        var builders = TestUtils.CreateBuildersWithOutputCaching(options: options);

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.GetAsync("?query=value");
            var subsequentResponse = await client.GetAsync("?query=value");

            await AssertCachedResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task ServesCachedContent_IfVaryQueryKeysExplicit_Matches_QueryKeyCaseInsensitive()
    {
        var options = new OutputCacheOptions();
        options.AddBasePolicy(b => b.SetVaryByQuery("QueryA", "queryb"));

        var builders = TestUtils.CreateBuildersWithOutputCaching(options: options);

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.GetAsync("?querya=valuea&queryb=valueb");
            var subsequentResponse = await client.GetAsync("?QueryA=valuea&QueryB=valueb");

            await AssertCachedResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task ServesCachedContent_IfVaryQueryKeyStar_Matches_QueryKeyCaseInsensitive()
    {
        var options = new OutputCacheOptions();
        options.AddBasePolicy(b => b.SetVaryByQuery("*"));

        var builders = TestUtils.CreateBuildersWithOutputCaching(options: options);

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.GetAsync("?querya=valuea&queryb=valueb");
            var subsequentResponse = await client.GetAsync("?QueryA=valuea&QueryB=valueb");

            await AssertCachedResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task ServesCachedContent_IfVaryQueryKeyExplicit_Matches_OrderInsensitive()
    {
        var options = new OutputCacheOptions();
        options.AddBasePolicy(b => b.SetVaryByQuery("QueryB", "QueryA"));

        var builders = TestUtils.CreateBuildersWithOutputCaching(options: options);

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.GetAsync("?QueryA=ValueA&QueryB=ValueB");
            var subsequentResponse = await client.GetAsync("?QueryB=ValueB&QueryA=ValueA");

            await AssertCachedResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task ServesCachedContent_IfVaryQueryKeyStar_Matches_OrderInsensitive()
    {
        var options = new OutputCacheOptions();
        options.AddBasePolicy(b => b.SetVaryByQuery("*"));

        var builders = TestUtils.CreateBuildersWithOutputCaching(options: options);

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.GetAsync("?QueryA=ValueA&QueryB=ValueB");
            var subsequentResponse = await client.GetAsync("?QueryB=ValueB&QueryA=ValueA");

            await AssertCachedResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task ServesFreshContent_IfVaryQueryKey_Mismatches()
    {
        var options = new OutputCacheOptions();
        options.AddBasePolicy(b => b.SetVaryByQuery("query").Build());
        var builders = TestUtils.CreateBuildersWithOutputCaching(options: options);

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.GetAsync("?query=value");
            var subsequentResponse = await client.GetAsync("?query=value2");

            await AssertFreshResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task ServesCachedContent_IfOtherVaryQueryKey_Mismatches()
    {
        var options = new OutputCacheOptions();
        options.AddBasePolicy(b => b.SetVaryByQuery("query").Build());

        var builders = TestUtils.CreateBuildersWithOutputCaching(options: options);

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.GetAsync("?other=value1");
            var subsequentResponse = await client.GetAsync("?other=value2");

            await AssertCachedResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task ServesFreshContent_IfVaryQueryKeyExplicit_Mismatch_QueryKeyCaseSensitive()
    {
        var options = new OutputCacheOptions();
        options.AddBasePolicy(new VaryByQueryPolicy("QueryA", "QueryB"));
        var builders = TestUtils.CreateBuildersWithOutputCaching(options: options);

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.GetAsync("?querya=valuea&queryb=valueb");
            var subsequentResponse = await client.GetAsync("?querya=ValueA&queryb=ValueB");

            await AssertFreshResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task ServesFreshContent_IfVaryQueryKeyStar_Mismatch_QueryKeyValueCaseSensitive()
    {
        var options = new OutputCacheOptions();
        options.AddBasePolicy(new VaryByQueryPolicy("*"));
        var builders = TestUtils.CreateBuildersWithOutputCaching(options: options);

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.GetAsync("?querya=valuea&queryb=valueb");
            var subsequentResponse = await client.GetAsync("?querya=ValueA&queryb=ValueB");

            await AssertFreshResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task ServesCachedContent_IfRequestRequirements_NotMet()
    {
        var builders = TestUtils.CreateBuildersWithOutputCaching();

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.GetAsync("");
            client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue()
            {
                MaxAge = TimeSpan.FromSeconds(0)
            };
            var subsequentResponse = await client.GetAsync("");

            await AssertCachedResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task Serves504_IfOnlyIfCachedHeader_IsSpecified()
    {
        var builders = TestUtils.CreateBuildersWithOutputCaching();

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.GetAsync("");
            client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue()
            {
                OnlyIfCached = true
            };
            var subsequentResponse = await client.GetAsync("/different");

            initialResponse.EnsureSuccessStatusCode();
            Assert.Equal(System.Net.HttpStatusCode.GatewayTimeout, subsequentResponse.StatusCode);
        }
    }

    [Fact]
    public async Task ServesFreshContent_IfSetCookie_IsSpecified()
    {
        var builders = TestUtils.CreateBuildersWithOutputCaching(contextAction: context => context.Response.Headers.SetCookie = "cookieName=cookieValue");

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.GetAsync("");
            var subsequentResponse = await client.GetAsync("");

            await AssertFreshResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task ServesCachedContent_IfSubsequentRequestContainsNoStore()
    {
        var builders = TestUtils.CreateBuildersWithOutputCaching();

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.GetAsync("");
            client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue()
            {
                NoStore = true
            };
            var subsequentResponse = await client.GetAsync("");

            await AssertCachedResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task ServesCachedContent_IfInitialRequestContainsNoStore()
    {
        var builders = TestUtils.CreateBuildersWithOutputCaching();

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue()
            {
                NoStore = true
            };
            var initialResponse = await client.GetAsync("");
            var subsequentResponse = await client.GetAsync("");

            await AssertCachedResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task ServesCachedContent_IfInitialResponseContainsNoStore()
    {
        var builders = TestUtils.CreateBuildersWithOutputCaching(contextAction: context => context.Response.Headers.CacheControl = CacheControlHeaderValue.NoStoreString);

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.GetAsync("");
            var subsequentResponse = await client.GetAsync("");

            await AssertCachedResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task Serves304_IfIfModifiedSince_Satisfied()
    {
        var builders = TestUtils.CreateBuildersWithOutputCaching(contextAction: context =>
        {
            // Ensure these headers are also returned on the subsequent response
            context.Response.GetTypedHeaders().ETag = new EntityTagHeaderValue("\"E1\"");
            context.Response.Headers.ContentLocation = "/";
            context.Response.Headers.Vary = HeaderNames.From;
        });

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.GetAsync("");
            client.DefaultRequestHeaders.IfModifiedSince = DateTimeOffset.MaxValue;
            var subsequentResponse = await client.GetAsync("");

            initialResponse.EnsureSuccessStatusCode();
            Assert.Equal(System.Net.HttpStatusCode.NotModified, subsequentResponse.StatusCode);
            Assert304Headers(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task ServesCachedContent_IfIfModifiedSince_NotSatisfied()
    {
        var builders = TestUtils.CreateBuildersWithOutputCaching();

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.GetAsync("");
            client.DefaultRequestHeaders.IfModifiedSince = DateTimeOffset.MinValue;
            var subsequentResponse = await client.GetAsync("");

            await AssertCachedResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task Serves304_IfIfNoneMatch_Satisfied()
    {
        var builders = TestUtils.CreateBuildersWithOutputCaching(contextAction: context =>
        {
            context.Response.GetTypedHeaders().ETag = new EntityTagHeaderValue("\"E1\"");
            context.Response.Headers.ContentLocation = "/";
            context.Response.Headers.Vary = HeaderNames.From;
        });

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.GetAsync("");
            client.DefaultRequestHeaders.IfNoneMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue("\"E1\""));
            var subsequentResponse = await client.GetAsync("");

            initialResponse.EnsureSuccessStatusCode();
            Assert.Equal(System.Net.HttpStatusCode.NotModified, subsequentResponse.StatusCode);
            Assert304Headers(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task ServesCachedContent_IfIfNoneMatch_NotSatisfied()
    {
        var builders = TestUtils.CreateBuildersWithOutputCaching(contextAction: context => context.Response.GetTypedHeaders().ETag = new EntityTagHeaderValue("\"E1\""));

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.GetAsync("");
            client.DefaultRequestHeaders.IfNoneMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue("\"E2\""));
            var subsequentResponse = await client.GetAsync("");

            await AssertCachedResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task ServesCachedContent_IfBodySize_IsCacheable()
    {
        var options = new OutputCacheOptions
        {
            MaximumBodySize = 1000
        };
        options.AddBasePolicy(b => b.Build());

        var builders = TestUtils.CreateBuildersWithOutputCaching(options: options);

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.GetAsync("");
            var subsequentResponse = await client.GetAsync("");

            await AssertCachedResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task ServesFreshContent_IfBodySize_IsNotCacheable()
    {
        var builders = TestUtils.CreateBuildersWithOutputCaching(options: new OutputCacheOptions()
        {
            MaximumBodySize = 1
        });

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.GetAsync("");
            var subsequentResponse = await client.GetAsync("/different");

            await AssertFreshResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task ServesFreshContent_CaseSensitivePaths_IsNotCacheable()
    {
        var builders = TestUtils.CreateBuildersWithOutputCaching(options: new OutputCacheOptions()
        {
            UseCaseSensitivePaths = true
        });

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.GetAsync("/path");
            var subsequentResponse = await client.GetAsync("/Path");

            await AssertFreshResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task ServesCachedContent_WithoutReplacingCachedVaryBy_OnCacheMiss()
    {
        var builders = TestUtils.CreateBuildersWithOutputCaching(contextAction: context => context.Response.Headers.Vary = HeaderNames.From);

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            client.DefaultRequestHeaders.From = "user@example.com";
            var initialResponse = await client.GetAsync("");
            client.DefaultRequestHeaders.From = "user2@example.com";
            var otherResponse = await client.GetAsync("");
            client.DefaultRequestHeaders.From = "user@example.com";
            var subsequentResponse = await client.GetAsync("");

            await AssertCachedResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task ServesCachedContent_IfCachedVaryByNotUpdated_OnCacheMiss()
    {
        var builders = TestUtils.CreateBuildersWithOutputCaching(contextAction: context => context.Response.Headers.Vary = context.Request.Headers.Pragma);

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            client.DefaultRequestHeaders.From = "user@example.com";
            client.DefaultRequestHeaders.Pragma.Clear();
            client.DefaultRequestHeaders.Pragma.Add(new System.Net.Http.Headers.NameValueHeaderValue("From"));
            client.DefaultRequestHeaders.MaxForwards = 1;
            var initialResponse = await client.GetAsync("");
            client.DefaultRequestHeaders.From = "user2@example.com";
            client.DefaultRequestHeaders.Pragma.Clear();
            client.DefaultRequestHeaders.Pragma.Add(new System.Net.Http.Headers.NameValueHeaderValue("From"));
            client.DefaultRequestHeaders.MaxForwards = 2;
            var otherResponse = await client.GetAsync("");
            client.DefaultRequestHeaders.From = "user@example.com";
            client.DefaultRequestHeaders.Pragma.Clear();
            client.DefaultRequestHeaders.Pragma.Add(new System.Net.Http.Headers.NameValueHeaderValue("From"));
            client.DefaultRequestHeaders.MaxForwards = 1;
            var subsequentResponse = await client.GetAsync("");

            await AssertCachedResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task ServesCachedContent_IfAvailable_UsingHead_WithContentLength()
    {
        var builders = TestUtils.CreateBuildersWithOutputCaching();

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();
            var client = server.CreateClient();
            var initialResponse = await client.SendAsync(TestUtils.CreateRequest("HEAD", "?contentLength=10"));
            var subsequentResponse = await client.SendAsync(TestUtils.CreateRequest("HEAD", "?contentLength=10"));

            await AssertCachedResponseAsync(initialResponse, subsequentResponse);
        }
    }

    [Fact]
    public async Task MiddlewareFaultsAreObserved()
    {
        var builders = TestUtils.CreateBuildersWithOutputCaching(contextAction: _ => throw new SomeException());

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();

            for (int i = 0; i < 10; i++)
            {
                await RunClient(server);
            }
        }

        static async Task RunClient(TestServer server)
        {
            var client = server.CreateClient();
            await Assert.ThrowsAsync<SomeException>(
                () => client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "")));
        }
    }

    sealed class SomeException : Exception { }

    [Fact]
    public async Task ServesCorrectlyUnderConcurrentLoad()
    {
        var builders = TestUtils.CreateBuildersWithOutputCaching();

        foreach (var builder in builders)
        {
            using var host = builder.Build();

            await host.StartAsync();

            using var server = host.GetTestServer();

            var guid = await RunClient(server, -1);

            var clients = new Task<Guid>[1024];
            for (int i = 0; i < clients.Length; i++)
            {
                clients[i] = Task.Run(() => RunClient(server, i));
            }
            await Task.WhenAll(clients);

            // note already completed
            for (int i = 0; i < clients.Length; i++)
            {
                Assert.Equal(guid, await clients[i]);
            }
        }

        static async Task<Guid> RunClient(TestServer server, int id)
        {
            string s = null;
            try
            {
                var client = server.CreateClient();
                var resp = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, ""));
                var len = resp.Content.Headers.ContentLength;
                s = await resp.Content.ReadAsStringAsync();

                Assert.NotNull(len);
                Guid value;
                switch (len.Value)
                {
                    case 36:
                        // usually we just write a guid
                        Assert.True(Guid.TryParse(s, out value));
                        break;
                    case 98:
                        // the file-based builder prepends extra data
                        Assert.True(Guid.TryParse(s.Substring(s.Length - 36), out value));
                        break;
                    default:
                        Assert.Fail($"Unexpected length: {len.Value}");
                        value = Guid.NewGuid(); // not reached
                        break;
                }
                return value;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Client {id} failed; payload '{s}', failure: {ex.Message}", ex);
            }
        }
    }

    private static void Assert304Headers(HttpResponseMessage initialResponse, HttpResponseMessage subsequentResponse)
    {
        // https://tools.ietf.org/html/rfc7232#section-4.1
        // The server generating a 304 response MUST generate any of the
        // following header fields that would have been sent in a 200 (OK)
        // response to the same request: Cache-Control, Content-Location, Date,
        // ETag, Expires, and Vary.

        Assert.Equal(initialResponse.Headers.CacheControl, subsequentResponse.Headers.CacheControl);
        Assert.Equal(initialResponse.Content.Headers.ContentLocation, subsequentResponse.Content.Headers.ContentLocation);
        Assert.Equal(initialResponse.Headers.Date, subsequentResponse.Headers.Date);
        Assert.Equal(initialResponse.Headers.ETag, subsequentResponse.Headers.ETag);
        Assert.Equal(initialResponse.Content.Headers.Expires, subsequentResponse.Content.Headers.Expires);
        Assert.Equal(initialResponse.Headers.Vary, subsequentResponse.Headers.Vary);
    }

    private static async Task AssertCachedResponseAsync(HttpResponseMessage initialResponse, HttpResponseMessage subsequentResponse)
    {
        initialResponse.EnsureSuccessStatusCode();
        subsequentResponse.EnsureSuccessStatusCode();

        foreach (var header in initialResponse.Headers)
        {
            Assert.Equal(initialResponse.Headers.GetValues(header.Key), subsequentResponse.Headers.GetValues(header.Key));
        }
        Assert.True(subsequentResponse.Headers.Contains(HeaderNames.Age));
        Assert.Equal(await initialResponse.Content.ReadAsStringAsync(), await subsequentResponse.Content.ReadAsStringAsync());
    }

    private static async Task AssertFreshResponseAsync(HttpResponseMessage initialResponse, HttpResponseMessage subsequentResponse)
    {
        initialResponse.EnsureSuccessStatusCode();
        subsequentResponse.EnsureSuccessStatusCode();

        Assert.False(subsequentResponse.Headers.Contains(HeaderNames.Age));

        if (initialResponse.RequestMessage.Method == HttpMethod.Head &&
            subsequentResponse.RequestMessage.Method == HttpMethod.Head)
        {
            Assert.True(initialResponse.Headers.Contains("X-Value"));
            Assert.NotEqual(initialResponse.Headers.GetValues("X-Value"), subsequentResponse.Headers.GetValues("X-Value"));
        }
        else
        {
            Assert.NotEqual(await initialResponse.Content.ReadAsStringAsync(), await subsequentResponse.Content.ReadAsStringAsync());
        }
    }
}
