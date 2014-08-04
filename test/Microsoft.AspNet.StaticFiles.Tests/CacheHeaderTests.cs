// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Shouldly;
using Xunit;

namespace Microsoft.AspNet.StaticFiles
{
    public class CacheHeaderTests
    {
        [Fact]
        public async Task ServerShouldReturnETag()
        {
            TestServer server = TestServer.Create(app => app.UseFileServer());

            HttpResponseMessage response = await server.CreateClient().GetAsync("http://localhost/SubFolder/Extra.xml");
            response.Headers.ETag.ShouldNotBe(null);
            response.Headers.ETag.Tag.ShouldNotBe(null);
        }

        [Fact]
        public async Task SameETagShouldBeReturnedAgain()
        {
            TestServer server = TestServer.Create(app => app.UseFileServer());

            HttpResponseMessage response1 = await server.CreateClient().GetAsync("http://localhost/SubFolder/Extra.xml");
            HttpResponseMessage response2 = await server.CreateClient().GetAsync("http://localhost/SubFolder/Extra.xml");
            response1.Headers.ETag.ShouldBe(response2.Headers.ETag);
        }

        // 14.24 If-Match
        // If none of the entity tags match, or if "*" is given and no current
        // entity exists, the server MUST NOT perform the requested method, and
        // MUST return a 412 (Precondition Failed) response. This behavior is
        // most useful when the client wants to prevent an updating method, such
        // as PUT, from modifying a resource that has changed since the client
        // last retrieved it.

        [Fact]
        public async Task IfMatchShouldReturn412WhenNotListed()
        {
            TestServer server = TestServer.Create(app => app.UseFileServer());
            var req = new HttpRequestMessage(HttpMethod.Get, "http://localhost/SubFolder/Extra.xml");
            req.Headers.Add("If-Match", "\"fake\"");
            HttpResponseMessage resp = await server.CreateClient().SendAsync(req);
            resp.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
        }

        [Fact]
        public async Task IfMatchShouldBeServedWhenListed()
        {
            TestServer server = TestServer.Create(app => app.UseFileServer());
            HttpResponseMessage original = await server.CreateClient().GetAsync("http://localhost/SubFolder/Extra.xml");

            var req = new HttpRequestMessage(HttpMethod.Get, "http://localhost/SubFolder/Extra.xml");
            req.Headers.Add("If-Match", original.Headers.ETag.ToString());
            HttpResponseMessage resp = await server.CreateClient().SendAsync(req);
            resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task IfMatchShouldBeServedForAstrisk()
        {
            TestServer server = TestServer.Create(app => app.UseFileServer());
            var req = new HttpRequestMessage(HttpMethod.Get, "http://localhost/SubFolder/Extra.xml");
            req.Headers.Add("If-Match", "*");
            HttpResponseMessage resp = await server.CreateClient().SendAsync(req);
            resp.StatusCode.ShouldBe(HttpStatusCode.OK);
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

        [Fact]
        public async Task IfNoneMatchShouldReturn304ForMatchingOnGetAndHeadMethod()
        {
            TestServer server = TestServer.Create(app => app.UseFileServer());
            HttpResponseMessage resp1 = await server.CreateClient().GetAsync("http://localhost/SubFolder/Extra.xml");

            var req2 = new HttpRequestMessage(HttpMethod.Get, "http://localhost/SubFolder/Extra.xml");
            req2.Headers.Add("If-None-Match", resp1.Headers.ETag.ToString());
            HttpResponseMessage resp2 = await server.CreateClient().SendAsync(req2);
            resp2.StatusCode.ShouldBe(HttpStatusCode.NotModified);

            var req3 = new HttpRequestMessage(HttpMethod.Head, "http://localhost/SubFolder/Extra.xml");
            req3.Headers.Add("If-None-Match", resp1.Headers.ETag.ToString());
            HttpResponseMessage resp3 = await server.CreateClient().SendAsync(req3);
            resp3.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        }

        [Fact]
        public async Task IfNoneMatchShouldBeIgnoredForNonTwoHundredAnd304Responses()
        {
            TestServer server = TestServer.Create(app => app.UseFileServer());
            HttpResponseMessage resp1 = await server.CreateClient().GetAsync("http://localhost/SubFolder/Extra.xml");

            var req2 = new HttpRequestMessage(HttpMethod.Post, "http://localhost/SubFolder/Extra.xml");
            req2.Headers.Add("If-None-Match", resp1.Headers.ETag.ToString());
            HttpResponseMessage resp2 = await server.CreateClient().SendAsync(req2);
            resp2.StatusCode.ShouldBe(HttpStatusCode.NotFound);

            var req3 = new HttpRequestMessage(HttpMethod.Put, "http://localhost/SubFolder/Extra.xml");
            req3.Headers.Add("If-None-Match", resp1.Headers.ETag.ToString());
            HttpResponseMessage resp3 = await server.CreateClient().SendAsync(req3);
            resp3.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        // 14.26 If-None-Match
        // If none of the entity tags match, then the server MAY perform the
        // requested method as if the If-None-Match header field did not exist,
        // but MUST also ignore any If-Modified-Since header field(s) in the
        // request. That is, if no entity tags match, then the server MUST NOT
        // return a 304 (Not Modified) response.

        // A server MUST use the strong comparison function (see section 13.3.3)
        // to compare the entity tags in If-Match.

        [Fact]
        public async Task ServerShouldReturnLastModified()
        {
            TestServer server = TestServer.Create(app => app.UseFileServer());

            HttpResponseMessage response = await server.CreateClient().GetAsync("http://localhost/SubFolder/Extra.xml");
            response.Content.Headers.LastModified.ShouldNotBe(null);
        }

        // 13.3.4
        // An HTTP/1.1 origin server, upon receiving a conditional request that
        // includes both a Last-Modified date (e.g., in an If-Modified-Since or
        // If-Unmodified-Since header field) and one or more entity tags (e.g.,
        // in an If-Match, If-None-Match, or If-Range header field) as cache
        // validators, MUST NOT return a response status of 304 (Not Modified)
        // unless doing so is consistent with all of the conditional header
        // fields in the request.

        [Fact]
        public async Task MatchingBothConditionsReturnsNotModified()
        {
            TestServer server = TestServer.Create(app => app.UseFileServer());
            HttpResponseMessage resp1 = await server
                .CreateRequest("/SubFolder/Extra.xml")
                .GetAsync();

            HttpResponseMessage resp2 = await server
                .CreateRequest("/SubFolder/Extra.xml")
                .AddHeader("If-None-Match", resp1.Headers.ETag.ToString())
                .And(req => req.Headers.IfModifiedSince = resp1.Content.Headers.LastModified)
                .GetAsync();

            resp2.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        }

        [Fact]
        public async Task MissingEitherOrBothConditionsReturnsNormally()
        {
            TestServer server = TestServer.Create(app => app.UseFileServer());
            HttpResponseMessage resp1 = await server
                .CreateRequest("/SubFolder/Extra.xml")
                .GetAsync();

            DateTimeOffset lastModified = resp1.Content.Headers.LastModified.Value;
            DateTimeOffset pastDate = lastModified.AddHours(-1);
            DateTimeOffset furtureDate = lastModified.AddHours(1);

            HttpResponseMessage resp2 = await server
                .CreateRequest("/SubFolder/Extra.xml")
                .AddHeader("If-None-Match", "\"fake\"")
                .And(req => req.Headers.IfModifiedSince = lastModified)
                .GetAsync();

            HttpResponseMessage resp3 = await server
                .CreateRequest("/SubFolder/Extra.xml")
                .AddHeader("If-None-Match", resp1.Headers.ETag.ToString())
                .And(req => req.Headers.IfModifiedSince = pastDate)
                .GetAsync();

            HttpResponseMessage resp4 = await server
                .CreateRequest("/SubFolder/Extra.xml")
                .AddHeader("If-None-Match", "\"fake\"")
                .And(req => req.Headers.IfModifiedSince = furtureDate)
                .GetAsync();

            resp2.StatusCode.ShouldBe(HttpStatusCode.OK);
            resp3.StatusCode.ShouldBe(HttpStatusCode.OK);
            resp4.StatusCode.ShouldBe(HttpStatusCode.OK);
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
        [Fact]
        public async Task InvalidIfModifiedSinceDateFormatGivesNormalGet()
        {
            TestServer server = TestServer.Create(app => app.UseFileServer());

            HttpResponseMessage res = await server
                .CreateRequest("/SubFolder/Extra.xml")
                .AddHeader("If-Modified-Since", "bad-date")
                .GetAsync();

            res.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        // b) If the variant has been modified since the If-Modified-Since
        //   date, the response is exactly the same as for a normal GET.

        // c) If the variant has not been modified since a valid If-
        //   Modified-Since date, the server SHOULD return a 304 (Not
        //   Modified) response.

        [Fact]
        public async Task IfModifiedSinceDateEqualsLastModifiedShouldReturn304()
        {
            TestServer server = TestServer.Create(app => app.UseFileServer());

            HttpResponseMessage res1 = await server
                .CreateRequest("/SubFolder/Extra.xml")
                .GetAsync();

            HttpResponseMessage res2 = await server
                .CreateRequest("/SubFolder/Extra.xml")
                .And(req => req.Headers.IfModifiedSince = res1.Content.Headers.LastModified)
                .GetAsync();

            res2.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        }
    }
}
