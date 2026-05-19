// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace Microsoft.AspNetCore.StaticFiles;

public class CacheHeaderTests
{
    [Fact]
    public async Task ServerShouldReturnETag()
    {
        using var host = await StaticFilesTestServer.Create(app => app.UseFileServer());
        using var server = host.GetTestServer();

        HttpResponseMessage response = await server.CreateClient().GetAsync("http://localhost/SubFolder/extra.xml");
        Assert.NotNull(response.Headers.ETag);
        Assert.NotNull(response.Headers.ETag.Tag);
    }

    [Fact]
    public async Task SameETagShouldBeReturnedAgain()
    {
        using var host = await StaticFilesTestServer.Create(app => app.UseFileServer());
        using var server = host.GetTestServer();

        HttpResponseMessage response1 = await server.CreateClient().GetAsync("http://localhost/SubFolder/extra.xml");
        HttpResponseMessage response2 = await server.CreateClient().GetAsync("http://localhost/SubFolder/extra.xml");
        Assert.Equal(response2.Headers.ETag, response1.Headers.ETag);
    }

    // 14.24 If-Match
    // If none of the entity tags match, or if "*" is given and no current
    // entity exists, the server MUST NOT perform the requested method, and
    // MUST return a 412 (Precondition Failed) response. This behavior is
    // most useful when the client wants to prevent an updating method, such
    // as PUT, from modifying a resource that has changed since the client
    // last retrieved it.

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task IfMatchShouldReturn412WhenNotListed(HttpMethod method)
    {
        using var host = await StaticFilesTestServer.Create(app => app.UseFileServer());
        using var server = host.GetTestServer();
        var req = new HttpRequestMessage(method, "http://localhost/SubFolder/extra.xml");
        req.Headers.Add("If-Match", "\"fake\"");
        HttpResponseMessage resp = await server.CreateClient().SendAsync(req);
        Assert.Equal(HttpStatusCode.PreconditionFailed, resp.StatusCode);
    }

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task IfMatchShouldBeServedWhenListed(HttpMethod method)
    {
        using var host = await StaticFilesTestServer.Create(app => app.UseFileServer());
        using var server = host.GetTestServer();
        HttpResponseMessage original = await server.CreateClient().GetAsync("http://localhost/SubFolder/extra.xml");

        var req = new HttpRequestMessage(method, "http://localhost/SubFolder/extra.xml");
        req.Headers.Add("If-Match", original.Headers.ETag.ToString());
        HttpResponseMessage resp = await server.CreateClient().SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task IfMatchShouldBeServedForAsterisk(HttpMethod method)
    {
        using var host = await StaticFilesTestServer.Create(app => app.UseFileServer());
        using var server = host.GetTestServer();
        var req = new HttpRequestMessage(method, "http://localhost/SubFolder/extra.xml");
        req.Headers.Add("If-Match", "*");
        HttpResponseMessage resp = await server.CreateClient().SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Theory]
    [MemberData(nameof(UnsupportedMethods))]
    public async Task IfMatchShouldBeIgnoredForUnsupportedMethods(HttpMethod method)
    {
        using var host = await StaticFilesTestServer.Create(app => app.UseFileServer());
        using var server = host.GetTestServer();
        var req = new HttpRequestMessage(method, "http://localhost/SubFolder/extra.xml");
        req.Headers.Add("If-Match", "*");
        HttpResponseMessage resp = await server.CreateClient().SendAsync(req);
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
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
        using var host = await StaticFilesTestServer.Create(app => app.UseFileServer());
        using var server = host.GetTestServer();
        HttpResponseMessage resp1 = await server.CreateClient().GetAsync("http://localhost/SubFolder/extra.xml");

        var req2 = new HttpRequestMessage(method, "http://localhost/SubFolder/extra.xml");
        req2.Headers.Add("If-None-Match", resp1.Headers.ETag.ToString());
        HttpResponseMessage resp2 = await server.CreateClient().SendAsync(req2);
        Assert.Equal(HttpStatusCode.NotModified, resp2.StatusCode);
    }

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task IfNoneMatchAllShouldReturn304ForMatching(HttpMethod method)
    {
        using var host = await StaticFilesTestServer.Create(app => app.UseFileServer());
        using var server = host.GetTestServer();
        HttpResponseMessage resp1 = await server.CreateClient().GetAsync("http://localhost/SubFolder/extra.xml");

        var req2 = new HttpRequestMessage(method, "http://localhost/SubFolder/extra.xml");
        req2.Headers.Add("If-None-Match", "*");
        HttpResponseMessage resp2 = await server.CreateClient().SendAsync(req2);
        Assert.Equal(HttpStatusCode.NotModified, resp2.StatusCode);
    }

    [Theory]
    [MemberData(nameof(UnsupportedMethods))]
    public async Task IfNoneMatchShouldBeIgnoredForNonTwoHundredAnd304Responses(HttpMethod method)
    {
        using var host = await StaticFilesTestServer.Create(app => app.UseFileServer());
        using var server = host.GetTestServer();
        HttpResponseMessage resp1 = await server.CreateClient().GetAsync("http://localhost/SubFolder/extra.xml");

        var req2 = new HttpRequestMessage(method, "http://localhost/SubFolder/extra.xml");
        req2.Headers.Add("If-None-Match", resp1.Headers.ETag.ToString());
        HttpResponseMessage resp2 = await server.CreateClient().SendAsync(req2);
        Assert.Equal(HttpStatusCode.NotFound, resp2.StatusCode);
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
        using var host = await StaticFilesTestServer.Create(app => app.UseFileServer());
        using var server = host.GetTestServer();

        HttpResponseMessage response = await server.CreateClient().SendAsync(
            new HttpRequestMessage(method, "http://localhost/SubFolder/extra.xml"));

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
        using var host = await StaticFilesTestServer.Create(app => app.UseFileServer());
        using var server = host.GetTestServer();
        HttpResponseMessage resp1 = await server
            .CreateRequest("/SubFolder/extra.xml")
            .SendAsync(method.Method);

        HttpResponseMessage resp2 = await server
            .CreateRequest("/SubFolder/extra.xml")
            .AddHeader("If-None-Match", resp1.Headers.ETag.ToString())
            .And(req => req.Headers.IfModifiedSince = resp1.Content.Headers.LastModified)
            .SendAsync(method.Method);

        Assert.Equal(HttpStatusCode.NotModified, resp2.StatusCode);
    }

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task MatchingAtLeastOneETagReturnsNotModified(HttpMethod method)
    {
        using var host = await StaticFilesTestServer.Create(app => app.UseFileServer());
        using var server = host.GetTestServer();
        HttpResponseMessage resp1 = await server
            .CreateRequest("/SubFolder/extra.xml")
            .SendAsync(method.Method);
        var etag = resp1.Headers.ETag.ToString();

        HttpResponseMessage resp2 = await server
            .CreateRequest("/SubFolder/extra.xml")
            .AddHeader("If-Match", etag + ", " + etag)
            .SendAsync(method.Method);

        Assert.Equal(HttpStatusCode.OK, resp2.StatusCode);

        HttpResponseMessage resp3 = await server
            .CreateRequest("/SubFolder/extra.xml")
            .AddHeader("If-Match", etag + ", \"badetag\"")
            .SendAsync(method.Method);

        Assert.Equal(HttpStatusCode.OK, resp3.StatusCode);
    }

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task MissingEitherOrBothConditionsReturnsNormally(HttpMethod method)
    {
        using var host = await StaticFilesTestServer.Create(app => app.UseFileServer());
        using var server = host.GetTestServer();
        HttpResponseMessage resp1 = await server
            .CreateRequest("/SubFolder/extra.xml")
            .SendAsync(method.Method);

        DateTimeOffset lastModified = resp1.Content.Headers.LastModified.Value;
        DateTimeOffset pastDate = lastModified.AddHours(-1);
        DateTimeOffset futureDate = lastModified.AddHours(1);

        HttpResponseMessage resp2 = await server
            .CreateRequest("/SubFolder/extra.xml")
            .AddHeader("If-None-Match", "\"fake\"")
            .And(req => req.Headers.IfModifiedSince = lastModified)
            .SendAsync(method.Method);

        HttpResponseMessage resp3 = await server
            .CreateRequest("/SubFolder/extra.xml")
            .AddHeader("If-None-Match", resp1.Headers.ETag.ToString())
            .And(req => req.Headers.IfModifiedSince = pastDate)
            .SendAsync(method.Method);

        HttpResponseMessage resp4 = await server
            .CreateRequest("/SubFolder/extra.xml")
            .AddHeader("If-None-Match", "\"fake\"")
            .And(req => req.Headers.IfModifiedSince = futureDate)
            .SendAsync(method.Method);

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
        using var host = await StaticFilesTestServer.Create(app => app.UseFileServer());
        using var server = host.GetTestServer();

        HttpResponseMessage res = await server
            .CreateRequest("/SubFolder/extra.xml")
            .AddHeader("If-Modified-Since", "bad-date")
            .SendAsync(method.Method);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task FutureIfModifiedSinceDateFormatGivesNormalGet(HttpMethod method)
    {
        using var host = await StaticFilesTestServer.Create(app => app.UseFileServer());
        using var server = host.GetTestServer();

        HttpResponseMessage res = await server
            .CreateRequest("/SubFolder/extra.xml")
            .And(req => req.Headers.IfModifiedSince = DateTimeOffset.Now.AddYears(1))
            .SendAsync(method.Method);

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
        using var host = await StaticFilesTestServer.Create(app => app.UseFileServer());
        using var server = host.GetTestServer();

        HttpResponseMessage res1 = await server
            .CreateRequest("/SubFolder/extra.xml")
            .SendAsync(method.Method);

        HttpResponseMessage res2 = await server
            .CreateRequest("/SubFolder/extra.xml")
            .And(req => req.Headers.IfModifiedSince = DateTimeOffset.Now)
            .SendAsync(method.Method);

        Assert.Equal(HttpStatusCode.NotModified, res2.StatusCode);
    }

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task SupportsIfModifiedDateFormats(HttpMethod method)
    {
        using var host = await StaticFilesTestServer.Create(app => app.UseFileServer());
        using var server = host.GetTestServer();
        HttpResponseMessage res1 = await server
            .CreateRequest("/SubFolder/extra.xml")
            .SendAsync(method.Method);

        var formats = new[]
        {
                "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
                "dddd, dd-MMM-yy HH:mm:ss 'GMT'",
                "ddd MMM  d HH:mm:ss yyyy"
            };

        foreach (var format in formats)
        {
            HttpResponseMessage res2 = await server
                .CreateRequest("/SubFolder/extra.xml")
                .AddHeader("If-Modified-Since", DateTimeOffset.UtcNow.ToString(format, CultureInfo.InvariantCulture))
                .SendAsync(method.Method);

            Assert.Equal(HttpStatusCode.NotModified, res2.StatusCode);
        }
    }

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task IfModifiedSinceDateLessThanLastModifiedShouldReturn200(HttpMethod method)
    {
        using var host = await StaticFilesTestServer.Create(app => app.UseFileServer());
        using var server = host.GetTestServer();

        HttpResponseMessage res1 = await server
            .CreateRequest("/SubFolder/extra.xml")
            .SendAsync(method.Method);

        HttpResponseMessage res2 = await server
            .CreateRequest("/SubFolder/extra.xml")
            .And(req => req.Headers.IfModifiedSince = DateTimeOffset.MinValue)
            .SendAsync(method.Method);

        Assert.Equal(HttpStatusCode.OK, res2.StatusCode);
    }

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task InvalidIfUnmodifiedSinceDateFormatGivesNormalGet(HttpMethod method)
    {
        using var host = await StaticFilesTestServer.Create(app => app.UseFileServer());
        using var server = host.GetTestServer();

        HttpResponseMessage res = await server
            .CreateRequest("/SubFolder/extra.xml")
            .AddHeader("If-Unmodified-Since", "bad-date")
            .SendAsync(method.Method);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task FutureIfUnmodifiedSinceDateFormatGivesNormalGet(HttpMethod method)
    {
        using var host = await StaticFilesTestServer.Create(app => app.UseFileServer());
        using var server = host.GetTestServer();

        HttpResponseMessage res = await server
            .CreateRequest("/SubFolder/extra.xml")
            .And(req => req.Headers.IfUnmodifiedSince = DateTimeOffset.Now.AddYears(1))
            .SendAsync(method.Method);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Theory]
    [MemberData(nameof(SupportedMethods))]
    public async Task IfUnmodifiedSinceDateLessThanLastModifiedShouldReturn412(HttpMethod method)
    {
        using var host = await StaticFilesTestServer.Create(app => app.UseFileServer());
        using var server = host.GetTestServer();

        HttpResponseMessage res1 = await server
            .CreateRequest("/SubFolder/extra.xml")
            .SendAsync(method.Method);

        HttpResponseMessage res2 = await server
            .CreateRequest("/SubFolder/extra.xml")
            .And(req => req.Headers.IfUnmodifiedSince = DateTimeOffset.MinValue)
            .SendAsync(method.Method);

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
}
