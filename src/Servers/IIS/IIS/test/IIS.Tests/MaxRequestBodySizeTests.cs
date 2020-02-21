// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace IIS.Tests
{
    [SkipIfHostableWebCoreNotAvailable]
    [MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8, SkipReason = "https://github.com/aspnet/IISIntegration/issues/866")]
    public class MaxRequestBodySizeTests : LoggedTest
    {
        [ConditionalFact]
        public async Task RequestBodyTooLargeContentLengthExceedsGlobalLimit()
        {
            var globalMaxRequestBodySize = 0x100000000;

            BadHttpRequestException exception = null;
            using (var testServer = await TestServer.Create(
                async ctx =>
                {
                    try
                    {
                        await ctx.Request.Body.ReadAsync(new byte[2000]);
                    }
                    catch (BadHttpRequestException ex)
                    {
                        exception = ex;
                        throw ex;
                    }
                }, LoggerFactory))
            {
                using (var connection = testServer.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        $"Content-Length: {globalMaxRequestBodySize + 1}",
                        "Host: localhost",
                        "",
                        "");
                    await connection.Receive("HTTP/1.1 413 Payload Too Large");
                }
            }

            Assert.Equal(CoreStrings.BadRequest_RequestBodyTooLarge, exception.Message);
        }

        [ConditionalFact]
        public async Task RequestBodyTooLargeContentLengthExceedingPerRequestLimit()
        {
            var maxRequestSize = 0x10000;
            var perRequestMaxRequestBodySize = 0x100;

            BadHttpRequestException exception = null;
            using (var testServer = await TestServer.Create(
                async ctx =>
                {
                    try
                    {
                        var feature = ctx.Features.Get<IHttpMaxRequestBodySizeFeature>();
                        Assert.Equal(maxRequestSize, feature.MaxRequestBodySize);
                        feature.MaxRequestBodySize = perRequestMaxRequestBodySize;

                        await ctx.Request.Body.ReadAsync(new byte[2000]);
                    }
                    catch (BadHttpRequestException ex)
                    {
                        exception = ex;
                        throw ex;
                    }
                }, LoggerFactory, new IISServerOptions { MaxRequestBodySize = maxRequestSize }))
            {
                using (var connection = testServer.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        $"Content-Length: {perRequestMaxRequestBodySize + 1}",
                        "Host: localhost",
                        "",
                        "");
                    await connection.Receive("HTTP/1.1 413 Payload Too Large");
                }
            }

            Assert.Equal(CoreStrings.BadRequest_RequestBodyTooLarge, exception.Message);
        }

        [ConditionalFact]
        public async Task DoesNotRejectRequestWithContentLengthHeaderExceedingGlobalLimitIfLimitDisabledPerRequest()
        {
            using (var testServer = await TestServer.Create(
                async ctx =>
                {
                    var feature = ctx.Features.Get<IHttpMaxRequestBodySizeFeature>();
                    Assert.Equal(0, feature.MaxRequestBodySize);
                    feature.MaxRequestBodySize = null;

                    await ctx.Request.Body.ReadAsync(new byte[2000]);

                }, LoggerFactory, new IISServerOptions { MaxRequestBodySize = 0 }))
            {
                using (var connection = testServer.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        $"Content-Length: 1",
                        "Host: localhost",
                        "",
                        "A");
                    await connection.Receive("HTTP/1.1 200 OK");
                }
            }
        }

        [ConditionalFact]
        public async Task DoesNotRejectRequestWithChunkedExceedingGlobalLimitIfLimitDisabledPerRequest()
        {
            using (var testServer = await TestServer.Create(
                async ctx =>
                {
                    var feature = ctx.Features.Get<IHttpMaxRequestBodySizeFeature>();
                    Assert.Equal(0, feature.MaxRequestBodySize);
                    feature.MaxRequestBodySize = null;

                    await ctx.Request.Body.ReadAsync(new byte[2000]);

                }, LoggerFactory, new IISServerOptions { MaxRequestBodySize = 0 }))
            {
                using (var connection = testServer.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        $"Transfer-Encoding: chunked",
                        "Host: localhost",
                        "",
                        "1",
                        "a",
                        "0",
                        "");
                    await connection.Receive("HTTP/1.1 200 OK");
                }
            }
        }

        [ConditionalFact]
        public async Task DoesNotRejectBodylessGetRequestWithZeroMaxRequestBodySize()
        {
            using (var testServer = await TestServer.Create(
                async ctx =>
                {
                    await ctx.Request.Body.ReadAsync(new byte[2000]);

                }, LoggerFactory, new IISServerOptions { MaxRequestBodySize = 0 }))
            {
                using (var connection = testServer.CreateConnection())
                {
                    await connection.Send(
                        "GET / HTTP/1.1",
                        "Host: localhost",
                        "",
                        "");

                    await connection.Receive("HTTP/1.1 200 OK");
                }
            }
        }

        [ConditionalFact]
        public async Task DoesNotRejectBodylessPostWithZeroContentLengthRequestWithZeroMaxRequestBodySize()
        {
            using (var testServer = await TestServer.Create(
                async ctx =>
                {
                    await ctx.Request.Body.ReadAsync(new byte[2000]);

                }, LoggerFactory, new IISServerOptions { MaxRequestBodySize = 0 }))
            {
                using (var connection = testServer.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        $"Content-Length: 0",
                        "Host: localhost",
                        "",
                        "");

                    await connection.Receive("HTTP/1.1 200 OK");
                }
            }
        }

        [ConditionalFact]
        public async Task DoesNotRejectBodylessPostWithEmptyChunksRequestWithZeroMaxRequestBodySize()
        {
            using (var testServer = await TestServer.Create(
                async ctx =>
                {
                    await ctx.Request.Body.ReadAsync(new byte[2000]);

                }, LoggerFactory, new IISServerOptions { MaxRequestBodySize = 0 }))
            {
                using (var connection = testServer.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        $"Transfer-Encoding: chunked",
                        "Host: localhost",
                        "",
                        "0",
                        "",
                        "");

                    await connection.Receive("HTTP/1.1 200 OK");
                }
            }
        }


        [ConditionalFact]
        public async Task SettingMaxRequestBodySizeAfterReadingFromRequestBodyThrows()
        {
            var perRequestMaxRequestBodySize = 0x10;
            var payloadSize = perRequestMaxRequestBodySize + 1;
            var payload = new string('A', payloadSize);
            InvalidOperationException invalidOpEx = null;

            using (var testServer = await TestServer.Create(
               async ctx =>
               {
                   var buffer = new byte[1];
                   Assert.Equal(1, await ctx.Request.Body.ReadAsync(buffer, 0, 1));

                   var feature = ctx.Features.Get<IHttpMaxRequestBodySizeFeature>();
                   Assert.True(feature.IsReadOnly);

                   invalidOpEx = Assert.Throws<InvalidOperationException>(() =>
                       feature.MaxRequestBodySize = perRequestMaxRequestBodySize);
                   throw invalidOpEx;
               }, LoggerFactory))
            {
                using (var connection = testServer.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Host: localhost",
                        "Content-Length: " + payloadSize,
                        "",
                        payload);
                    await connection.Receive(
                        "HTTP/1.1 500 Internal Server Error");
                }
            }
        }

        [ConditionalFact]
        public async Task RequestBodyTooLargeChunked()
        {
            var maxRequestSize = 0x1000;

            BadHttpRequestException exception = null;
            using (var testServer = await TestServer.Create(
                async ctx =>
                {
                    try
                    {
                        while (true)
                        {
                            var num = await ctx.Request.Body.ReadAsync(new byte[2000]);
                        }
                    }
                    catch (BadHttpRequestException ex)
                    {
                        exception = ex;
                        throw ex;
                    }
                }, LoggerFactory, new IISServerOptions { MaxRequestBodySize = maxRequestSize }))
            {
                using (var connection = testServer.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Transfer-Encoding: chunked",
                        "Host: localhost",
                        "",
                        "1001",
                        new string('a', 4097),
                        "0",
                        "");
                    await connection.Receive("HTTP/1.1 413 Payload Too Large");
                }
            }

            Assert.NotNull(exception);
            Assert.Equal(CoreStrings.BadRequest_RequestBodyTooLarge, exception.Message);
        }

        [ConditionalFact]
        public async Task EveryReadFailsWhenContentLengthHeaderExceedsGlobalLimit()
        {
            BadHttpRequestException requestRejectedEx1 = null;
            BadHttpRequestException requestRejectedEx2 = null;
            using (var testServer = await TestServer.Create(
                async ctx =>
                {
                    var buffer = new byte[1];
                    requestRejectedEx1 = await Assert.ThrowsAsync<BadHttpRequestException>(
                        async () => await ctx.Request.Body.ReadAsync(buffer, 0, 1));
                    requestRejectedEx2 = await Assert.ThrowsAsync<BadHttpRequestException>(
                        async () => await ctx.Request.Body.ReadAsync(buffer, 0, 1));
                    throw requestRejectedEx2;
                }, LoggerFactory, new IISServerOptions { MaxRequestBodySize = 0 }))
            {
                using (var connection = testServer.CreateConnection())
                {
                    await connection.Send(
                        "POST / HTTP/1.1",
                        "Host: localhost",
                        "Content-Length: " + (new IISServerOptions().MaxRequestBodySize + 1),
                        "",
                        "");
                    await connection.Receive(
                        "HTTP/1.1 413 Payload Too Large");
                }
            }

            Assert.NotNull(requestRejectedEx1);
            Assert.NotNull(requestRejectedEx2);
            Assert.Equal(CoreStrings.BadRequest_RequestBodyTooLarge, requestRejectedEx1.Message);
            Assert.Equal(CoreStrings.BadRequest_RequestBodyTooLarge, requestRejectedEx2.Message);
        }
    }
}
