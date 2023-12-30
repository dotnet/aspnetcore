// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.IIS;
using Microsoft.AspNetCore.Server.IIS.FunctionalTests;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using BadHttpRequestException = Microsoft.AspNetCore.Http.BadHttpRequestException;

namespace IIS.Tests;

[SkipIfHostableWebCoreNotAvailable]
[MinimumOSVersion(OperatingSystems.Windows, WindowsVersions.Win8, SkipReason = "https://github.com/aspnet/IISIntegration/issues/866")]
[SkipOnHelix("Unsupported queue", Queues = "Windows.Amd64.VS2022.Pre.Open;")]
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
        VerifyLogs(exception);
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
        VerifyLogs(exception);
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
        VerifyLogs(exception);
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
                requestRejectedEx1 = await Assert.ThrowsAnyAsync<BadHttpRequestException>(
                    async () => await ctx.Request.Body.ReadAsync(buffer, 0, 1));
                requestRejectedEx2 = await Assert.ThrowsAnyAsync<BadHttpRequestException>(
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
        VerifyLogs(requestRejectedEx2);
    }

    private void VerifyLogs(BadHttpRequestException thrownError)
    {
        // Bad requests should not be logged over LogLevel.Debug because this can create a lot of log noise that cannot
        // be controlled by the app. We have no choice but to throw if the bad request is not observed until after the
        // app starts reading the request body. We log ApplicationErrors because these tests rethrow the
        // BadHttpRequestExceptions. IIS should emit no other logs over LogLevel.Debug for these bad requests.
        var appErrorLog = Assert.Single(TestSink.Writes, w => w.LoggerName == "Microsoft.AspNetCore.Server.IIS.Core.IISHttpServer" && w.LogLevel > LogLevel.Debug);
        var badRequestLog = Assert.Single(TestSink.Writes, w => w.LoggerName == "Microsoft.AspNetCore.Server.IIS.Core.IISHttpServer" && w.EventId == new EventId(4, "ConnectionBadRequest"));

        Assert.Equal(new EventId(2, "ApplicationError"), appErrorLog.EventId);
        Assert.Equal(LogLevel.Error, appErrorLog.LogLevel);
        Assert.Same(thrownError, appErrorLog.Exception);
        Assert.Same(thrownError, badRequestLog.Exception);
    }
}
