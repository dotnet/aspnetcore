// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys.FunctionalTests
{
    public class ResponseCachingTests
    {
        private readonly string _absoluteFilePath;
        private readonly long _fileLength;

        public ResponseCachingTests()
        {
            _absoluteFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Microsoft.AspNetCore.Server.HttpSys.dll");
            _fileLength = new FileInfo(_absoluteFilePath).Length;
        }

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
        [QuarantinedTest("https://github.com/dotnet/aspnetcore-internal/issues/2135")]
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
        [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8, SkipReason = "Content type not required for caching on Win7.")]
        public async Task Caching_WithoutContentType_NotCached()
        {
            var requestCount = 1;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                // httpContext.Response.ContentType = "some/thing"; // Http.Sys requires content-type for caching
                httpContext.Response.Headers["x-request-count"] = (requestCount++).ToString();
                httpContext.Response.Headers["Cache-Control"] = "public, max-age=10";
                httpContext.Response.ContentLength = 10;
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                Assert.Equal("1", await SendRequestAsync(address));
                Assert.Equal("2", await SendRequestAsync(address));
            }
        }

        [ConditionalFact]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore-internal/issues/2207")]
        public async Task Caching_WithoutContentType_Cached_OnWin7AndWin2008R2()
        {
            if (Utilities.IsWin8orLater)
            {
                return;
            }

            var requestCount = 1;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                // httpContext.Response.ContentType = "some/thing"; // Http.Sys requires content-type for caching
                httpContext.Response.Headers["x-request-count"] = (requestCount++).ToString();
                httpContext.Response.Headers["Cache-Control"] = "public, max-age=10";
                httpContext.Response.ContentLength = 10;
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                Assert.Equal("1", await SendRequestAsync(address));
                Assert.Equal("1", await SendRequestAsync(address));
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
        public async Task Caching_MaxAgeHuge_Cached()
        {
            var requestCount = 1;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.ContentType = "some/thing"; // Http.Sys requires content-type for caching
                httpContext.Response.Headers["x-request-count"] = (requestCount++).ToString();
                httpContext.Response.Headers["Cache-Control"] = "public, max-age=" + int.MaxValue.ToString(CultureInfo.InvariantCulture);
                httpContext.Response.ContentLength = 10;
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
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
        [QuarantinedTest("https://github.com/dotnet/aspnetcore-internal/issues/2208")]
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

        [ConditionalFact]
        public async Task Caching_Flush_NotCached()
        {
            var requestCount = 1;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.ContentType = "some/thing"; // Http.Sys requires content-type for caching
                httpContext.Response.Headers["x-request-count"] = (requestCount++).ToString();
                httpContext.Response.Headers["Cache-Control"] = "public, max-age=10";
                httpContext.Response.ContentLength = 10;
                httpContext.Response.Body.FlushAsync();
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                Assert.Equal("1", await SendRequestAsync(address));
                Assert.Equal("2", await SendRequestAsync(address));
            }
        }

        [ConditionalFact]
        public async Task Caching_WriteFullContentLength_Cached()
        {
            var requestCount = 1;
            string address;
            using (Utilities.CreateHttpServer(out address, async httpContext =>
            {
                httpContext.Response.ContentType = "some/thing"; // Http.Sys requires content-type for caching
                httpContext.Response.Headers["x-request-count"] = (requestCount++).ToString();
                httpContext.Response.Headers["Cache-Control"] = "public, max-age=10";
                httpContext.Response.ContentLength = 10;
                await httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
                // Http.Sys will add this for us
                Assert.Null(httpContext.Response.ContentLength);
            }))
            {
                Assert.Equal("1", await SendRequestAsync(address));
                Assert.Equal("1", await SendRequestAsync(address));
            }
        }

        [ConditionalFact]
        public async Task Caching_SendFileNoContentLength_NotCached()
        {
            var requestCount = 1;
            string address;
            using (Utilities.CreateHttpServer(out address, async httpContext =>
            {
                httpContext.Response.ContentType = "some/thing"; // Http.Sys requires content-type for caching
                httpContext.Response.Headers["x-request-count"] = (requestCount++).ToString();
                httpContext.Response.Headers["Cache-Control"] = "public, max-age=10";
                await httpContext.Response.SendFileAsync(_absoluteFilePath, 0, null, CancellationToken.None);
            }))
            {
                Assert.Equal("1", await GetFileAsync(address));
                Assert.Equal("2", await GetFileAsync(address));
            }
        }

        [ConditionalFact]
        public async Task Caching_SendFileWithFullContentLength_Cached()
        {
            var requestCount = 1;
            string address;
            using (Utilities.CreateHttpServer(out address, async httpContext =>
            {
                httpContext.Response.ContentType = "some/thing"; // Http.Sys requires content-type for caching
                httpContext.Response.Headers["x-request-count"] = (requestCount++).ToString();
                httpContext.Response.Headers["Cache-Control"] = "public, max-age=10";
                httpContext.Response.ContentLength = _fileLength;
                await httpContext.Response.SendFileAsync(_absoluteFilePath, 0, null, CancellationToken.None);
            }))
            {
                Assert.Equal("1", await GetFileAsync(address));
                Assert.Equal("1", await GetFileAsync(address));
            }
        }

        [ConditionalFact]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore-internal/issues/2209")]
        public async Task Caching_VariousStatusCodes_Cached()
        {
            var requestCount = 1;
            string address;
            using (Utilities.CreateHttpServer(out address, httpContext =>
            {
                httpContext.Response.ContentType = "some/thing"; // Http.Sys requires content-type for caching
                httpContext.Response.Headers["x-request-count"] = (requestCount++).ToString();
                httpContext.Response.Headers["Cache-Control"] = "public, max-age=10";
                var status = int.Parse(httpContext.Request.Path.Value.Substring(1));
                httpContext.Response.StatusCode = status;
                httpContext.Response.ContentLength = 10;
                return httpContext.Response.Body.WriteAsync(new byte[10], 0, 10);
            }))
            {
                // Http.Sys will cache almost any status code.
                for (int status = 200; status < 600; status++)
                {
                    switch (status)
                    {
                        case 206: // 206 (Partial Content) is not cached
                        case 407: // 407 (Proxy Authentication Required) makes CoreCLR's HttpClient throw
                            continue;
                    }
                    requestCount = 1;
                    try
                    {
                        Assert.Equal("1", await SendRequestAsync(address + status, status));
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Failed to get first response for {status}", ex);
                    }
                    try
                    {
                        Assert.Equal("1", await SendRequestAsync(address + status, status));
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Failed to get second response for {status}", ex);
                    }
                }
            }
        }

        private async Task<string> SendRequestAsync(string uri, int status = 200)
        {
            using (var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(10) })
            {
                var response = await client.GetAsync(uri);
                Assert.Equal(status, (int)response.StatusCode);
                if (status != 204 && status != 304)
                {
                    Assert.Equal(10, response.Content.Headers.ContentLength);
                    Assert.Equal(new byte[10], await response.Content.ReadAsByteArrayAsync());
                }
                return response.Headers.GetValues("x-request-count").FirstOrDefault();
            }
        }

        private async Task<string> GetFileAsync(string uri)
        {
            using (var client = new HttpClient() { Timeout = TimeSpan.FromSeconds(10) })
            {
                var response = await client.GetAsync(uri);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(_fileLength, response.Content.Headers.ContentLength);
                return response.Headers.GetValues("x-request-count").FirstOrDefault();
            }
        }
    }
}
