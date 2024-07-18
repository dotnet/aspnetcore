// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.StaticAssets.Tests;

public class StaticAssetsIntegrationTests
{
    [Fact]
    public async Task CanServeAssetsFromManifestAsync()
    {
        // Arrange
        var appName = nameof(CanServeAssetsFromManifestAsync);
        var (contentRoot, webRoot) = ConfigureAppPaths(appName);

        CreateTestManifest(
            appName,
            webRoot,
            [
                new TestResource("sample.txt", "Hello, World!", false),
            ]);

        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
        {
            ApplicationName = appName,
            ContentRootPath = contentRoot,
            EnvironmentName = "Development",
            WebRootPath = webRoot
        });
        builder.WebHost.ConfigureServices(services =>
        {
            services.AddRouting();
        });
        builder.WebHost.UseTestServer();

        var app = builder.Build();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapStaticAssets();
        });

        await app.StartAsync();

        var client = app.GetTestClient();

        // Act
        var response = await client.GetAsync("/sample.txt");
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal($"\"{GetEtag("Hello, World!")}\"", response.Headers.ETag.Tag);
        Assert.Equal(13, response.Content.Headers.ContentLength);
        Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
        Assert.Equal("Hello, World!", await response.Content.ReadAsStringAsync());

        Directory.Delete(webRoot, true);
    }

    [Fact]
    public async Task CachingHeadersAreDisabled_InDevelopment()
    {
        // Arrange
        var appName = nameof(CachingHeadersAreDisabled_InDevelopment);
        var (contentRoot, webRoot) = ConfigureAppPaths(appName);

        CreateTestManifest(
            appName,
            webRoot,
            [
                new TestResource("sample.txt", "Hello, World!", false, [new("Cache-Control", "immutable")]),
            ]);

        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
        {
            ApplicationName = appName,
            ContentRootPath = contentRoot,
            EnvironmentName = "Development",
            WebRootPath = webRoot
        });
        builder.WebHost.ConfigureServices(services =>
        {
            services.AddRouting();
        });
        builder.WebHost.UseTestServer();

        var app = builder.Build();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapStaticAssets();
        });

        await app.StartAsync();

        var client = app.GetTestClient();

        // Act
        var response = await client.GetAsync("/sample.txt");
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal($"\"{GetEtag("Hello, World!")}\"", response.Headers.ETag.Tag);
        Assert.Equal(13, response.Content.Headers.ContentLength);
        Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
        Assert.Equal("Hello, World!", await response.Content.ReadAsStringAsync());
        Assert.True(response.Headers.CacheControl.NoCache);

        Directory.Delete(webRoot, true);
    }

    [Fact]
    public async Task CanEnable_CachingHeadersAreDisabled_InDevelopment()
    {
        // Arrange
        var appName = nameof(CanEnable_CachingHeadersAreDisabled_InDevelopment);
        var (contentRoot, webRoot) = ConfigureAppPaths(appName);

        CreateTestManifest(
            appName,
            webRoot,
            [
                new TestResource("sample.txt", "Hello, World!", false, [new("Cache-Control", "immutable")]),
            ]);

        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
        {
            ApplicationName = appName,
            ContentRootPath = contentRoot,
            EnvironmentName = "Development",
            WebRootPath = webRoot
        });
        builder.WebHost.ConfigureServices(services =>
        {
            services.AddRouting();
        });
        builder.Configuration["EnableStaticAssetsDevelopmentCaching"] = "true";
        builder.WebHost.UseTestServer();

        var app = builder.Build();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapStaticAssets();
        });

        await app.StartAsync();

        var client = app.GetTestClient();

        // Act
        var response = await client.GetAsync("/sample.txt");
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal($"\"{GetEtag("Hello, World!")}\"", response.Headers.ETag.Tag);
        Assert.Equal(13, response.Content.Headers.ContentLength);
        Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
        Assert.Equal("Hello, World!", await response.Content.ReadAsStringAsync());
        Assert.Equal("immutable", response.Headers.CacheControl.ToString());

        Directory.Delete(webRoot, true);
    }

    [Fact]
    public async Task Integrity_IsDisabled_InDevelopment()
    {
        // Arrange
        var appName = nameof(Integrity_IsDisabled_InDevelopment);
        var (contentRoot, webRoot) = ConfigureAppPaths(appName);

        CreateTestManifest(
            appName,
            webRoot,
            [
                new TestResource("sample.txt", "Hello, World!", false, [new("Cache-Control", "immutable")]),
            ]);

        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
        {
            ApplicationName = appName,
            ContentRootPath = contentRoot,
            EnvironmentName = "Development",
            WebRootPath = webRoot
        });
        builder.WebHost.ConfigureServices(services =>
        {
            services.AddRouting();
        });
        builder.WebHost.UseTestServer();

        var app = builder.Build();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            var builder = endpoints.MapStaticAssets();
            var descriptors = builder.Descriptors;

            endpoints.MapGet("/has-integrity", context =>
            {
                var descriptor = descriptors[0];
                var integrity = descriptors[0].Properties.FirstOrDefault(p => p.Name == "integrity");
                if (integrity != null)
                {
                    context.Response.StatusCode = 400;
                }
                return Task.CompletedTask;
            });
        });

        await app.StartAsync();

        var client = app.GetTestClient();

        // Act
        var response = await client.GetAsync("/has-integrity");
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Directory.Delete(webRoot, true);
    }

    [Fact]
    public async Task CanEnableIntegrity_InDevelopment()
    {
        // Arrange
        var appName = nameof(Integrity_IsDisabled_InDevelopment);
        var (contentRoot, webRoot) = ConfigureAppPaths(appName);

        CreateTestManifest(
            appName,
            webRoot,
            [
                new TestResource("sample.txt", "Hello, World!", false, [new("Cache-Control", "immutable")]),
            ]);

        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
        {
            ApplicationName = appName,
            ContentRootPath = contentRoot,
            EnvironmentName = "Development",
            WebRootPath = webRoot
        });
        builder.WebHost.ConfigureServices(services =>
        {
            services.AddRouting();
        });
        builder.WebHost.UseTestServer();
        builder.Configuration["EnableStaticAssetsDevelopmentIntegrity"] = "true";
        var app = builder.Build();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            var builder = endpoints.MapStaticAssets();
            var descriptors = builder.Descriptors;

            endpoints.MapGet("/has-integrity", context =>
            {
                var descriptor = descriptors[0];
                var integrity = descriptors[0].Properties.FirstOrDefault(p => p.Name == "integrity");
                if (integrity == null)
                {
                    context.Response.StatusCode = 400;
                }
                return Task.CompletedTask;
            });
        });

        await app.StartAsync();

        var client = app.GetTestClient();

        // Act
        var response = await client.GetAsync("/has-integrity");
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Directory.Delete(webRoot, true);
    }

    [Fact]
    public async Task CanServeNewFilesAddedAfterBuildDuringDevelopment()
    {
        // Arrange
        var appName = nameof(CanServeNewFilesAddedAfterBuildDuringDevelopment);
        var (contentRoot, webRoot) = ConfigureAppPaths(appName);

        CreateTestManifest(
            appName,
            webRoot,
            []);

        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
        {
            ApplicationName = appName,
            ContentRootPath = contentRoot,
            EnvironmentName = "Development",
            WebRootPath = webRoot
        });

        builder.WebHost.UseSetting(StaticAssetDevelopmentRuntimeHandler.ReloadStaticAssetsAtRuntimeKey, "true");
        builder.WebHost.ConfigureServices(services =>
        {
            services.AddRouting();
        });
        builder.WebHost.UseTestServer();

        var app = builder.Build();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapStaticAssets();
        });

        await app.StartAsync();

        var filePath = Path.Combine(webRoot, "sample.txt");
        var lastModified = DateTimeOffset.UtcNow;
        File.WriteAllText(filePath, "Hello, World!");

        var client = app.GetTestClient();

        // Act
        var response = await client.GetAsync("/sample.txt");
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal($"\"3/1gIbsr1bCvZ2KQgJ7DpTGR3YHH9wpLKGiKNiGCmG8=\"", response.Headers.ETag.Tag);
        Assert.Equal(13, response.Content.Headers.ContentLength);
        Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
        Assert.Equal("Hello, World!", await response.Content.ReadAsStringAsync());

        Directory.Delete(webRoot, true);
    }

    [Fact]
    public async Task CanModifyAssetsOnTheFlyInDevelopment()
    {
        // Arrange
        var appName = nameof(CanModifyAssetsOnTheFlyInDevelopment);
        var (contentRoot, webRoot) = ConfigureAppPaths(appName);

        CreateTestManifest(
            appName,
            webRoot,
            [
                new TestResource("sample.txt", "Hello, World!", false),
            ]);

        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
        {
            ApplicationName = appName,
            ContentRootPath = contentRoot,
            EnvironmentName = "Development",
            WebRootPath = webRoot
        });
        builder.WebHost.UseSetting(StaticAssetDevelopmentRuntimeHandler.ReloadStaticAssetsAtRuntimeKey, "true");
        builder.WebHost.ConfigureServices(services =>
        {
            services.AddRouting();
        });
        builder.WebHost.UseTestServer();

        var app = builder.Build();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapStaticAssets();
        });

        await app.StartAsync();

        var client = app.GetTestClient();

        File.WriteAllText(Path.Combine(webRoot, "sample.txt"), "Hello, World! Modified");

        // Act
        var response = await client.GetAsync("/sample.txt");
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal($"\"+fvSyRQcr4/t/rcA0u1KfZ8c3CpXxBDxsxDhnAftNqg=\"", response.Headers.ETag.Tag);
        Assert.Equal(22, response.Content.Headers.ContentLength);
        Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
        Assert.Equal("Hello, World! Modified", await response.Content.ReadAsStringAsync());

        Directory.Delete(webRoot, true);
    }

    [Fact]
    public async Task CanModifyAssetsWithCompressedVersionsOnTheFlyInDevelopment()
    {
        // Arrange
        var appName = nameof(CanModifyAssetsWithCompressedVersionsOnTheFlyInDevelopment);
        var (contentRoot, webRoot) = ConfigureAppPaths(appName);

        CreateTestManifest(
            appName,
            webRoot,
            [
                new TestResource("sample.txt", "Hello, World!", true),
            ]);

        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
        {
            ApplicationName = appName,
            ContentRootPath = contentRoot,
            EnvironmentName = "Development",
            WebRootPath = webRoot
        });
        builder.WebHost.UseSetting(StaticAssetDevelopmentRuntimeHandler.ReloadStaticAssetsAtRuntimeKey, "true");
        builder.WebHost.ConfigureServices(services =>
        {
            services.AddRouting();
        });
        builder.WebHost.UseTestServer();

        var app = builder.Build();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapStaticAssets();
        });

        await app.StartAsync();

        var client = app.GetTestClient();

        File.WriteAllText(Path.Combine(webRoot, "sample.txt"), "Hello, World! Modified");

        // Act
        var message = new HttpRequestMessage(HttpMethod.Get, "/sample.txt");
        message.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        var response = await client.SendAsync(message);

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(GetGzipEtag(Path.Combine(webRoot, "sample.txt")), response.Headers.ETag.Tag);
        Assert.Equal(55, response.Content.Headers.ContentLength);
        Assert.Equal("text/plain", response.Content.Headers.ContentType.ToString());
        var gzipContent = await response.Content.ReadAsStreamAsync();
        using var gzipStream = new GZipStream(gzipContent, CompressionMode.Decompress);
        using var reader = new StreamReader(gzipStream);
        var content = reader.ReadToEnd();
        Assert.Equal("Hello, World! Modified", content);

        Directory.Delete(webRoot, true);
    }

    private string GetGzipEtag(string filePath)
    {
        using var stream = new MemoryStream();
        using (var fileStream = File.OpenRead(filePath))
        {
            using var gzipStream = new GZipStream(stream, CompressionLevel.NoCompression, leaveOpen: true);
            fileStream.CopyTo(gzipStream);
            gzipStream.Flush();
        }
        stream.Flush();
        stream.Seek(0, SeekOrigin.Begin);

        return $"\"{Convert.ToBase64String(SHA256.HashData(stream))}\"";
    }

    private static (string contentRoot, string webRoot) ConfigureAppPaths(string appName)
    {
        var contentRoot = Path.Combine(AppContext.BaseDirectory, appName);
        var webRoot = Path.Combine(contentRoot, "wwwroot");

        return (contentRoot, webRoot);
    }

    private static void CreateTestManifest(string appName, string webRoot, params Span<TestResource> resources)
    {
        Directory.CreateDirectory(webRoot);
        var manifestPath = Path.Combine(AppContext.BaseDirectory, $"{appName}.staticwebassets.endpoints.json");
        var manifest = new StaticAssetsManifest()
        {
            Version = 1
        };

        for (var i = 0; i < resources.Length; i++)
        {
            var resource = resources[i];
            var filePath = Path.Combine(webRoot, resource.Path);
            var lastModified = DateTimeOffset.UtcNow;
            File.WriteAllText(filePath, resource.Content);
            var hash = GetEtag(resource.Content);
            manifest.Endpoints.Add(new StaticAssetDescriptor
            {
                Route = resource.Path,
                AssetPath = resource.Path,
                Selectors = [],
                Properties = [new("integrity", $"sha256-{hash}")],
                ResponseHeaders = [
                    new ("Accept-Ranges", "bytes"),
                    new("Content-Length", resource.Content.Length.ToString(CultureInfo.InvariantCulture)),
                    new("Content-Type", GetContentType(filePath)),
                    new ("ETag", $"\"{hash}\""),
                    new("Last-Modified", lastModified.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture)),
                    ..(resource.AdditionalHeaders ?? [])
                ]
            });

            if (resource.IncludeCompressedVersion)
            {
                var compressedFilePath = Path.Combine(webRoot, resource.Path + ".gz");
                var length = CreateCompressedFile(compressedFilePath, resource);

                manifest.Endpoints.Add(new StaticAssetDescriptor
                {
                    Route = resource.Path,
                    AssetPath = $"{resource.Path}.gz",
                    Selectors = [new StaticAssetSelector("Content-Encoding", "gzip", "1.0")],
                    Properties = [],
                    ResponseHeaders = [
                        new ("Accept-Ranges", "bytes"),
                        new ("Content-Type", GetContentType(filePath)),

                        new ("Content-Length", length.ToString(CultureInfo.InvariantCulture)),
                        new ("ETag", $"W/\"{GetEtag(resource.Content)}\""),
                        new ("ETag", $"\"{GetEtagForFile(compressedFilePath)}\""),
                        new ("Last-Modified", lastModified.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture)),

                        new ("Content-Encoding", "gzip"),
                        new ("Vary", "Accept-Encoding"),
                    ]
                });
            }
        }
        using var stream = File.Create(manifestPath);
        using var writer = new Utf8JsonWriter(stream);
        JsonSerializer.Serialize(writer, manifest);
    }

    private static long CreateCompressedFile(string filePath, TestResource resource)
    {
        using var fileStream = File.Create(filePath);
        using var gzipStream = new GZipStream(fileStream, CompressionLevel.Fastest);
        using var compressedWriter = new StreamWriter(gzipStream);
        compressedWriter.Write(resource.Content);
        compressedWriter.Flush();
        return fileStream.Length;
    }

    private static string GetEtagForFile(string compressedFilePath)
    {
        using var stream = File.OpenRead(compressedFilePath);
        return Convert.ToBase64String(SHA256.HashData(stream));
    }

    private static string GetEtag(string content)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hash);
    }

    private static async Task<HttpClient> CreateClient()
    {
        // Arrange
        // These aren't used as we are replacing the file provider with a test one
        var (contentRoot, webRoot) = (AppContext.BaseDirectory, AppContext.BaseDirectory);

        var manifest = new StaticAssetsManifest()
        {
            Version = 1
        };
        manifest.Endpoints.Add(new StaticAssetDescriptor
        {
            Route = "sample.txt",
            AssetPath = "sample.txt",
            Selectors = [],
            Properties = [],
            ResponseHeaders = [
                new ("Accept-Ranges", "bytes"),
                new("Content-Length", "Hello, World!".Length.ToString(CultureInfo.InvariantCulture)),
                new("Content-Type", GetContentType("sample.txt")),
                new ("ETag", $"\"{GetEtag("Hello, World!")}\""),
                new("Last-Modified", new DateTimeOffset(2023,03,03,0,0,0,TimeSpan.Zero).ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture))
            ]
        });

        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions
        {
            ApplicationName = "InMemory",
            ContentRootPath = contentRoot,
            EnvironmentName = "Development",
            WebRootPath = webRoot
        });
        builder.Environment.WebRootFileProvider = new TestFileProvider(new TestResource[]
        {
            new("sample.txt", "Hello, World!", false),
        });
        builder.WebHost.ConfigureServices(services =>
        {
            services.AddRouting();
        });
        builder.WebHost.UseTestServer();

        var app = builder.Build();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapStaticAssets(manifest);
        });

        await app.StartAsync();

        return app.GetTestClient();
    }

    [Fact]
    public async Task ServerShouldReturnETag()
    {
        var client = await CreateClient();
        var response = await client.GetAsync("http://localhost/sample.txt");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(response.Headers.ETag);
        Assert.NotNull(response.Headers.ETag.Tag);
    }

    [Fact]
    public async Task SameETagShouldBeReturnedAgain()
    {
        var client = await CreateClient();
        var response1 = await client.GetAsync("http://localhost/sample.txt");
        var response2 = await client.GetAsync("http://localhost/sample.txt");
        Assert.Equal(response2.Headers.ETag, response1.Headers.ETag);
    }

    //// 14.24 If-Match
    //// If none of the entity tags match, or if "*" is given and no current
    //// entity exists, the server MUST NOT perform the requested method, and
    //// MUST return a 412 (Precondition Failed) response. This behavior is
    //// most useful when the client wants to prevent an updating method, such
    //// as PUT, from modifying a resource that has changed since the client
    //// last retrieved it.

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task IfMatchShouldReturn412WhenNotListed(HttpMethod method)
    {
        var client = await CreateClient();
        var req = new HttpRequestMessage(method, "http://localhost/sample.txt");
        req.Headers.Add("If-Match", "\"fake\"");
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.PreconditionFailed, resp.StatusCode);
    }

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task IfMatchShouldBeServedWhenListed(HttpMethod method)
    {
        var client = await CreateClient();
        var original = await client.GetAsync("http://localhost/sample.txt");

        var req = new HttpRequestMessage(method, "http://localhost/sample.txt");
        req.Headers.Add("If-Match", original.Headers.ETag.ToString());
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task IfMatchShouldBeServedForAsterisk(HttpMethod method)
    {
        var client = await CreateClient();
        var req = new HttpRequestMessage(method, "http://localhost/sample.txt");
        req.Headers.Add("If-Match", "*");
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Theory]
    [MemberData(nameof(UnsupportedMethods))]
    public async Task IfMatchShouldBeIgnoredForUnsupportedMethods(HttpMethod method)
    {
        var client = await CreateClient();
        var req = new HttpRequestMessage(method, "http://localhost/sample.txt");
        req.Headers.Add("If-Match", "*");
        var resp = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, resp.StatusCode);
    }

    // 14.26 If-None-Match
    // If any of the entity tags match the entity tag of the entity that
    // would have been returned in the response to a similar GET request
    // (without the If-None-Match header) on that resource, or if "*" is
    // given and any current entity exists for that resource, then the
    // server MUST NOT perform the requested method, unless required to do
    // so because the resource's modification date fails to match that
    // supplied in an If-Modified-Since header field in the request.
    // Instead, if the request method was GET or HEAD, the server SHOULD
    // respond with a 304 (Not Modified) response, including the cache-
    // related header fields (particularly ETag) of one of the entities that
    // matched. For all other request methods, the server MUST respond with
    // a status of 412 (Precondition Failed).

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task IfNoneMatchShouldReturn304ForMatching(HttpMethod method)
    {
        var client = await CreateClient();
        var resp1 = await client.GetAsync("http://localhost/sample.txt");

        var req2 = new HttpRequestMessage(method, "http://localhost/sample.txt");
        req2.Headers.Add("If-None-Match", resp1.Headers.ETag.ToString());
        var resp2 = await client.SendAsync(req2);
        Assert.Equal(HttpStatusCode.NotModified, resp2.StatusCode);
    }

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task IfNoneMatchAllShouldReturn304ForMatching(HttpMethod method)
    {
        var client = await CreateClient();
        var resp1 = await client.GetAsync("http://localhost/sample.txt");

        var req2 = new HttpRequestMessage(method, "http://localhost/sample.txt");
        req2.Headers.Add("If-None-Match", "*");
        var resp2 = await client.SendAsync(req2);
        Assert.Equal(HttpStatusCode.NotModified, resp2.StatusCode);
    }

    [Theory]
    [MemberData(nameof(UnsupportedMethods))]
    public async Task IfNoneMatchShouldBeIgnoredForNonTwoHundredAnd304Responses(HttpMethod method)
    {
        var client = await CreateClient();
        var resp1 = await client.GetAsync("http://localhost/sample.txt");

        var req2 = new HttpRequestMessage(method, "http://localhost/sample.txt");
        req2.Headers.Add("If-None-Match", resp1.Headers.ETag.ToString());
        var resp2 = await client.SendAsync(req2);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, resp2.StatusCode);
    }

    // 14.26 If-None-Match
    // If none of the entity tags match, then the server MAY perform the
    // requested method as if the If-None-Match header field did not exist,
    // but MUST also ignore any If-Modified-Since header field(s) in the
    // request. That is, if no entity tags match, then the server MUST NOT
    // return a 304 (Not Modified) response.

    // A server MUST use the strong comparison function (see section 13.3.3)
    // to compare the entity tags in If-Match.

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task ServerShouldReturnLastModified(HttpMethod method)
    {
        var client = await CreateClient();

        var response = await client.SendAsync(
            new HttpRequestMessage(method, "http://localhost/sample.txt"));

        Assert.NotNull(response.Content.Headers.LastModified);
        // Verify that DateTimeOffset is UTC
        Assert.Equal(response.Content.Headers.LastModified.Value.Offset, TimeSpan.Zero);
    }

    // 13.3.4
    // An HTTP/1.1 origin server, upon receiving a conditional request that
    // includes both a Last-Modified date (e.g., in an If-Modified-Since or
    // If-Unmodified-Since header field) and one or more entity tags (e.g.,
    // in an If-Match, If-None-Match, or If-Range header field) as cache
    // validators, MUST NOT return a response status of 304 (Not Modified)
    // unless doing so is consistent with all of the conditional header
    // fields in the request.

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task MatchingBothConditionsReturnsNotModified(HttpMethod method)
    {
        var client = await CreateClient();
        var req1 = new HttpRequestMessage(method, "http://localhost/sample.txt");
        var resp1 = await client.SendAsync(req1);

        var req2 = new HttpRequestMessage(method, "http://localhost/sample.txt");
        req2.Headers.Add("If-None-Match", resp1.Headers.ETag.ToString());
        req2.Headers.IfModifiedSince = resp1.Content.Headers.LastModified;
        var resp2 = await client.SendAsync(req2);

        Assert.Equal(HttpStatusCode.NotModified, resp2.StatusCode);
    }

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task MatchingAtLeastOneETagReturnsNotModified(HttpMethod method)
    {
        var client = await CreateClient();
        var req1 = new HttpRequestMessage(method, "http://localhost/sample.txt");
        var resp1 = await client.SendAsync(req1);
        var etag = resp1.Headers.ETag.ToString();

        var req2 = new HttpRequestMessage(method, "http://localhost/sample.txt");
        req2.Headers.Add("If-Match", etag + ", " + etag);

        var resp2 = await client.SendAsync(req2);

        Assert.Equal(HttpStatusCode.OK, resp2.StatusCode);

        var req3 = new HttpRequestMessage(method, "http://localhost/sample.txt");
        req3.Headers.Add("If-Match", etag + ", \"badetag\"");
        var resp3 = await client.SendAsync(req3);

        Assert.Equal(HttpStatusCode.OK, resp3.StatusCode);
    }

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task MissingEitherOrBothConditionsReturnsNormally(HttpMethod method)
    {
        var client = await CreateClient();
        var req1 = new HttpRequestMessage(method, "http://localhost/sample.txt");
        var resp1 = await client.SendAsync(req1);

        var lastModified = resp1.Content.Headers.LastModified.Value;
        var pastDate = lastModified.AddHours(-1);
        var futureDate = lastModified.AddHours(1);

        var req2 = new HttpRequestMessage(method, "http://localhost/sample.txt");
        req2.Headers.IfNoneMatch.Add(new EntityTagHeaderValue("\"fake\""));
        req2.Headers.IfModifiedSince = lastModified;
        var resp2 = await client.SendAsync(req2);

        var req3 = new HttpRequestMessage(method, "http://localhost/sample.txt");
        req3.Headers.IfNoneMatch.Add(resp1.Headers.ETag);
        req3.Headers.IfModifiedSince = pastDate;
        var resp3 = await client.SendAsync(req3);

        var req4 = new HttpRequestMessage(method, "http://localhost/sample.txt");
        req4.Headers.IfNoneMatch.Add(new EntityTagHeaderValue("\"fake\""));
        req4.Headers.IfModifiedSince = futureDate;
        var resp4 = await client.SendAsync(req4);

        Assert.Equal(HttpStatusCode.OK, resp2.StatusCode);
        Assert.Equal(HttpStatusCode.OK, resp3.StatusCode);
        Assert.Equal(HttpStatusCode.OK, resp4.StatusCode);
    }

    // 14.25 If-Modified-Since
    // The If-Modified-Since request-header field is used with a method to
    // make it conditional: if the requested variant has not been modified
    // since the time specified in this field, an entity will not be
    // returned from the server; instead, a 304 (not modified) response will
    // be returned without any message-body.

    // a) If the request would normally result in anything other than a
    //   200 (OK) status, or if the passed If-Modified-Since date is
    //   invalid, the response is exactly the same as for a normal GET.
    //   A date which is later than the server's current time is
    //   invalid.
    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task InvalidIfModifiedSinceDateFormatGivesNormalGet(HttpMethod method)
    {
        var client = await CreateClient();

        var req = new HttpRequestMessage(method, "http://localhost/sample.txt");
        req.Headers.TryAddWithoutValidation("If-Modified-Since", "bad-date");
        var res = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task FutureIfModifiedSinceDateFormatGivesNormalGet(HttpMethod method)
    {
        var client = await CreateClient();

        var req = new HttpRequestMessage(method, "http://localhost/sample.txt");
        req.Headers.IfModifiedSince = DateTimeOffset.Now.AddYears(1);
        var res = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    // b) If the variant has been modified since the If-Modified-Since
    //   date, the response is exactly the same as for a normal GET.

    // c) If the variant has not been modified since a valid If-
    //   Modified-Since date, the server SHOULD return a 304 (Not
    //   Modified) response.

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task IfModifiedSinceDateGreaterThanLastModifiedShouldReturn304(HttpMethod method)
    {
        var client = await CreateClient();

        var res1 = await client.SendAsync(
            new HttpRequestMessage(method, "http://localhost/sample.txt"));

        var req2 = new HttpRequestMessage(method, "http://localhost/sample.txt");
        req2.Headers.IfModifiedSince = DateTimeOffset.Now;
        var res2 = await client.SendAsync(req2);

        Assert.Equal(HttpStatusCode.NotModified, res2.StatusCode);
    }

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task SupportsIfModifiedDateFormats(HttpMethod method)
    {
        var client = await CreateClient();
        var res1 = await client.SendAsync(
            new HttpRequestMessage(method, "http://localhost/sample.txt"));

        var formats = new[]
        {
                "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
                "dddd, dd-MMM-yy HH:mm:ss 'GMT'",
                "ddd MMM  d HH:mm:ss yyyy"
            };

        foreach (var format in formats)
        {
            var req2 = new HttpRequestMessage(method, "sample.txt");
            req2.Headers.TryAddWithoutValidation("If-Modified-Since", DateTimeOffset.UtcNow.ToString(format, CultureInfo.InvariantCulture));
            var res2 = await client.SendAsync(req2);

            Assert.Equal(HttpStatusCode.NotModified, res2.StatusCode);
        }
    }

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task IfModifiedSinceDateLessThanLastModifiedShouldReturn200(HttpMethod method)
    {
        var client = await CreateClient();

        var res1 = await client.SendAsync(
            new HttpRequestMessage(method, "http://localhost/sample.txt"));

        var req2 = new HttpRequestMessage(method, "http://localhost/sample.txt");
        req2.Headers.IfModifiedSince = DateTimeOffset.MinValue;
        var res2 = await client.SendAsync(req2);

        Assert.Equal(HttpStatusCode.OK, res2.StatusCode);
    }

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task InvalidIfUnmodifiedSinceDateFormatGivesNormalGet(HttpMethod method)
    {
        var client = await CreateClient();

        var req = new HttpRequestMessage(method, "http://localhost/sample.txt");
        req.Headers.TryAddWithoutValidation("If-Unmodified-Since", "bad-date");
        var res = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task FutureIfUnmodifiedSinceDateFormatGivesNormalGet(HttpMethod method)
    {
        var client = await CreateClient();
        var req = new HttpRequestMessage(method, "http://localhost/sample.txt");
        req.Headers.IfUnmodifiedSince = DateTimeOffset.Now.AddYears(1);
        var res = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task IfUnmodifiedSinceDateLessThanLastModifiedShouldReturn412(HttpMethod method)
    {
        var client = await CreateClient();

        var res1 = await client.SendAsync(
            new HttpRequestMessage(method, "http://localhost/sample.txt"));

        var req2 = new HttpRequestMessage(method, "http://localhost/sample.txt");
        req2.Headers.IfUnmodifiedSince = DateTimeOffset.MinValue;
        var res2 = await client.SendAsync(req2);

        Assert.Equal(HttpStatusCode.PreconditionFailed, res2.StatusCode);
    }

    public static IEnumerable<object[]> SupportedMethods => new[]
    {
            new [] { HttpMethod.Get },
            new [] { HttpMethod.Head }
        };

    public static IEnumerable<object[]> UnsupportedMethods => new[]
    {
            new [] { HttpMethod.Post },
            new [] { HttpMethod.Put },
            new [] { HttpMethod.Options },
            new [] { HttpMethod.Trace },
            new [] { new HttpMethod("VERB") }
        };

    private static string GetContentType(string filePath)
    {
        return Path.GetExtension(filePath) switch
        {
            ".txt" => "text/plain",
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".json" => "application/json",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };
    }

    private record TestResource(string Path, string Content, bool IncludeCompressedVersion, StaticAssetResponseHeader[] AdditionalHeaders = null);

    private class TestFileProvider(TestResource[] testResources) : IFileProvider
    {
        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return NotFoundDirectoryContents.Singleton;
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            for (var i = 0; i < testResources.Length; i++)
            {
                if (testResources[i].Path == subpath)
                {
                    return new TestFileInfo(testResources[i]);
                }
            }

            return new NotFoundFileInfo(subpath);
        }

        public IChangeToken Watch(string filter)
        {
            return NullChangeToken.Singleton;
        }

        private class TestFileInfo(TestResource testResource) : IFileInfo
        {
            public bool Exists => true;

            public long Length => testResource.Content.Length;

            public string PhysicalPath => null;

            public string Name => Path.GetFileName(testResource.Path);

            public DateTimeOffset LastModified => new(2023, 03, 03, 0, 0, 0, TimeSpan.Zero);

            public bool IsDirectory => false;

            public Stream CreateReadStream()
            {
                var stream = new MemoryStream();
                var writer = new StreamWriter(stream);
                writer.Write(testResource.Content);
                writer.Flush();
                stream.Position = 0;
                return stream;
            }
        }
    }
}
