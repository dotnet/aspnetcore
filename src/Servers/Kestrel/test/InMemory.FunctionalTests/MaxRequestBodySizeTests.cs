// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests.TestTransport;
using Microsoft.AspNetCore.InternalTesting;
using Xunit;
using BadHttpRequestException = Microsoft.AspNetCore.Server.Kestrel.Core.BadHttpRequestException;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.InMemory.FunctionalTests;

public class MaxRequestBodySizeTests : LoggedTest
{
    [Fact]
    public async Task RejectsRequestWithContentLengthHeaderExceedingGlobalLimit()
    {
        // 4 GiB
        var globalMaxRequestBodySize = 0x100000000;
#pragma warning disable CS0618 // Type or member is obsolete
        BadHttpRequestException requestRejectedEx = null;
#pragma warning restore CS0618 // Type or member is obsolete

        await using (var server = new TestServer(async context =>
        {
            Assert.True(context.Request.CanHaveBody());
            var buffer = new byte[1];
#pragma warning disable CS0618 // Type or member is obsolete
            requestRejectedEx = await Assert.ThrowsAsync<BadHttpRequestException>(
#pragma warning restore CS0618 // Type or member is obsolete
                    async () => await context.Request.Body.ReadAsync(buffer, 0, 1));
            throw requestRejectedEx;
        },
        new TestServiceContext(LoggerFactory) { ServerOptions = { Limits = { MaxRequestBodySize = globalMaxRequestBodySize } } }))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Content-Length: " + (globalMaxRequestBodySize + 1),
                    "",
                    "");
                await connection.ReceiveEnd(
                    "HTTP/1.1 413 Payload Too Large",
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }

        Assert.NotNull(requestRejectedEx);
        Assert.Equal(CoreStrings.FormatBadRequest_RequestBodyTooLarge(globalMaxRequestBodySize), requestRejectedEx.Message);
    }

    [Fact]
    public async Task RejectsRequestWithBodySizeExceedingPerRequestLimitAndExceptionWasCaughtByApplication()
    {
        var maxRequestBodySize = 3;
        var requestBody = "client content";
        var customApplicationResponse = "custom";
        Assert.True(requestBody.Length > maxRequestBodySize);

        await using (var server = new TestServer(async context =>
        {
#pragma warning disable CS0618 // Type or member is obsolete
            BadHttpRequestException requestRejectedEx = await Assert.ThrowsAsync<BadHttpRequestException>(async () =>
#pragma warning restore CS0618 // Type or member is obsolete
            {
                using (var stream = new StreamReader(context.Request.Body))
                {
                    string body = await stream.ReadToEndAsync();
                }
            });
            context.Response.StatusCode = requestRejectedEx.StatusCode;
            await context.Response.WriteAsync(customApplicationResponse);
            throw requestRejectedEx;
        },
        new TestServiceContext(LoggerFactory) { ServerOptions = { Limits = { MaxRequestBodySize = maxRequestBodySize } } }))
        {
            using var connection = server.CreateConnection();
            await connection.Send(
                "POST / HTTP/1.1",
                "Host:",
                $"Content-Length: {requestBody.Length}",
                "",
                requestBody);
            await connection.ReceiveEnd(
                "HTTP/1.1 413 Payload Too Large",
                "Connection: close",
                $"Date: {server.Context.DateHeaderValue}",
                "Transfer-Encoding: chunked",
                "",
                $"{customApplicationResponse.Length}",
                customApplicationResponse,
                "");
        }
    }

    [Fact]
    public async Task RejectsRequestWithChunckedBodySizeExceedingPerRequestLimitAndExceptionWasCaughtByApplication()
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

        var maxRequestBodySize = 3;
        var customApplicationResponse = "custom";
        var chunkedPayload = $"5;random chunk extension\r\nHello\r\n6\r\n World\r\n0\r\n";
        Assert.True(chunkedPayload.Length > maxRequestBodySize);

        await using (var server = new TestServer(async context =>
        {
#pragma warning disable CS0618 // Type or member is obsolete
            BadHttpRequestException requestRejectedEx = await Assert.ThrowsAsync<BadHttpRequestException>(async () =>
#pragma warning restore CS0618 // Type or member is obsolete
            {
                using (var stream = new StreamReader(context.Request.Body))
                {
                    string body = await stream.ReadToEndAsync();
                }
            });
            context.Response.StatusCode = requestRejectedEx.StatusCode;
            await context.Response.WriteAsync(customApplicationResponse);
            throw requestRejectedEx;
        },
        new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory)) { ServerOptions = { Limits = { MaxRequestBodySize = maxRequestBodySize } } }))
        {
            using var connection = server.CreateConnection();
            await connection.Send(
                "POST / HTTP/1.1",
                "Host:",
                "Transfer-Encoding: chunked",
                "",
                chunkedPayload);
            await connection.ReceiveEnd(
                "HTTP/1.1 413 Payload Too Large",
                "Connection: close",
                $"Date: {server.Context.DateHeaderValue}",
                "Transfer-Encoding: chunked",
                "",
                $"{customApplicationResponse.Length}",
                customApplicationResponse,
                "");
        }

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.Equal(ConnectionEndReason.MaxRequestBodySizeExceeded, m.Tags));
    }

    [Fact]
    public async Task RejectsRequestWithContentLengthHeaderExceedingPerRequestLimit()
    {
        // 8 GiB
        var globalMaxRequestBodySize = 0x200000000;
        // 4 GiB
        var perRequestMaxRequestBodySize = 0x100000000;
#pragma warning disable CS0618 // Type or member is obsolete
        BadHttpRequestException requestRejectedEx = null;
#pragma warning restore CS0618 // Type or member is obsolete

        await using (var server = new TestServer(async context =>
        {
            var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
            Assert.Equal(globalMaxRequestBodySize, feature.MaxRequestBodySize);

            // Disable the MaxRequestBodySize prior to calling Request.Body.ReadAsync();
            feature.MaxRequestBodySize = perRequestMaxRequestBodySize;

            var buffer = new byte[1];
#pragma warning disable CS0618 // Type or member is obsolete
            requestRejectedEx = await Assert.ThrowsAsync<BadHttpRequestException>(
#pragma warning restore CS0618 // Type or member is obsolete
                    async () => await context.Request.Body.ReadAsync(buffer, 0, 1));
            throw requestRejectedEx;
        },
        new TestServiceContext(LoggerFactory) { ServerOptions = { Limits = { MaxRequestBodySize = globalMaxRequestBodySize } } }))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Content-Length: " + (perRequestMaxRequestBodySize + 1),
                    "",
                    "");
                await connection.ReceiveEnd(
                    "HTTP/1.1 413 Payload Too Large",
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }

        Assert.NotNull(requestRejectedEx);
        Assert.Equal(CoreStrings.FormatBadRequest_RequestBodyTooLarge(perRequestMaxRequestBodySize), requestRejectedEx.Message);
    }

    [Fact]
    public async Task DoesNotRejectRequestWithContentLengthHeaderExceedingGlobalLimitIfLimitDisabledPerRequest()
    {
        await using (var server = new TestServer(async context =>
        {
            var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
            Assert.Equal(0, feature.MaxRequestBodySize);

            // Disable the MaxRequestBodySize prior to calling Request.Body.ReadAsync();
            feature.MaxRequestBodySize = null;

            var buffer = new byte[1];

            Assert.Equal(1, await context.Request.Body.ReadAsync(buffer, 0, 1));
            Assert.Equal((byte)'A', buffer[0]);
            Assert.Equal(0, await context.Request.Body.ReadAsync(buffer, 0, 1));

            context.Response.ContentLength = 1;
            await context.Response.Body.WriteAsync(buffer, 0, 1);
        },
        new TestServiceContext(LoggerFactory) { ServerOptions = { Limits = { MaxRequestBodySize = 0 } } }))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Content-Length: 1",
                    "",
                    "A");
                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 1",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "A");
            }
        }
    }

    [Fact]
    public async Task DoesNotRejectBodylessGetRequestWithZeroMaxRequestBodySize()
    {
        await using (var server = new TestServer(context => context.Request.Body.CopyToAsync(Stream.Null),
            new TestServiceContext { ServerOptions = { Limits = { MaxRequestBodySize = 0 } } }))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "GET / HTTP/1.1",
                    "Host:",
                    "",
                    "POST / HTTP/1.1",
                    "Host:",
                    "Content-Length: 1",
                    "",
                    "");
                await connection.ReceiveEnd(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "HTTP/1.1 413 Payload Too Large",
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task SettingMaxRequestBodySizeAfterReadingFromRequestBodyThrows()
    {
        var perRequestMaxRequestBodySize = 0x10;
        var payloadSize = perRequestMaxRequestBodySize + 1;
        var payload = new string('A', payloadSize);
        InvalidOperationException invalidOpEx = null;

        await using (var server = new TestServer(async context =>
        {
            var buffer = new byte[1];
            Assert.Equal(1, await context.Request.Body.ReadAsync(buffer, 0, 1));

            var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
            Assert.Equal(new KestrelServerLimits().MaxRequestBodySize, feature.MaxRequestBodySize);
            Assert.True(feature.IsReadOnly);

            invalidOpEx = Assert.Throws<InvalidOperationException>(() =>
                feature.MaxRequestBodySize = perRequestMaxRequestBodySize);
            throw invalidOpEx;
        }, new TestServiceContext(LoggerFactory)))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Content-Length: " + payloadSize,
                    "",
                    payload);
                await connection.Receive(
                    "HTTP/1.1 500 Internal Server Error",
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }

        Assert.NotNull(invalidOpEx);
        Assert.Equal(CoreStrings.MaxRequestBodySizeCannotBeModifiedAfterRead, invalidOpEx.Message);
    }

    [Fact]
    public async Task SettingMaxRequestBodySizeAfterUpgradingRequestThrows()
    {
        InvalidOperationException invalidOpEx = null;

        await using (var server = new TestServer(async context =>
        {
            var upgradeFeature = context.Features.Get<IHttpUpgradeFeature>();
            var stream = await upgradeFeature.UpgradeAsync();

            var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
            Assert.Equal(new KestrelServerLimits().MaxRequestBodySize, feature.MaxRequestBodySize);
            Assert.True(feature.IsReadOnly);

            invalidOpEx = Assert.Throws<InvalidOperationException>(() =>
                feature.MaxRequestBodySize = 0x10);
            throw invalidOpEx;
        }, new TestServiceContext(LoggerFactory)))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send("GET / HTTP/1.1",
                    "Host:",
                    "Connection: Upgrade",
                    "",
                    "");
                await connection.Receive("HTTP/1.1 101 Switching Protocols",
                    "Connection: Upgrade",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
                await connection.ReceiveEnd();
            }
        }

        Assert.NotNull(invalidOpEx);
        Assert.Equal(CoreStrings.MaxRequestBodySizeCannotBeModifiedForUpgradedRequests, invalidOpEx.Message);
    }

    [Fact]
    public async Task EveryReadFailsWhenContentLengthHeaderExceedsGlobalLimit()
    {
        var testMeterFactory = new TestMeterFactory();
        using var connectionDuration = new MetricCollector<double>(testMeterFactory, "Microsoft.AspNetCore.Server.Kestrel", "kestrel.connection.duration");

#pragma warning disable CS0618 // Type or member is obsolete
        BadHttpRequestException requestRejectedEx1 = null;
        BadHttpRequestException requestRejectedEx2 = null;
#pragma warning restore CS0618 // Type or member is obsolete

        await using (var server = new TestServer(async context =>
        {
            var buffer = new byte[1];
#pragma warning disable CS0618 // Type or member is obsolete
            requestRejectedEx1 = await Assert.ThrowsAsync<BadHttpRequestException>(
            async () => await context.Request.Body.ReadAsync(buffer, 0, 1));
            requestRejectedEx2 = await Assert.ThrowsAsync<BadHttpRequestException>(
                async () => await context.Request.Body.ReadAsync(buffer, 0, 1));
#pragma warning restore CS0618 // Type or member is obsolete
            throw requestRejectedEx2;
        },
        new TestServiceContext(LoggerFactory, metrics: new KestrelMetrics(testMeterFactory)) { ServerOptions = { Limits = { MaxRequestBodySize = 0 } } }))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Content-Length: " + (new KestrelServerLimits().MaxRequestBodySize + 1),
                    "",
                    "");
                await connection.ReceiveEnd(
                    "HTTP/1.1 413 Payload Too Large",
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }

        Assert.NotNull(requestRejectedEx1);
        Assert.NotNull(requestRejectedEx2);
        Assert.Equal(CoreStrings.FormatBadRequest_RequestBodyTooLarge(0), requestRejectedEx1.Message);
        Assert.Equal(CoreStrings.FormatBadRequest_RequestBodyTooLarge(0), requestRejectedEx2.Message);

        Assert.Collection(connectionDuration.GetMeasurementSnapshot(), m => MetricsAssert.Equal(ConnectionEndReason.MaxRequestBodySizeExceeded, m.Tags));
    }

    [Fact]
    public async Task ChunkFramingAndExtensionsCountTowardsRequestBodySize()
    {
        var chunkedPayload = "5;random chunk extension\r\nHello\r\n6\r\n World\r\n0\r\n\r\n";
        var globalMaxRequestBodySize = chunkedPayload.Length - 1;
#pragma warning disable CS0618 // Type or member is obsolete
        BadHttpRequestException requestRejectedEx = null;
#pragma warning restore CS0618 // Type or member is obsolete

        await using (var server = new TestServer(async context =>
        {
            var buffer = new byte[11];
#pragma warning disable CS0618 // Type or member is obsolete
            requestRejectedEx = await Assert.ThrowsAsync<BadHttpRequestException>(async () =>
#pragma warning restore CS0618 // Type or member is obsolete
            {
                var count = 0;
                do
                {
                    count = await context.Request.Body.ReadAsync(buffer, 0, 11);
                } while (count != 0);
            });

            throw requestRejectedEx;
        },
        new TestServiceContext(LoggerFactory) { ServerOptions = { Limits = { MaxRequestBodySize = globalMaxRequestBodySize } } }))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Transfer-Encoding: chunked",
                    "",
                    chunkedPayload);
                await connection.ReceiveEnd(
                    "HTTP/1.1 413 Payload Too Large",
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }

        Assert.NotNull(requestRejectedEx);
        Assert.Equal(CoreStrings.FormatBadRequest_RequestBodyTooLarge(globalMaxRequestBodySize), requestRejectedEx.Message);
    }

    [Fact]
    public async Task TrailingHeadersDoNotCountTowardsRequestBodySize()
    {
        var chunkedPayload = $"5;random chunk extension\r\nHello\r\n6\r\n World\r\n0\r\n";
        var trailingHeaders = "Trailing-Header: trailing-value\r\n\r\n";
        var globalMaxRequestBodySize = chunkedPayload.Length;

        await using (var server = new TestServer(async context =>
        {
            var offset = 0;
            var count = 0;
            var buffer = new byte[11];

            do
            {
                count = await context.Request.Body.ReadAsync(buffer, offset, 11 - offset);
                offset += count;
            } while (count != 0);

            Assert.Equal("Hello World", Encoding.ASCII.GetString(buffer));
            Assert.Equal("trailing-value", context.Request.GetTrailer("Trailing-Header").ToString());
        },
        new TestServiceContext(LoggerFactory) { ServerOptions = { Limits = { MaxRequestBodySize = globalMaxRequestBodySize } } }))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Transfer-Encoding: chunked",
                    "",
                    chunkedPayload + trailingHeaders);
                await connection.Receive(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }
    }

    [Fact]
    public async Task PerRequestMaxRequestBodySizeGetsReset()
    {
        var chunkedPayload = "5;random chunk extension\r\nHello\r\n6\r\n World\r\n0\r\n\r\n";
        var globalMaxRequestBodySize = chunkedPayload.Length - 1;
        var firstRequest = true;
#pragma warning disable CS0618 // Type or member is obsolete
        BadHttpRequestException requestRejectedEx = null;
#pragma warning restore CS0618 // Type or member is obsolete

        await using (var server = new TestServer(async context =>
        {
            var feature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
            Assert.Equal(globalMaxRequestBodySize, feature.MaxRequestBodySize);

            var buffer = new byte[11];
            var count = 0;

            if (firstRequest)
            {
                firstRequest = false;
                feature.MaxRequestBodySize = chunkedPayload.Length;

                do
                {
                    count = await context.Request.Body.ReadAsync(buffer, 0, 11);
                } while (count != 0);
            }
            else
            {
#pragma warning disable CS0618 // Type or member is obsolete
                requestRejectedEx = await Assert.ThrowsAsync<BadHttpRequestException>(async () =>
#pragma warning restore CS0618 // Type or member is obsolete
                {
                    do
                    {
                        count = await context.Request.Body.ReadAsync(buffer, 0, 11);
                    } while (count != 0);
                });

                throw requestRejectedEx;
            }
        },
        new TestServiceContext(LoggerFactory) { ServerOptions = { Limits = { MaxRequestBodySize = globalMaxRequestBodySize } } }))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Transfer-Encoding: chunked",
                    "",
                    chunkedPayload + "POST / HTTP/1.1",
                    "Host:",
                    "Transfer-Encoding: chunked",
                    "",
                    chunkedPayload);
                await connection.ReceiveEnd(
                    "HTTP/1.1 200 OK",
                    "Content-Length: 0",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "HTTP/1.1 413 Payload Too Large",
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }

        Assert.NotNull(requestRejectedEx);
        Assert.Equal(CoreStrings.FormatBadRequest_RequestBodyTooLarge(globalMaxRequestBodySize), requestRejectedEx.Message);
    }

    [Fact]
    public async Task EveryReadFailsWhenChunkedPayloadExceedsGlobalLimit()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        BadHttpRequestException requestRejectedEx1 = null;
        BadHttpRequestException requestRejectedEx2 = null;
#pragma warning restore CS0618 // Type or member is obsolete

        await using (var server = new TestServer(async context =>
        {
            var buffer = new byte[1];
#pragma warning disable CS0618 // Type or member is obsolete
            requestRejectedEx1 = await Assert.ThrowsAsync<BadHttpRequestException>(
            async () => await context.Request.Body.ReadAsync(buffer, 0, 1));
            requestRejectedEx2 = await Assert.ThrowsAsync<BadHttpRequestException>(
                async () => await context.Request.Body.ReadAsync(buffer, 0, 1));
#pragma warning restore CS0618 // Type or member is obsolete
            throw requestRejectedEx2;
        },
        new TestServiceContext(LoggerFactory) { ServerOptions = { Limits = { MaxRequestBodySize = 0 } } }))
        {
            using (var connection = server.CreateConnection())
            {
                await connection.Send(
                    "POST / HTTP/1.1",
                    "Host:",
                    "Transfer-Encoding: chunked",
                    "",
                    "1\r\n");
                await connection.ReceiveEnd(
                    "HTTP/1.1 413 Payload Too Large",
                    "Content-Length: 0",
                    "Connection: close",
                    $"Date: {server.Context.DateHeaderValue}",
                    "",
                    "");
            }
        }

        Assert.NotNull(requestRejectedEx1);
        Assert.NotNull(requestRejectedEx2);
        Assert.Equal(CoreStrings.FormatBadRequest_RequestBodyTooLarge(0), requestRejectedEx1.Message);
        Assert.Equal(CoreStrings.FormatBadRequest_RequestBodyTooLarge(0), requestRejectedEx2.Message);
    }
}
