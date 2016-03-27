// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Server.WebListener.FunctionalTests
{
    public class ResponseCachingTests
    {
        [Fact]
        public async Task Caching_NoCacheControl_NotCached()
        {
            var requestCount = 1;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.ContentType = "some/thing"; // Http.Sys requires content-type for caching
                httpContext.Response.Headers["x-request-count"] = (requestCount++).ToString();
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                Assert.Equal("1", await SendRequestAsync(address));
                Assert.Equal("2", await SendRequestAsync(address));
            }
        }

        [Fact]
        public async Task Caching_JustPublic_NotCached()
        {
            var requestCount = 1;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.ContentType = "some/thing"; // Http.Sys requires content-type for caching
                httpContext.Response.Headers["x-request-count"] = (requestCount++).ToString();
                httpContext.Response.Headers["Cache-Control"] = "public";
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                Assert.Equal("1", await SendRequestAsync(address));
                Assert.Equal("2", await SendRequestAsync(address));
            }
        }

        [Fact]
        public async Task Caching_MaxAge_Cached()
        {
            var requestCount = 1;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.ContentType = "some/thing"; // Http.Sys requires content-type for caching
                httpContext.Response.Headers["x-request-count"] = (requestCount++).ToString();
                httpContext.Response.Headers["Cache-Control"] = "public, max-age=10";
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                Assert.Equal("1", await SendRequestAsync(address));
                Assert.Equal("1", await SendRequestAsync(address));
            }
        }

        [Fact]
        public async Task Caching_SMaxAge_Cached()
        {
            var requestCount = 1;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.ContentType = "some/thing"; // Http.Sys requires content-type for caching
                httpContext.Response.Headers["x-request-count"] = (requestCount++).ToString();
                httpContext.Response.Headers["Cache-Control"] = "public, s-maxage=10";
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                Assert.Equal("1", await SendRequestAsync(address));
                Assert.Equal("1", await SendRequestAsync(address));
            }
        }

        [Fact]
        public async Task Caching_SMaxAgeAndMaxAge_SMaxAgePreferredCached()
        {
            var requestCount = 1;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.ContentType = "some/thing"; // Http.Sys requires content-type for caching
                httpContext.Response.Headers["x-request-count"] = (requestCount++).ToString();
                httpContext.Response.Headers["Cache-Control"] = "public, max-age=0, s-maxage=10";
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                Assert.Equal("1", await SendRequestAsync(address));
                Assert.Equal("1", await SendRequestAsync(address));
            }
        }

        [Fact]
        public async Task Caching_Expires_Cached()
        {
            var requestCount = 1;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.ContentType = "some/thing"; // Http.Sys requires content-type for caching
                httpContext.Response.Headers["x-request-count"] = (requestCount++).ToString();
                httpContext.Response.Headers["Cache-Control"] = "public";
                httpContext.Response.Headers["Expires"] = (DateTime.UtcNow + TimeSpan.FromSeconds(10)).ToString("r");
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                Assert.Equal("1", await SendRequestAsync(address));
                Assert.Equal("1", await SendRequestAsync(address));
            }
        }

        [Theory]
        [InlineData("Set-cookie")]
        [InlineData("vary")]
        [InlineData("pragma")]
        public async Task Caching_DisallowedResponseHeaders_NotCached(string headerName)
        {
            var requestCount = 1;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.ContentType = "some/thing"; // Http.Sys requires content-type for caching
                httpContext.Response.Headers["x-request-count"] = (requestCount++).ToString();
                httpContext.Response.Headers["Cache-Control"] = "public, max-age=10";
                httpContext.Response.Headers[headerName] = "headerValue";
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                Assert.Equal("1", await SendRequestAsync(address));
                Assert.Equal("2", await SendRequestAsync(address));
            }
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-1")]
        public async Task Caching_InvalidExpires_NotCached(string expiresValue)
        {
            var requestCount = 1;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.ContentType = "some/thing"; // Http.Sys requires content-type for caching
                httpContext.Response.Headers["x-request-count"] = (requestCount++).ToString();
                httpContext.Response.Headers["Cache-Control"] = "public";
                httpContext.Response.Headers["Expires"] = expiresValue;
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                Assert.Equal("1", await SendRequestAsync(address));
                Assert.Equal("2", await SendRequestAsync(address));
            }
        }

        [Fact]
        public async Task Caching_ExpiresWithoutPublic_NotCached()
        {
            var requestCount = 1;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.ContentType = "some/thing"; // Http.Sys requires content-type for caching
                httpContext.Response.Headers["x-request-count"] = (requestCount++).ToString();
                httpContext.Response.Headers["Expires"] = (DateTime.UtcNow + TimeSpan.FromSeconds(10)).ToString("r");
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                Assert.Equal("1", await SendRequestAsync(address));
                Assert.Equal("2", await SendRequestAsync(address));
            }
        }

        [Fact]
        public async Task Caching_MaxAgeAndExpires_MaxAgePreferred()
        {
            var requestCount = 1;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.ContentType = "some/thing"; // Http.Sys requires content-type for caching
                httpContext.Response.Headers["x-request-count"] = (requestCount++).ToString();
                httpContext.Response.Headers["Cache-Control"] = "public, max-age=10";
                httpContext.Response.Headers["Expires"] = (DateTime.UtcNow - TimeSpan.FromSeconds(10)).ToString("r"); // In the past
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                Assert.Equal("1", await SendRequestAsync(address));
                Assert.Equal("1", await SendRequestAsync(address));
            }
        }

        private async Task<string> SendRequestAsync(string uri)
        {
            using (var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(10) })
            {
                var response = await client.GetAsync(uri);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(10, response.Content.Headers.ContentLength);
                Assert.Equal(new byte[10], await response.Content.ReadAsByteArrayAsync());
                return response.Headers.GetValues("x-request-count").FirstOrDefault();
            }
        }
    }
}
