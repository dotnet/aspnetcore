// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys;

public class ResponseHeaderTests : LoggedTest
{
    [ConditionalFact]
    public async Task ResponseHeaders_ServerSendsDefaultHeaders_Success()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            HttpResponseMessage response = await SendRequestAsync(address);
            response.EnsureSuccessStatusCode();
            Assert.Equal(2, response.Headers.Count());
            Assert.False(response.Headers.TransferEncodingChunked.HasValue);
            Assert.True(response.Headers.Date.HasValue);
            Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers.Server.ToString());
            Assert.Single(response.Content.Headers);
            Assert.Equal(0, response.Content.Headers.ContentLength);
        }
    }

    [ConditionalFact]
    public async Task ResponseHeaders_ServerSendsSingleValueKnownHeaders_Success()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            var responseInfo = httpContext.Features.Get<IHttpResponseFeature>();
            var responseHeaders = responseInfo.Headers;
            responseHeaders["WWW-Authenticate"] = new string[] { "custom1" };
            return Task.FromResult(0);
        }, LoggerFactory))
        {

#pragma warning disable SYSLIB0014 // HttpClient would merge the headers no matter what
            WebRequest request = WebRequest.Create(address);
#pragma warning restore SYSLIB0014
            HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
            Assert.Equal(4, response.Headers.Count);
            Assert.Null(response.Headers["Transfer-Encoding"]);
            Assert.Equal(0, response.ContentLength);
            Assert.NotNull(response.Headers["Date"]);
            Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers["Server"]);
            Assert.Equal("custom1", response.Headers["WWW-Authenticate"]);
        }
    }

    [ConditionalFact]
    public async Task ResponseHeaders_ServerSendsMultiValueKnownHeaders_Success()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            var responseInfo = httpContext.Features.Get<IHttpResponseFeature>();
            var responseHeaders = responseInfo.Headers;
            responseHeaders["WWW-Authenticate"] = new string[] { "custom1, and custom2", "custom3" };
            return Task.FromResult(0);
        }, LoggerFactory))
        {
#pragma warning disable SYSLIB0014 // HttpClient would merge the headers no matter what
            WebRequest request = WebRequest.Create(address);
#pragma warning restore SYSLIB0014
            HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
            Assert.Equal(4, response.Headers.Count);
            Assert.Null(response.Headers["Transfer-Encoding"]);
            Assert.Equal(0, response.ContentLength);
            Assert.NotNull(response.Headers["Date"]);
            Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers["Server"]);
            Assert.Equal("custom1, and custom2, custom3", response.Headers["WWW-Authenticate"]);
        }
    }

    [ConditionalFact]
    public async Task ResponseHeaders_ServerSendsCustomHeaders_Success()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            var responseInfo = httpContext.Features.Get<IHttpResponseFeature>();
            var responseHeaders = responseInfo.Headers;
            responseHeaders["Custom-Header1"] = new string[] { "custom1, and custom2", "custom3" };
            return Task.FromResult(0);
        }, LoggerFactory))
        {
#pragma warning disable SYSLIB0014 // HttpClient would merge the headers no matter what
            WebRequest request = WebRequest.Create(address);
#pragma warning restore SYSLIB0014
            HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
            Assert.Equal(4, response.Headers.Count);
            Assert.Null(response.Headers["Transfer-Encoding"]);
            Assert.Equal(0, response.ContentLength);
            Assert.NotNull(response.Headers["Date"]);
            Assert.Equal("Microsoft-HTTPAPI/2.0", response.Headers["Server"]);
            Assert.Equal("custom1, and custom2, custom3", response.Headers["Custom-Header1"]);
        }
    }

    [ConditionalFact]
    public async Task ResponseHeaders_ServerSendsNonAsciiHeaders_Success()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            var responseInfo = httpContext.Features.Get<IHttpResponseFeature>();
            var responseHeaders = responseInfo.Headers;
            responseHeaders["Custom-Header1"] = new string[] { "Dašta" };
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            var socketsHttpHandler = new SocketsHttpHandler() { ResponseHeaderEncodingSelector = (_, _) => Encoding.UTF8 };
            var httpClient = new HttpClient(socketsHttpHandler);
            var response = await httpClient.GetAsync(address);
            response.EnsureSuccessStatusCode();
            Assert.True(response.Headers.TryGetValues("Custom-Header1", out var header));
            Assert.Equal("Dašta", header.Single());
        }
    }

    [ConditionalFact]
    public async Task ResponseHeaders_ServerSendsConnectionClose_Closed()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            var responseInfo = httpContext.Features.Get<IHttpResponseFeature>();
            var responseHeaders = responseInfo.Headers;
            responseHeaders["Connection"] = new string[] { "Close" };
            return httpContext.Response.Body.FlushAsync(); // Http.Sys adds the Content-Length: header for us if we don't flush
        }, LoggerFactory))
        {
            HttpResponseMessage response = await SendRequestAsync(address);
            response.EnsureSuccessStatusCode();
            Assert.True(response.Headers.ConnectionClose.Value);
            Assert.Equal(new string[] { "close" }, response.Headers.GetValues("Connection"));
            Assert.True(response.Headers.TransferEncodingChunked.HasValue);
            Assert.True(response.Headers.TransferEncodingChunked);
            IEnumerable<string> values;
            var result = response.Content.Headers.TryGetValues("Content-Length", out values);
            Assert.False(result);
        }
    }

    [ConditionalFact]
    public async Task ResponseHeaders_HTTP10Request_Gets11Close()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            using (HttpClient client = new HttpClient())
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, address);
                request.Version = new Version(1, 0);
                HttpResponseMessage response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                Assert.Equal(new Version(1, 1), response.Version);
                Assert.True(response.Headers.ConnectionClose.Value);
                Assert.Equal(new string[] { "close" }, response.Headers.GetValues("Connection"));
                Assert.False(response.Headers.TransferEncodingChunked.HasValue);
            }
        }
    }

    [ConditionalFact]
    public async Task ResponseHeaders_HTTP10RequestWithChunkedHeader_ManualChunking()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            var response = httpContext.Response;
            var responseHeaders = response.Headers;
            responseHeaders["Transfer-Encoding"] = new string[] { "chunked" };
            var responseBytes = Encoding.ASCII.GetBytes("10\r\nManually Chunked\r\n0\r\n\r\n");
            return response.Body.WriteAsync(responseBytes, 0, responseBytes.Length);
        }, LoggerFactory))
        {
            using (HttpClient client = new HttpClient())
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, address);
                request.Version = new Version(1, 0);
                HttpResponseMessage response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                Assert.Equal(new Version(1, 1), response.Version);
                Assert.True(response.Headers.TransferEncodingChunked.HasValue);
                Assert.False(response.Content.Headers.Contains("Content-Length"));
                Assert.True(response.Headers.ConnectionClose.Value);
                Assert.Equal(new string[] { "close" }, response.Headers.GetValues("Connection"));
                Assert.Equal("Manually Chunked", await response.Content.ReadAsStringAsync());
            }
        }
    }

    [ConditionalFact]
    public async Task Headers_FlushSendsHeaders_Success()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Features.Get<IHttpBodyControlFeature>().AllowSynchronousIO = true;
                var response = httpContext.Response;
                var responseHeaders = response.Headers;
                responseHeaders.Add("Custom1", new string[] { "value1a", "value1b" });
                responseHeaders.Add("Custom2", new string[] { "value2a, value2b" });
                var body = response.Body;
                Assert.False(response.HasStarted);
                body.Flush();
                Assert.True(response.HasStarted);
                Assert.Throws<InvalidOperationException>(() => response.StatusCode = 404);
                Assert.Throws<InvalidOperationException>(() => responseHeaders.Add("Custom3", new string[] { "value3a, value3b", "value3c" }));
                return Task.FromResult(0);
            }, LoggerFactory))
        {
            HttpResponseMessage response = await SendRequestAsync(address);
            response.EnsureSuccessStatusCode();
            Assert.Equal(5, response.Headers.Count()); // Date, Server, Chunked

            Assert.Equal(2, response.Headers.GetValues("Custom1").Count());
            Assert.Equal("value1a", response.Headers.GetValues("Custom1").First());
            Assert.Equal("value1b", response.Headers.GetValues("Custom1").Skip(1).First());
            Assert.Single(response.Headers.GetValues("Custom2"));
            Assert.Equal("value2a, value2b", response.Headers.GetValues("Custom2").First());
        }
    }

    [ConditionalFact]
    public async Task Headers_FlushAsyncSendsHeaders_Success()
    {
        string address;
        using (Utilities.CreateHttpServer(out address, async httpContext =>
            {
                var response = httpContext.Response;
                var responseHeaders = response.Headers;
                responseHeaders.Add("Custom1", new string[] { "value1a", "value1b" });
                responseHeaders.Add("Custom2", new string[] { "value2a, value2b" });
                var body = response.Body;
                Assert.False(response.HasStarted);
                await body.FlushAsync();
                Assert.True(response.HasStarted);
                Assert.Throws<InvalidOperationException>(() => response.StatusCode = 404);
                Assert.Throws<InvalidOperationException>(() => responseHeaders.Add("Custom3", new string[] { "value3a, value3b", "value3c" }));
            }, LoggerFactory))
        {
            HttpResponseMessage response = await SendRequestAsync(address);
            response.EnsureSuccessStatusCode();
            Assert.Equal(5, response.Headers.Count()); // Date, Server, Chunked

            Assert.Equal(2, response.Headers.GetValues("Custom1").Count());
            Assert.Equal("value1a", response.Headers.GetValues("Custom1").First());
            Assert.Equal("value1b", response.Headers.GetValues("Custom1").Skip(1).First());
            Assert.Single(response.Headers.GetValues("Custom2"));
            Assert.Equal("value2a, value2b", response.Headers.GetValues("Custom2").First());
        }
    }

    [ConditionalTheory, MemberData(nameof(NullHeaderData))]
    public async Task Headers_IgnoreNullHeaders(string headerName, StringValues headerValue, StringValues expectedValue)
    {
        string address;
        using (Utilities.CreateHttpServer(out address, httpContext =>
        {
            var responseHeaders = httpContext.Response.Headers;
            responseHeaders.Add(headerName, headerValue);
            return Task.FromResult(0);
        }, LoggerFactory))
        {
            HttpResponseMessage response = await SendRequestAsync(address);
            response.EnsureSuccessStatusCode();
            var headers = response.Headers;

            if (expectedValue.Count == 0)
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

            // Unknown headers
            dataset.Add("NullString", (string)null, (string)null);
            dataset.Add("EmptyString", "", "");
            dataset.Add("NullStringArray", new string[] { null }, "");
            dataset.Add("EmptyStringArray", new string[] { "" }, "");
            dataset.Add("MixedStringArray", new string[] { null, "" }, new string[] { "", "" });
            // Known headers
            dataset.Add("Location", (string)null, (string)null);
            dataset.Add("Location", "", (string)null);
            dataset.Add("Location", new string[] { null }, (string)null);
            dataset.Add("Location", new string[] { "" }, (string)null);
            dataset.Add("Location", new string[] { "a" }, "a");
            dataset.Add("Location", new string[] { null, "" }, (string)null);
            dataset.Add("Location", new string[] { null, "", "a", "b" }, new string[] { "a", "b" });

            return dataset;
        }
    }

    private async Task<HttpResponseMessage> SendRequestAsync(string uri)
    {
        using (HttpClient client = new HttpClient())
        {
            return await client.GetAsync(uri);
        }
    }
}
