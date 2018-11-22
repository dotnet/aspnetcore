// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Xunit;

namespace Microsoft.AspNetCore.Server.HttpSys.Listener
{
    public class ResponseCachingTests
    {
        private readonly string _absoluteFilePath;
        private readonly long _fileLength;

        public ResponseCachingTests()
        {
            _absoluteFilePath = Directory.GetFiles(Directory.GetCurrentDirectory()).First();
            _fileLength = new FileInfo(_absoluteFilePath).Length;
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows, WindowsVersions.Win2008R2, WindowsVersions.Win7, SkipReason = "Content type not required for caching on Win7 and Win2008R2.")]
        public async Task Caching_SetTtlWithoutContentType_NotCached()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                // Http.sys requires a content-type to cache
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());

                responseTask = SendRequestAsync(address);

                context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "2";
                // Http.sys requires a content-type to cache
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                context.Dispose();

                response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("2", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [ConditionalFact]
        public async Task Caching_SetTtlWithoutContentType_Cached_OnWin7AndWin2008R2()
        {
            if (Utilities.IsWin8orLater)
            {
                return;
            }

            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                // Http.sys does not require a content-type to cache on Win7 and Win2008R2
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());

                // Send a second request and make sure we get the same response (without listening for one on the server).
                response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [ConditionalFact]
        public async Task Caching_SetTtlWithContentType_Cached()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());

                // Send a second request and make sure we get the same response (without listening for one on the server).
                response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [ConditionalFact]
        // Http.Sys does not set the optional Age header for cached content.
        // http://tools.ietf.org/html/rfc7234#section-5.1
        public async Task Caching_CheckAge_NotSentWithCachedContent()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
                Assert.False(response.Headers.Age.HasValue);

                // Send a second request and make sure we get the same response (without listening for one on the server).
                response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
                Assert.False(response.Headers.Age.HasValue);
            }
        }

        [ConditionalFact]
        // Http.Sys does not update the optional Age header for cached content.
        // http://tools.ietf.org/html/rfc7234#section-5.1
        public async Task Caching_SetAge_AgeHeaderCachedAndNotUpdated()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.Headers["age"] = "12345";
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
                Assert.True(response.Headers.Age.HasValue);
                Assert.Equal(TimeSpan.FromSeconds(12345), response.Headers.Age.Value);

                // Send a second request and make sure we get the same response (without listening for one on the server).
                response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
                Assert.True(response.Headers.Age.HasValue);
                Assert.Equal(TimeSpan.FromSeconds(12345), response.Headers.Age.Value);
            }
        }

        [ConditionalFact]
        public async Task Caching_SetTtlZeroSeconds_NotCached()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.CacheTtl = TimeSpan.FromSeconds(0);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());

                responseTask = SendRequestAsync(address);

                context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "2";
                // Http.sys requires a content-type to cache
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                context.Dispose();

                response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("2", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [ConditionalFact]
        public async Task Caching_SetTtlMiliseconds_NotCached()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.CacheTtl = TimeSpan.FromMilliseconds(900);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());

                responseTask = SendRequestAsync(address);

                context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "2";
                // Http.sys requires a content-type to cache
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                context.Dispose();

                response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("2", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [ConditionalFact]
        public async Task Caching_SetTtlNegative_NotCached()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.CacheTtl = TimeSpan.FromSeconds(-10);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());

                responseTask = SendRequestAsync(address);

                context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "2";
                // Http.sys requires a content-type to cache
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                context.Dispose();

                response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("2", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [ConditionalFact]
        public async Task Caching_SetTtlHuge_Cached()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.CacheTtl = TimeSpan.MaxValue;
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());

                // Send a second request and make sure we get the same response (without listening for one on the server).
                response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [ConditionalFact]
        public async Task Caching_SetTtlAndWriteBody_Cached()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.ContentLength = 10;
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                await context.Response.Body.WriteAsync(new byte[10], 0, 10);
                // Http.Sys will add this for us
                Assert.Null(context.Response.ContentLength);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[10], await response.Content.ReadAsByteArrayAsync());

                // Send a second request and make sure we get the same response (without listening for one on the server).
                response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[10], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [ConditionalFact]
        public async Task Caching_SetTtlAndWriteAsyncBody_Cached()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.ContentLength = 10;
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                await context.Response.Body.WriteAsync(new byte[10], 0, 10);
                // Http.Sys will add this for us
                Assert.Null(context.Response.ContentLength);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[10], await response.Content.ReadAsByteArrayAsync());

                // Send a second request and make sure we get the same response (without listening for one on the server).
                response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[10], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [ConditionalFact]
        public async Task Caching_Flush_NotCached()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address);

                server.Options.AllowSynchronousIO = true;
                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                context.Response.Body.Flush();
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());

                responseTask = SendRequestAsync(address);

                context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "2";
                context.Dispose();

                response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("2", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [ConditionalFact]
        public async Task Caching_WriteFlush_NotCached()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                await context.Response.Body.WriteAsync(new byte[10], 0, 10);
                await context.Response.Body.FlushAsync();
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[10], await response.Content.ReadAsByteArrayAsync());

                responseTask = SendRequestAsync(address);

                context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "2";
                context.Dispose();

                response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("2", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [ConditionalFact]
        public async Task Caching_WriteFullContentLength_Cached()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.ContentLength = 10;
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                await context.Response.Body.WriteAsync(new byte[10], 0, 10);
                // Http.Sys will add this for us
                Assert.Null(context.Response.ContentLength);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(10, response.Content.Headers.ContentLength);

                // Send a second request and make sure we get the same response (without listening for one on the server).
                response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(10, response.Content.Headers.ContentLength);
            }
        }

        [ConditionalFact]
        public async Task Caching_SendFileNoContentLength_NotCached()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                await context.Response.SendFileAsync(_absoluteFilePath, 0, null, CancellationToken.None);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(_fileLength, response.Content.Headers.ContentLength);

                responseTask = SendRequestAsync(address);

                context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "2";
                context.Dispose();

                response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("2", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [ConditionalFact]
        public async Task Caching_SendFileWithFullContentLength_Cached()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.ContentLength =_fileLength;
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                await context.Response.SendFileAsync(_absoluteFilePath, 0, null, CancellationToken.None);
                // Http.Sys will add this for us
                Assert.Null(context.Response.ContentLength);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(_fileLength, response.Content.Headers.ContentLength);

                // Send a second request and make sure we get the same response (without listening for one on the server).
                response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(_fileLength, response.Content.Headers.ContentLength);
            }
        }

        [ConditionalFact]
        public async Task Caching_SetTtlAndStatusCode_Cached()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                // Http.Sys will cache almost any status code.
                for (int status = 200; status < 600; status++)
                {
                    switch (status)
                    {
                        case 206: // 206 (Partial Content) is not cached
                        case 407: // 407 (Proxy Authentication Required) makes CoreCLR's HttpClient throw
                            continue;
                    }

                    var responseTask = SendRequestAsync(address + status);

                    var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                    context.Response.StatusCode = status;
                    context.Response.Headers["x-request-count"] = status.ToString();
                    context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                    context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                    context.Dispose();

                    HttpResponseMessage response;
                    try
                    {
                        response = await responseTask;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Failed to get first response for {status}", ex);
                    }
                    Assert.Equal(status, (int)response.StatusCode);
                    Assert.Equal(status.ToString(), response.Headers.GetValues("x-request-count").FirstOrDefault());
                    Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());

                    // Send a second request and make sure we get the same response (without listening for one on the server).
                    try
                    {
                        response = await SendRequestAsync(address + status);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Failed to get second response for {status}", ex);
                    }
                    Assert.Equal(status, (int)response.StatusCode);
                    Assert.Equal(status.ToString(), response.Headers.GetValues("x-request-count").FirstOrDefault());
                    Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
                }
            }
        }

        // Only GET requests can have cached responses.
        [ConditionalTheory]
        // See HTTP_VERB for known verbs
        [InlineData("HEAD")]
        [InlineData("UNKNOWN")]
        [InlineData("INVALID")]
        [InlineData("OPTIONS")]
        [InlineData("DELETE")]
        [InlineData("TRACE")]
        [InlineData("TRACK")]
        [InlineData("MOVE")]
        [InlineData("COPY")]
        [InlineData("PROPFIND")]
        [InlineData("PROPPATCH")]
        [InlineData("MKCOL")]
        [InlineData("LOCK")]
        [InlineData("UNLOCK")]
        [InlineData("SEARCH")]
        [InlineData("CUSTOMVERB")]
        [InlineData("PATCH")]
        [InlineData("POST")]
        [InlineData("PUT")]
        // [InlineData("CONNECT", null)] 400 bad request if it's not a WebSocket handshake.
        public async Task Caching_VariousUnsupportedRequestMethods_NotCached(string method)
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address, method);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = context.Request.Method + "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(method + "1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());

                responseTask = SendRequestAsync(address, method);

                context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = context.Request.Method + "2";
                // Http.sys requires a content-type to cache
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                context.Dispose();

                response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(method + "2", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
            }
        }

        // RFC violation. http://tools.ietf.org/html/rfc7234#section-4.4
        // "A cache MUST invalidate the effective Request URI ... when a non-error status code
        // is received in response to an unsafe request method."
        [ConditionalTheory]
        // See HTTP_VERB for known verbs
        [InlineData("HEAD")]
        [InlineData("UNKNOWN")]
        [InlineData("INVALID")]
        [InlineData("OPTIONS")]
        [InlineData("DELETE")]
        [InlineData("TRACE")]
        [InlineData("TRACK")]
        [InlineData("MOVE")]
        [InlineData("COPY")]
        [InlineData("PROPFIND")]
        [InlineData("PROPPATCH")]
        [InlineData("MKCOL")]
        [InlineData("LOCK")]
        [InlineData("UNLOCK")]
        [InlineData("SEARCH")]
        [InlineData("CUSTOMVERB")]
        [InlineData("PATCH")]
        [InlineData("POST")]
        [InlineData("PUT")]
        // [InlineData("CONNECT", null)] 400 bad request if it's not a WebSocket handshake.
        public async Task Caching_UnsupportedRequestMethods_BypassCacheAndLeaveItIntact(string method)
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                // Cache the first response
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = context.Request.Method + "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("GET1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());

                // Try to clear the cache with a second request
                responseTask = SendRequestAsync(address, method);

                context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = context.Request.Method + "2";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Dispose();

                response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(method + "2", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());

                // Send a third request to check the cache.
                responseTask = SendRequestAsync(address);

                // The cache wasn't cleared when it should have been
                response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("GET1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
            }
        }

        // RFC violation / implementation limiation, Vary is not respected.
        // http://tools.ietf.org/html/rfc7234#section-4.1
        [ConditionalFact]
        public async Task Caching_SetVary_NotRespected()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address, "GET", "x-vary", "vary1");

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.Headers["vary"] = "x-vary";
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal("x-vary", response.Headers.GetValues("vary").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());

                // Send a second request and make sure we get the same response (without listening for one on the server).
                response = await SendRequestAsync(address, "GET", "x-vary", "vary2");
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal("x-vary", response.Headers.GetValues("vary").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
            }
        }

        // http://tools.ietf.org/html/rfc7234#section-3.2
        [ConditionalFact]
        public async Task Caching_RequestAuthorization_NotCached()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address, "GET", "Authorization", "Basic abc123");

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());

                responseTask = SendRequestAsync(address, "GET", "Authorization", "Basic abc123");

                context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "2";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Dispose();

                // Send a second request and make sure we get the same response (without listening for one on the server).
                response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("2", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
            }
        }

        [ConditionalFact]
        public async Task Caching_RequestAuthorization_NotServedFromCache()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());

                responseTask = SendRequestAsync(address, "GET", "Authorization", "Basic abc123");

                context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "2";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Dispose();

                // Send a second request and make sure we get the same response (without listening for one on the server).
                response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("2", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
            }
        }

        // Responses can be cached for requests with Pragma: no-cache.
        // http://tools.ietf.org/html/rfc7234#section-5.2.1.4
        [ConditionalFact]
        public async Task Caching_RequestPragmaNoCache_Cached()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address, "GET", "Pragma", "no-cache");

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());

                response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
            }
        }

        // RFC violation, Requests with Pragma: no-cache should not be served from cache.
        // http://tools.ietf.org/html/rfc7234#section-5.4
        // http://tools.ietf.org/html/rfc7234#section-5.2.1.4
        [ConditionalFact]
        public async Task Caching_RequestPragmaNoCache_NotRespectedAndServedFromCache()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());

                response = await SendRequestAsync(address, "GET", "Pragma", "no-cache");
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
            }
        }

        // Responses can be cached for requests with cache-control: no-cache.
        // http://tools.ietf.org/html/rfc7234#section-5.2.1.4
        [ConditionalFact]
        public async Task Caching_RequestCacheControlNoCache_Cached()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address, "GET", "Cache-Control", "no-cache");

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());

                response = await SendRequestAsync(address);
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
            }
        }

        // RFC violation, Requests with Cache-Control: no-cache should not be served from cache.
        // http://tools.ietf.org/html/rfc7234#section-5.2.1.4
        [ConditionalFact]
        public async Task Caching_RequestCacheControlNoCache_NotRespectedAndServedFromCache()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());

                response = await SendRequestAsync(address, "GET", "Cache-Control", "no-cache");
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
            }
        }

        // RFC violation
        // http://tools.ietf.org/html/rfc7234#section-5.2.1.1
        [ConditionalFact]
        public async Task Caching_RequestCacheControlMaxAgeZero_NotRespectedAndServedFromCache()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());

                response = await SendRequestAsync(address, "GET", "Cache-Control", "min-fresh=0");
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
            }
        }

        // RFC violation
        // http://tools.ietf.org/html/rfc7234#section-5.2.1.3
        [ConditionalFact]
        public async Task Caching_RequestCacheControlMinFreshOutOfRange_NotRespectedAndServedFromCache()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());

                response = await SendRequestAsync(address, "GET", "Cache-Control", "min-fresh=20");
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[0], await response.Content.ReadAsByteArrayAsync());
            }
        }

        // Http.Sys limitation, partial responses are not cached.
        [ConditionalFact]
        public async Task Caching_CacheRange_NotCached()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address, "GET", "Range", "bytes=0-10");

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.StatusCode = 206;
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.Headers["content-range"] = "bytes 0-10/100";
                context.Response.ContentLength = 11;
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                await context.Response.Body.WriteAsync(new byte[100], 0, 11);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(206, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[11], await response.Content.ReadAsByteArrayAsync());

                responseTask = SendRequestAsync(address, "GET", "Range", "bytes=0-10");

                context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.StatusCode = 206;
                context.Response.Headers["x-request-count"] = "2";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.Headers["content-range"] = "bytes 0-10/100";
                context.Response.ContentLength = 11;
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                await context.Response.Body.WriteAsync(new byte[100], 0, 11);
                context.Dispose();

                response = await responseTask;
                Assert.Equal(206, (int)response.StatusCode);
                Assert.Equal("2", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal("bytes 0-10/100", response.Content.Headers.GetValues("content-range").FirstOrDefault());
                Assert.Equal(new byte[11], await response.Content.ReadAsByteArrayAsync());
            }
        }

        // http://tools.ietf.org/html/rfc7233#section-4.1
        [ConditionalFact]
        public async Task Caching_RequestRangeFromCache_RangeServedFromCache()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.ContentLength = 100;
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                await context.Response.Body.WriteAsync(new byte[100], 0, 100);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[100], await response.Content.ReadAsByteArrayAsync());

                response = await SendRequestAsync(address, "GET", "Range", "bytes=0-10", HttpCompletionOption.ResponseHeadersRead);
                Assert.Equal(206, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal("bytes 0-10/100", response.Content.Headers.GetValues("content-range").FirstOrDefault());
                Assert.Equal(11, response.Content.Headers.ContentLength);
            }
        }

        // http://tools.ietf.org/html/rfc7233#section-4.1
        [ConditionalFact]
        public async Task Caching_RequestMultipleRangesFromCache_RangesServedFromCache()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.ContentLength = 100;
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                await context.Response.Body.WriteAsync(new byte[100], 0, 100);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(new byte[100], await response.Content.ReadAsByteArrayAsync());

                response = await SendRequestAsync(address, "GET", "Range", "bytes=0-10,15-20");
                Assert.Equal(206, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.StartsWith("multipart/byteranges;", response.Content.Headers.GetValues("content-type").First());
            }
        }

        [ConditionalFact]
        public async Task Caching_RequestRangeFromCachedFile_ServedFromCache()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseLength = _fileLength / 2; // Make sure it handles partial files.
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.ContentLength = responseLength;
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                await context.Response.SendFileAsync(_absoluteFilePath, 0, responseLength, CancellationToken.None);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(responseLength, response.Content.Headers.ContentLength);

                // Send a second request and make sure we get the same response (without listening for one on the server).
                var rangeLength = responseLength / 2;
                response = await SendRequestAsync(address, "GET", "Range", "bytes=0-" + (rangeLength - 1), HttpCompletionOption.ResponseHeadersRead);
                Assert.Equal(206, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(rangeLength, response.Content.Headers.ContentLength);
                Assert.Equal("bytes 0-" + (rangeLength - 1) + "/" + responseLength, response.Content.Headers.GetValues("content-range").FirstOrDefault());
            }
        }

        [ConditionalFact]
        public async Task Caching_RequestMultipleRangesFromCachedFile_ServedFromCache()
        {
            string address;
            using (var server = Utilities.CreateHttpServer(out address))
            {
                address += Guid.NewGuid().ToString(); // Avoid cache collisions for failed tests.
                var responseLength = _fileLength / 2; // Make sure it handles partial files.
                var responseTask = SendRequestAsync(address);

                var context = await server.AcceptAsync(Utilities.DefaultTimeout).Before(responseTask);
                context.Response.Headers["x-request-count"] = "1";
                context.Response.Headers["content-type"] = "some/thing"; // Http.sys requires a content-type to cache
                context.Response.ContentLength = responseLength;
                context.Response.CacheTtl = TimeSpan.FromSeconds(10);
                await context.Response.SendFileAsync(_absoluteFilePath, 0, responseLength, CancellationToken.None);
                context.Dispose();

                var response = await responseTask;
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.Equal(responseLength, response.Content.Headers.ContentLength);
                // Send a second request and make sure we get the same response (without listening for one on the server).
                var rangeLength = responseLength / 4;
                response = await SendRequestAsync(address, "GET", "Range", "bytes=0-" + (rangeLength - 1) + "," + rangeLength + "-" + (rangeLength + rangeLength - 1), HttpCompletionOption.ResponseHeadersRead);
                Assert.Equal(206, (int)response.StatusCode);
                Assert.Equal("1", response.Headers.GetValues("x-request-count").FirstOrDefault());
                Assert.StartsWith("multipart/byteranges;", response.Content.Headers.GetValues("content-type").First());
            }
        }

        private async Task<HttpResponseMessage> SendRequestAsync(string uri, string method = "GET", string extraHeader = null, string extraHeaderValue = null, HttpCompletionOption httpCompletionOption = HttpCompletionOption.ResponseContentRead)
        {
            using (var handler = new HttpClientHandler() { AllowAutoRedirect = false })
            {
                using (var client = new HttpClient(handler) { Timeout = Utilities.DefaultTimeout })
                {
                    var request = new HttpRequestMessage(new HttpMethod(method), uri);
                    if (!string.IsNullOrEmpty(extraHeader))
                    {
                        request.Headers.Add(extraHeader, extraHeaderValue);
                    }
                    return await client.SendAsync(request, httpCompletionOption);
                }
            }
        }
    }
}
