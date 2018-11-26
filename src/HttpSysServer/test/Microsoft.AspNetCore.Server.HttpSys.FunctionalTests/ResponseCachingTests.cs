// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys.FunctionalTests
{
    public class ResponseCachingTests
    {
        [ConditionalFact]
        public async Task Caching_NoCacheControl_NotCached()
        {
            var requestCount = 1;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.ContentType = "some/thing"; // Http.Sys requires content-type for caching
                httpContext.Response.Headers["x-request-count"] = (requestCount++).ToString();
                httpContext.Response.ContentLength = 10;
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                Assert.Equal("1", await SendRequestAsync(address));
                Assert.Equal("2", await SendRequestAsync(address));
            }
        }

        [ConditionalFact]
        public async Task Caching_JustPublic_NotCached()
        {
            var requestCount = 1;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.ContentType = "some/thing"; // Http.Sys requires content-type for caching
                httpContext.Response.Headers["x-request-count"] = (requestCount++).ToString();
                httpContext.Response.Headers["Cache-Control"] = "public";
                httpContext.Response.ContentLength = 10;
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                Assert.Equal("1", await SendRequestAsync(address));
                Assert.Equal("2", await SendRequestAsync(address));
            }
        }

        [ConditionalFact]
        public async Task Caching_MaxAge_Cached()
        {
            var requestCount = 1;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.ContentType = "some/thing"; // Http.Sys requires content-type for caching
                httpContext.Response.Headers["x-request-count"] = (requestCount++).ToString();
                httpContext.Response.Headers["Cache-Control"] = "public, max-age=10";
                httpContext.Response.ContentLength = 10;
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                Assert.Equal("1", await SendRequestAsync(address));
                Assert.Equal("1", await SendRequestAsync(address));
            }
        }

        [ConditionalFact]
        public async Task Caching_SMaxAge_Cached()
        {
            var requestCount = 1;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.ContentType = "some/thing"; // Http.Sys requires content-type for caching
                httpContext.Response.Headers["x-request-count"] = (requestCount++).ToString();
                httpContext.Response.Headers["Cache-Control"] = "public, s-maxage=10";
                httpContext.Response.ContentLength = 10;
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                Assert.Equal("1", await SendRequestAsync(address));
                Assert.Equal("1", await SendRequestAsync(address));
            }
        }

        [ConditionalFact]
        public async Task Caching_SMaxAgeAndMaxAge_SMaxAgePreferredCached()
        {
            var requestCount = 1;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.ContentType = "some/thing"; // Http.Sys requires content-type for caching
                httpContext.Response.Headers["x-request-count"] = (requestCount++).ToString();
                httpContext.Response.Headers["Cache-Control"] = "public, max-age=0, s-maxage=10";
                httpContext.Response.ContentLength = 10;
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                Assert.Equal("1", await SendRequestAsync(address));
                Assert.Equal("1", await SendRequestAsync(address));
            }
        }

        [ConditionalFact]
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
                httpContext.Response.ContentLength = 10;
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                Assert.Equal("1", await SendRequestAsync(address));
                Assert.Equal("1", await SendRequestAsync(address));
            }
        }

        [ConditionalTheory]
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
                httpContext.Response.ContentLength = 10;
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                Assert.Equal("1", await SendRequestAsync(address));
                Assert.Equal("2", await SendRequestAsync(address));
            }
        }

        [ConditionalTheory]
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
                httpContext.Response.ContentLength = 10;
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                Assert.Equal("1", await SendRequestAsync(address));
                Assert.Equal("2", await SendRequestAsync(address));
            }
        }

        [ConditionalFact]
        public async Task Caching_ExpiresWithoutPublic_NotCached()
        {
            var requestCount = 1;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.ContentType = "some/thing"; // Http.Sys requires content-type for caching
                httpContext.Response.Headers["x-request-count"] = (requestCount++).ToString();
                httpContext.Response.Headers["Expires"] = (DateTime.UtcNow + TimeSpan.FromSeconds(10)).ToString("r");
                httpContext.Response.ContentLength = 10;
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                Assert.Equal("1", await SendRequestAsync(address));
                Assert.Equal("2", await SendRequestAsync(address));
            }
        }

        [ConditionalFact]
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
                httpContext.Response.ContentLength = 10;
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
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
