// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys;

public class ResponseTrailersTests : LoggedTest
{
    [ConditionalFact]
    public async Task ResponseTrailers_HTTP11_TrailersNotAvailable()
    {
        using (Utilities.CreateDynamicHttpsServer(out var address, httpContext =>
        {
            Assert.Equal("HTTP/1.1", httpContext.Request.Protocol);
            Assert.False(httpContext.Response.SupportsTrailers());
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address, http2: false);
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version11, response.Version);
            Assert.Empty(response.TrailingHeaders);
        }
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, "10.0.19529", SkipReason = "Requires HTTP/2 Trailers support.")]
    public async Task ResponseTrailers_HTTP2_TrailersAvailable()
    {
        using (Utilities.CreateDynamicHttpsServer(out var address, httpContext =>
        {
            Assert.Equal("HTTP/2", httpContext.Request.Protocol);
            Assert.True(httpContext.Response.SupportsTrailers());
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version20, response.Version);
            Assert.Empty(response.TrailingHeaders);
        }
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, "10.0.19529", SkipReason = "Requires HTTP/2 Trailers support.")]
    public async Task ResponseTrailers_ProhibitedTrailers_Blocked()
    {
        using (Utilities.CreateDynamicHttpsServer(out var address, httpContext =>
        {
            Assert.True(httpContext.Response.SupportsTrailers());
            foreach (var header in HeaderCollection.DisallowedTrailers)
            {
                Assert.Throws<InvalidOperationException>(() => httpContext.Response.AppendTrailer(header, "value"));
            }
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version20, response.Version);
            Assert.Empty(response.TrailingHeaders);
        }
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, "10.0.19529", SkipReason = "Requires HTTP/2 Trailers support.")]
    public async Task ResponseTrailers_NoBody_TrailersSent()
    {
        using (Utilities.CreateDynamicHttpsServer(out var address, httpContext =>
        {
            httpContext.Response.DeclareTrailer("trailername");
            httpContext.Response.AppendTrailer("trailername", "TrailerValue");
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version20, response.Version);
            Assert.NotEmpty(response.TrailingHeaders);
            Assert.Equal("TrailerValue", response.TrailingHeaders.GetValues("TrailerName").Single());
        }
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, "10.0.19529", SkipReason = "Requires HTTP/2 Trailers support.")]
    public async Task ResponseTrailers_WithBody_TrailersSent()
    {
        using (Utilities.CreateDynamicHttpsServer(out var address, async httpContext =>
        {
            await httpContext.Response.WriteAsync("Hello World");
            httpContext.Response.AppendTrailer("TrailerName", "Trailer Value");
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version20, response.Version);
            Assert.Equal("Hello World", await response.Content.ReadAsStringAsync());
            Assert.NotEmpty(response.TrailingHeaders);
            Assert.Equal("Trailer Value", response.TrailingHeaders.GetValues("TrailerName").Single());
        }
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, "10.0.19529", SkipReason = "Requires HTTP/2 Trailers support.")]
    public async Task ResponseTrailers_WithContentLengthBody_TrailersNotSent()
    {
        var body = "Hello World";
        var responseFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using (Utilities.CreateDynamicHttpsServer(out var address, async httpContext =>
        {
            httpContext.Response.ContentLength = body.Length;
            await httpContext.Response.WriteAsync(body);
            try
            {
                Assert.Throws<InvalidOperationException>(() => httpContext.Response.AppendTrailer("TrailerName", "Trailer Value"));
                responseFinished.SetResult();
            }
            catch (Exception ex)
            {
                responseFinished.SetException(ex);
            }
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version20, response.Version);
            Assert.Equal(body.Length.ToString(CultureInfo.InvariantCulture), response.Content.Headers.GetValues(HeaderNames.ContentLength).Single());
            Assert.Equal(body, await response.Content.ReadAsStringAsync());
            Assert.Empty(response.TrailingHeaders);
            await responseFinished.Task;
        }
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, "10.0.19529", SkipReason = "Requires HTTP/2 Trailers support.")]
    public async Task ResponseTrailers_WithTrailersBeforeContentLengthBody_TrailersSent()
    {
        var body = "Hello World";
        using (Utilities.CreateDynamicHttpsServer(out var address, async httpContext =>
        {
            httpContext.Response.ContentLength = body.Length * 2;
            await httpContext.Response.WriteAsync(body);
            httpContext.Response.AppendTrailer("TrailerName", "Trailer Value");
            await httpContext.Response.WriteAsync(body);
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version20, response.Version);
            // Avoid HttpContent's automatic content-length calculation.
            Assert.True(response.Content.Headers.TryGetValues(HeaderNames.ContentLength, out var contentLength), HeaderNames.ContentLength);
            Assert.Equal((2 * body.Length).ToString(CultureInfo.InvariantCulture), contentLength.First());
            Assert.Equal(body + body, await response.Content.ReadAsStringAsync());
            Assert.NotEmpty(response.TrailingHeaders);
            Assert.Equal("Trailer Value", response.TrailingHeaders.GetValues("TrailerName").Single());
        }
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, "10.0.19529", SkipReason = "Requires HTTP/2 Trailers support.")]
    public async Task ResponseTrailers_WithContentLengthBodyAndDeclared_TrailersSent()
    {
        var body = "Hello World";
        using (Utilities.CreateDynamicHttpsServer(out var address, async httpContext =>
        {
            httpContext.Response.ContentLength = body.Length;
            httpContext.Response.DeclareTrailer("TrailerName");
            await httpContext.Response.WriteAsync(body);
            httpContext.Response.AppendTrailer("TrailerName", "Trailer Value");
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version20, response.Version);
            // Avoid HttpContent's automatic content-length calculation.
            Assert.True(response.Content.Headers.TryGetValues(HeaderNames.ContentLength, out var contentLength), HeaderNames.ContentLength);
            Assert.Equal(body.Length.ToString(CultureInfo.InvariantCulture), contentLength.First());
            Assert.Equal("TrailerName", response.Headers.Trailer.Single());
            Assert.Equal(body, await response.Content.ReadAsStringAsync());
            Assert.NotEmpty(response.TrailingHeaders);
            Assert.Equal("Trailer Value", response.TrailingHeaders.GetValues("TrailerName").Single());
        }
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, "10.0.19529", SkipReason = "Requires HTTP/2 Trailers support.")]
    public async Task ResponseTrailers_WithContentLengthBodyAndDeclaredButMissingTrailers_Completes()
    {
        var body = "Hello World";
        using (Utilities.CreateDynamicHttpsServer(out var address, async httpContext =>
        {
            httpContext.Response.ContentLength = body.Length;
            httpContext.Response.DeclareTrailer("TrailerName");
            await httpContext.Response.WriteAsync(body);
            // If we declare trailers but don't send any make sure it completes anyways.
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version20, response.Version);
            // Avoid HttpContent's automatic content-length calculation.
            Assert.True(response.Content.Headers.TryGetValues(HeaderNames.ContentLength, out var contentLength), HeaderNames.ContentLength);
            Assert.Equal(body.Length.ToString(CultureInfo.InvariantCulture), contentLength.First());
            Assert.Equal("TrailerName", response.Headers.Trailer.Single());
            Assert.Equal(body, await response.Content.ReadAsStringAsync());
            Assert.Empty(response.TrailingHeaders);
        }
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, "10.0.19529", SkipReason = "Requires HTTP/2 Trailers support.")]
    public async Task ResponseTrailers_CompleteAsyncNoBody_TrailersSent()
    {
        var trailersReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using (Utilities.CreateDynamicHttpsServer(out var address, async httpContext =>
        {
            httpContext.Response.AppendTrailer("trailername", "TrailerValue");
            await httpContext.Response.CompleteAsync();
            await trailersReceived.Task.DefaultTimeout();
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version20, response.Version);
            Assert.NotEmpty(response.TrailingHeaders);
            Assert.Equal("TrailerValue", response.TrailingHeaders.GetValues("TrailerName").Single());
            trailersReceived.SetResult();
        }
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, "10.0.19529", SkipReason = "Requires HTTP/2 Trailers support.")]
    public async Task ResponseTrailers_CompleteAsyncWithBody_TrailersSent()
    {
        var trailersReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using (Utilities.CreateDynamicHttpsServer(out var address, async httpContext =>
        {
            await httpContext.Response.WriteAsync("Hello World");
            httpContext.Response.AppendTrailer("TrailerName", "Trailer Value");
            await httpContext.Response.CompleteAsync();
            await trailersReceived.Task.DefaultTimeout();
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version20, response.Version);
            Assert.Equal("Hello World", await response.Content.ReadAsStringAsync());
            Assert.NotEmpty(response.TrailingHeaders);
            Assert.Equal("Trailer Value", response.TrailingHeaders.GetValues("TrailerName").Single());
            trailersReceived.SetResult();
        }
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, "10.0.19529", SkipReason = "Requires HTTP/2 Trailers support.")]
    public async Task ResponseTrailers_MultipleValues_SentAsSeparateHeaders()
    {
        using (Utilities.CreateDynamicHttpsServer(out var address, httpContext =>
        {
            httpContext.Response.AppendTrailer("trailername", new StringValues(new[] { "TrailerValue0", "TrailerValue1" }));
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version20, response.Version);
            Assert.NotEmpty(response.TrailingHeaders);
            // We can't actually assert they are sent as separate headers using HttpClient, we'd have to write a lower level test
            // that read the header frames directly.
            Assert.Equal(new[] { "TrailerValue0", "TrailerValue1" }, response.TrailingHeaders.GetValues("TrailerName"));
        }
    }

    [ConditionalFact]
    [MinimumOSVersion(OperatingSystems.Windows, "10.0.19529", SkipReason = "Requires HTTP/2 Trailers support.")]
    public async Task ResponseTrailers_LargeTrailers_Success()
    {
        var values = new[] {
                        new string('a', 1024),
                        new string('b', 1024 * 4),
                        new string('c', 1024 * 8),
                        new string('d', 1024 * 16),
                        new string('e', 1024 * 32),
                        new string('f', 1024 * 64 - 1) }; // Max header size

        using (Utilities.CreateDynamicHttpsServer(out var address, httpContext =>
        {
            httpContext.Response.AppendTrailer("ThisIsALongerHeaderNameThatStillWorksForReals", new StringValues(values));
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            var response = await SendRequestAsync(address);
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpVersion.Version20, response.Version);
            Assert.NotEmpty(response.TrailingHeaders);
            // We can't actually assert they are sent in multiple frames using HttpClient, we'd have to write a lower level test
            // that read the header frames directly. We at least verify that really large values work.
            Assert.Equal(values, response.TrailingHeaders.GetValues("ThisIsALongerHeaderNameThatStillWorksForReals"));
        }
    }

    [ConditionalTheory, MemberData(nameof(NullHeaderData))]
    [MinimumOSVersion(OperatingSystems.Windows, "10.0.19529", SkipReason = "Requires HTTP/2 Trailers support.")]
    public async Task ResponseTrailers_NullValues_Ignored(string headerName, StringValues headerValue, StringValues expectedValue)
    {
        using (Utilities.CreateDynamicHttpsServer(out var address, httpContext =>
        {
            httpContext.Response.AppendTrailer(headerName, headerValue);
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            HttpResponseMessage response = await SendRequestAsync(address);
            response.EnsureSuccessStatusCode();
            var headers = response.TrailingHeaders;

            if (StringValues.IsNullOrEmpty(expectedValue))
            {
                Assert.False(headers.Contains(headerName));
            }
            else
            {
                Assert.True(headers.Contains(headerName));
                Assert.Equal(headers.GetValues(headerName), expectedValue);
            }
        }
    }

    public static TheoryData<string, StringValues, StringValues> NullHeaderData
    {
        get
        {
            var dataset = new TheoryData<string, StringValues, StringValues>();

            dataset.Add("NullString", (string)null, (string)null);
            dataset.Add("EmptyString", "", "");
            dataset.Add("NullStringArray", new string[] { null }, "");
            dataset.Add("EmptyStringArray", new string[] { "" }, "");
            dataset.Add("MixedStringArray", new string[] { null, "" }, new string[] { "", "" });
            dataset.Add("WithValidStrings", new string[] { null, "Value", "" }, new string[] { "", "Value", "" });

            return dataset;
        }
    }

    private async Task<HttpResponseMessage> SendRequestAsync(string uri, bool http2 = true)
    {
        var handler = new HttpClientHandler();
        handler.MaxResponseHeadersLength = 128;
        handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        using HttpClient client = new HttpClient(handler);
        client.DefaultRequestVersion = http2 ? HttpVersion.Version20 : HttpVersion.Version11;
        return await client.GetAsync(uri);
    }
}
