// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Core.Middleware;
using Microsoft.AspNetCore.Server.Kestrel.Core.Tests.TestHelpers;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public partial class TlsListenerMiddlewareTests
{
    [Theory]
    [MemberData(nameof(ValidClientHelloData))]
    public Task OnTlsClientHelloAsync_ValidData(int id, byte[] packetBytes, bool nextMiddlewareInvoked)
        => RunTlsClientHelloCallbackTest(id, packetBytes, nextMiddlewareInvoked, tlsClientHelloCallbackExpected: true);

    [Theory]
    [MemberData(nameof(InvalidClientHelloData))]
    public Task OnTlsClientHelloAsync_InvalidData(int id, byte[] packetBytes, bool nextMiddlewareInvoked)
        => RunTlsClientHelloCallbackTest(id, packetBytes, nextMiddlewareInvoked, tlsClientHelloCallbackExpected: false);

    [Theory]
    [MemberData(nameof(ValidClientHelloData_Segmented))]
    public Task OnTlsClientHelloAsync_ValidData_MultipleSegments(int id, List<byte[]> packets, bool nextMiddlewareInvoked)
        => RunTlsClientHelloCallbackTest_WithMultipleSegments(id, packets, nextMiddlewareInvoked, tlsClientHelloCallbackExpected: true);

    [Theory]
    [MemberData(nameof(InvalidClientHelloData_Segmented))]
    public Task OnTlsClientHelloAsync_InvalidData_MultipleSegments(int id, List<byte[]> packets, bool nextMiddlewareInvoked)
        => RunTlsClientHelloCallbackTest_WithMultipleSegments(id, packets, nextMiddlewareInvoked, tlsClientHelloCallbackExpected: false);

    [Fact]
    public async Task RunTlsClientHelloCallbackTest_DeterministinglyReads()
    {
        var serviceContext = new TestServiceContext();

        var pipe = new Pipe();
        var writer = pipe.Writer;
        var reader = new ObservablePipeReader(pipe.Reader);

        var transport = new DuplexPipe(reader, writer);
        var transportConnection = new DefaultConnectionContext("test", transport, transport);

        var nextMiddlewareInvoked = false;
        var tlsClientHelloCallbackInvoked = false;

        var middleware = new TlsListenerMiddleware(
            next: ctx =>
            {
                nextMiddlewareInvoked = true;
                var readResult = ctx.Transport.Input.ReadAsync();
                Assert.Equal(5, readResult.Result.Buffer.Length);

                return Task.CompletedTask;
            },
            tlsClientHelloBytesCallback: (ctx, data) =>
            {
                tlsClientHelloCallbackInvoked = true;
            }
        );

        await writer.WriteAsync(new byte[1] { 0x16 });
        var middlewareTask = Task.Run(() => middleware.OnTlsClientHelloAsync(transportConnection));
        await writer.WriteAsync(new byte[2] { 0x03, 0x01 });
        await writer.WriteAsync(new byte[2] { 0x00, 0x20 });
        await writer.CompleteAsync();

        await middlewareTask;
        Assert.True(nextMiddlewareInvoked);
        Assert.False(tlsClientHelloCallbackInvoked);

        // ensuring that we have read limited number of times
        Assert.True(reader.ReadAsyncCounter is >= 2 && reader.ReadAsyncCounter is <= 5,
            $"Expected ReadAsync() to happen about 2-5 times. Actually happened {reader.ReadAsyncCounter} times.");
    }

    private async Task RunTlsClientHelloCallbackTest_WithMultipleSegments(
        int id,
        List<byte[]> packets,
        bool nextMiddlewareInvokedExpected,
        bool tlsClientHelloCallbackExpected)
    {
        var pipe = new Pipe();
        var writer = pipe.Writer;
        var reader = new ObservablePipeReader(pipe.Reader);

        var transport = new DuplexPipe(reader, writer);
        var transportConnection = new DefaultConnectionContext("test", transport, transport);

        var nextMiddlewareInvokedActual = false;
        var tlsClientHelloCallbackActual = false;

        var fullLength = packets.Sum(p => p.Length);

        var middleware = new TlsListenerMiddleware(
            next: ctx =>
            {
                nextMiddlewareInvokedActual = true;
                return Task.CompletedTask;
            },
            tlsClientHelloBytesCallback: (ctx, data) =>
            {
                tlsClientHelloCallbackActual = true;

                Assert.NotNull(ctx);
                Assert.False(data.IsEmpty);
                Assert.Equal(fullLength, data.Length);
            }
        );

        // write first packet
        await writer.WriteAsync(packets[0]);
        var middlewareTask = Task.Run(() => middleware.OnTlsClientHelloAsync(transportConnection));

        var random = new Random();
        await Task.Delay(millisecondsDelay: random.Next(25, 75));

        // write all next packets
        foreach (var packet in packets.Skip(1))
        {
            await writer.WriteAsync(packet);
            await Task.Delay(millisecondsDelay: random.Next(25, 75));
        }
        await writer.CompleteAsync();
        await middlewareTask;

        Assert.Equal(nextMiddlewareInvokedExpected, nextMiddlewareInvokedActual);
        Assert.Equal(tlsClientHelloCallbackExpected, tlsClientHelloCallbackActual);
    }

    private async Task RunTlsClientHelloCallbackTest(
        int id,
        byte[] packetBytes,
        bool nextMiddlewareExpected,
        bool tlsClientHelloCallbackExpected)
    {
        var pipe = new Pipe();
        var writer = pipe.Writer;
        var reader = new ObservablePipeReader(pipe.Reader);

        var transport = new DuplexPipe(reader, writer);
        var transportConnection = new DefaultConnectionContext("test", transport, transport);

        var nextMiddlewareInvokedActual = false;
        var tlsClientHelloCallbackActual = false;

        var middleware = new TlsListenerMiddleware(
            next: ctx =>
            {
                nextMiddlewareInvokedActual = true;
                var readResult = ctx.Transport.Input.ReadAsync();
                Assert.Equal(packetBytes.Length, readResult.Result.Buffer.Length);

                return Task.CompletedTask;
            },
            tlsClientHelloBytesCallback: (ctx, data) =>
            {
                tlsClientHelloCallbackActual = true;

                Assert.NotNull(ctx);
                Assert.False(data.IsEmpty);
                Assert.Equal(packetBytes.Length, data.Length);
            }
        );

        await writer.WriteAsync(packetBytes);
        await writer.CompleteAsync();

        // call middleware and expect a callback
        await middleware.OnTlsClientHelloAsync(transportConnection);

        Assert.Equal(nextMiddlewareExpected, nextMiddlewareInvokedActual);
        Assert.Equal(tlsClientHelloCallbackExpected, tlsClientHelloCallbackActual);
    }

    public static IEnumerable<object[]> ValidClientHelloData()
    {
        int id = 0;
        foreach (var clientHello in valid_collection)
        {
            yield return new object[] { id++, clientHello, true /* invokes next middleware */ };
        }
    }

    public static IEnumerable<object[]> InvalidClientHelloData()
    {
        int id = 0;
        foreach (byte[] clientHello in invalid_collection)
        {
            yield return new object[] { id++, clientHello, true /* invokes next middleware */ };
        }
    }

    public static IEnumerable<object[]> ValidClientHelloData_Segmented()
    {
        int id = 0;
        foreach (var clientHello in valid_collection)
        {
            var clientHelloSegments = new List<byte[]>
            {
                clientHello.Take(1).ToArray(),
                clientHello.Skip(1).Take(2).ToArray(),
                clientHello.Skip(3).Take(2).ToArray(),
                clientHello.Skip(5).Take(1).ToArray(),
                clientHello.Skip(6).Take(clientHello.Length - 6).ToArray()
            };

            yield return new object[] { id++, clientHelloSegments, true /* invokes next middleware */ };
        }
    }

    public static IEnumerable<object[]> InvalidClientHelloData_Segmented()
    {
        int id = 0;
        foreach (var clientHello in invalid_collection)
        {
            var clientHelloSegments = new List<byte[]>();
            if (clientHello.Length >= 1)
            {
                clientHelloSegments.Add(clientHello.Take(1).ToArray());
            }
            if (clientHello.Length >= 3)
            {
                clientHelloSegments.Add(clientHello.Skip(1).Take(2).ToArray());
            }
            if (clientHello.Length >= 5)
            {
                clientHelloSegments.Add(clientHello.Skip(3).Take(2).ToArray());
            }
            if (clientHello.Length >= 6)
            {
                clientHelloSegments.Add(clientHello.Skip(5).Take(1).ToArray());
            }
            if (clientHello.Length >= 7)
            {
                clientHelloSegments.Add(clientHello.Skip(6).Take(clientHello.Length - 6).ToArray());
            }

            yield return new object[] { id++, clientHelloSegments, true /* invokes next middleware */ };
        }
    }

    private static byte[] valid_clientHelloHeader =
    {
        // 0x16 = Handshake
        0x16,
        // 0x0301 = TLS 1.0
        0x03, 0x01,
        // length = 0x0020 (32 bytes)
        0x00, 0x20,
        // Handshake.msg_type (client hello)
        0x01,
        // 31 bytes (zeros for simplicity)
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        0
    };

    private static byte[] valid_ClientHelloStandard =
    {
        // SslPlainText.(ContentType+ProtocolVersion)
        0x16, 0x03, 0x03,
        // SslPlainText.length
        0x00, 0xCB,
        // Handshake.msg_type (client hello)
        0x01,
        // Handshake.length
        0x00, 0x00, 0xC7,
        // ClientHello.client_version
        0x03, 0x03,
        // ClientHello.random
        0x0C, 0x3C, 0x85, 0x78, 0xCA,
        0x67, 0x70, 0xAA, 0x38, 0xCB,
        0x28, 0xBC, 0xDC, 0x3E, 0x30,
        0xBF, 0x11, 0x96, 0x95, 0x1A,
        0xB9, 0xF0, 0x99, 0xA4, 0x91,
        0x09, 0x13, 0xB4, 0x89, 0x94,
        0x27, 0x2E,
        // ClientHello.SessionId
        0x00,
        // ClientHello.cipher_suites
        0x00, 0x2A, 0xC0, 0x2C, 0xC0,
        0x2B, 0xC0, 0x30, 0xC0, 0x2F,
        0x00, 0x9F, 0x00, 0x9E, 0xC0,
        0x24, 0xC0, 0x23, 0xC0, 0x28,
        0xC0, 0x27, 0xC0, 0x0A, 0xC0,
        0x09, 0xC0, 0x14, 0xC0, 0x13,
        0x00, 0x9D, 0x00, 0x9C, 0x00,
        0x3D, 0x00, 0x3C, 0x00, 0x35,
        0x00, 0x2F, 0x00, 0x0A,
        // ClientHello.compression_methods
        0x01, 0x01,
        // ClientHello.extension_list_length
        0x00, 0x74,
        // Extension.extension_type (server_name)
        0x00, 0x00,
        // ServerNameListExtension.length
        0x00, 0x39,
        // ServerName.length
        0x00, 0x37,
        // ServerName.type
        0x00,
        // HostName.length
        0x00, 0x34,
        // HostName.bytes
        0x61, 0x61, 0x61, 0x61, 0x61,
        0x61, 0x61, 0x61, 0x61, 0x61,
        0x61, 0x61, 0x61, 0x61, 0x61,
        0x61, 0x61, 0x61, 0x61, 0x61,
        0x61, 0x61, 0x61, 0x61, 0x61,
        0x61, 0x61, 0x61, 0x61, 0x61,
        0x61, 0x61, 0x61, 0x61, 0x61,
        0x61, 0x61, 0x61, 0x61, 0x61,
        0x61, 0x61, 0x61, 0x61, 0x61,
        0x61, 0x61, 0x61, 0x61, 0x61,
        0x61, 0x61,
        // Extension.extension_type (00 0A)
        0x00, 0x0A,
        // Extension 0A
        0x00, 0x08, 0x00, 0x06, 0x00,
        0x1D, 0x00, 0x17, 0x00, 0x18,
        // Extension.extension_type (00 0B)
        0x00, 0x0B,
        // Extension 0B
        0x00, 0x02, 0x01, 0x00,
        // Extension.extension_type (00 0D)
        0x00, 0x0D,
        // Extension 0D
        0x00, 0x14, 0x00, 0x12, 0x04,
        0x01, 0x05, 0x01, 0x02, 0x01,
        0x04, 0x03, 0x05, 0x03, 0x02,
        0x03, 0x02, 0x02, 0x06, 0x01,
        0x06, 0x03,
        // Extension.extension_type (00 23)
        0x00, 0x23,
        // Extension 00 23
        0x00, 0x00,
        // Extension.extension_type (00 17)
        0x00, 0x17,
        // Extension 17
        0x00, 0x00,
        // Extension.extension_type (FF 01)
        0xFF, 0x01,
        // Extension FF01
        0x00, 0x01, 0x00
    };

    private static byte[] valid_Tls12ClientHello =
    {
        // SslPlainText.(ContentType+ProtocolVersion)
        0x16, 0x03, 0x01,
        // SslPlainText.length
        0x00, 0xD1,
        // Handshake.msg_type (client hello)
        0x01,
        // Handshake.length
        0x00, 0x00, 0xCD,
        // ClientHello.client_version
        0x03, 0x03,
        // ClientHello.random
        0x0C, 0x3C, 0x85, 0x78, 0xCA,
        0x67, 0x70, 0xAA, 0x38, 0xCB,
        0x28, 0xBC, 0xDC, 0x3E, 0x30,
        0xBF, 0x11, 0x96, 0x95, 0x1A,
        0xB9, 0xF0, 0x99, 0xA4, 0x91,
        0x09, 0x13, 0xB4, 0x89, 0x94,
        0x27, 0x2E,
        // ClientHello.SessionId
        0x00,
        // ClientHello.cipher_suites_length
        0x00, 0x5C,
        // ClientHello.cipher_suites
        0xC0, 0x30, 0xC0, 0x2C, 0xC0, 0x28, 0xC0, 0x24,
        0xC0, 0x14, 0xC0, 0x0A, 0x00, 0x9f, 0x00, 0x6B,
        0x00, 0x39, 0xCC, 0xA9, 0xCC, 0xA8, 0xCC, 0xAA,
        0xFF, 0x85, 0x00, 0xC4, 0x00, 0x88, 0x00, 0x81,
        0x00, 0x9D, 0x00, 0x3D, 0x00, 0x35, 0x00, 0xC0,
        0x00, 0x84, 0xC0, 0x2f, 0xC0, 0x2B, 0xC0, 0x27,
        0xC0, 0x23, 0xC0, 0x13, 0xC0, 0x09, 0x00, 0x9E,
        0x00, 0x67, 0x00, 0x33, 0x00, 0xBE, 0x00, 0x45,
        0x00, 0x9C, 0x00, 0x3C, 0x00, 0x2F, 0x00, 0xBA,
        0x00, 0x41, 0xC0, 0x11, 0xC0, 0x07, 0x00, 0x05,
        0x00, 0x04, 0xC0, 0x12, 0xC0, 0x08, 0x00, 0x16,
        0x00, 0x0a, 0x00, 0xff,
        // ClientHello.compression_methods
        0x01, 0x01,
        // ClientHello.extension_list_length
        0x00, 0x48,
        // Extension.extension_type (ec_point_formats)
        0x00, 0x0b, 0x00, 0x02, 0x01, 0x00,
        // Extension.extension_type (supported_groups)
        0x00, 0x0A, 0x00, 0x08, 0x00, 0x06, 0x00, 0x1D,
        0x00, 0x17, 0x00, 0x18,
        // Extension.extension_type (session_ticket)
        0x00, 0x23, 0x00, 0x00,
        // Extension.extension_type (signature_algorithms)
        0x00, 0x0D, 0x00, 0x1C, 0x00, 0x1A, 0x06, 0x01,
        0x06, 0x03, 0xEF, 0xEF, 0x05, 0x01, 0x05, 0x03,
        0x04, 0x01, 0x04, 0x03, 0xEE, 0xEE, 0xED, 0xED,
        0x03, 0x01, 0x03, 0x03, 0x02, 0x01, 0x02, 0x03,
        // Extension.extension_type (application_level_Protocol)
        0x00, 0x10, 0x00, 0x0e, 0x00, 0x0C, 0x02, 0x68,
        0x32, 0x08, 0x68, 0x74, 0x74, 0x70, 0x2F, 0x31,
        0x2E, 0x31
    };

    private static byte[] valid_Tls13ClientHello =
    {
        // SslPlainText.(ContentType+ProtocolVersion)
        0x16, 0x03, 0x01,
        // SslPlainText.length
        0x01, 0x08,
        // Handshake.msg_type (client hello)
        0x01,
        // Handshake.length
        0x00, 0x01, 0x04,
        // ClientHello.client_version
        0x03, 0x03,
        // ClientHello.random
        0x0C, 0x3C, 0x85, 0x78, 0xCA, 0x67, 0x70, 0xAA,
        0x38, 0xCB, 0x28, 0xBC, 0xDC, 0x3E, 0x30, 0xBF,
        0x11, 0x96, 0x95, 0x1A, 0xB9, 0xF0, 0x99, 0xA4,
        0x91, 0x09, 0x13, 0xB4, 0x89, 0x94, 0x27, 0x2E,
        // ClientHello.SessionId_Length
        0x20,
        // ClientHello.SessionId
        0x0C, 0x3C, 0x85, 0x78, 0xCA, 0x67, 0x70, 0xAA,
        0x38, 0xCB, 0x28, 0xBC, 0xDC, 0x3E, 0x30, 0xBF,
        0x11, 0x96, 0x95, 0x1A, 0xB9, 0xF0, 0x99, 0xA4,
        0x91, 0x09, 0x13, 0xB4, 0x89, 0x94, 0x27, 0x2E,
        // ClientHello.cipher_suites_length
        0x00, 0x0C,
        // ClientHello.cipher_suites
        0x13, 0x02, 0x13, 0x03, 0x13, 0x01, 0xC0, 0x14,
        0xc0, 0x30, 0x00, 0xFF,
        // ClientHello.compression_methods
        0x01, 0x00,
        // ClientHello.extension_list_length
        0x00, 0xAF,
        // Extension.extension_type (server_name) (10.211.55.2)
        0x00, 0x00, 0x00, 0x10, 0x00, 0x0e, 0x00, 0x00,
        0x0B, 0x31, 0x30, 0x2E, 0x32, 0x31, 0x31, 0x2E,
        0x35, 0x35, 0x2E, 0x32,
        // Extension.extension_type (ec_point_formats)
        0x00, 0x0B, 0x00, 0x04, 0x03, 0x00, 0x01, 0x02,
        // Extension.extension_type (supported_groups)
        0x00, 0x0A, 0x00, 0x0C, 0x00, 0x0A, 0x00, 0x1D,
        0x00, 0x17, 0x00, 0x1E, 0x00, 0x19, 0x00, 0x18,
        // Extension.extension_type (application_level_Protocol) (boo)
        0x00, 0x10, 0x00, 0x06, 0x00, 0x04, 0x03, 0x62,
        0x6f, 0x6f,
        // Extension.extension_type (encrypt_then_mac)
        0x00, 0x16, 0x00, 0x00,
        // Extension.extension_type (extended_master_key_secret)
        0x00, 0x17, 0x00, 0x00,
        // Extension.extension_type (signature_algorithms)
        0x00, 0x0D, 0x00, 0x30, 0x00, 0x2E,
        0x06, 0x03, 0xEF, 0xEF, 0x05, 0x01, 0x05, 0x03,
        0x06, 0x03, 0xEF, 0xEF, 0x05, 0x01, 0x05, 0x03,
        0x06, 0x03, 0xEF, 0xEF, 0x05, 0x01, 0x05, 0x03,
        0x04, 0x01, 0x04, 0x03, 0xEE, 0xEE, 0xED, 0xED,
        0x03, 0x01, 0x03, 0x03, 0x02, 0x01, 0x02, 0x03,
        0x03, 0x01, 0x03, 0x03, 0x02, 0x01,
        // Extension.extension_type (supported_versions)
        0x00, 0x2B, 0x00, 0x09, 0x08, 0x03, 0x04, 0x03,
        0x03, 0x03, 0x02, 0x03, 0x01,
        // Extension.extension_type (psk_key_exchange_modes)
        0x00, 0x2D, 0x00, 0x02, 0x01, 0x01,
        // Extension.extension_type (key_share)
        0x00, 0x33, 0x00, 0x26, 0x00, 0x24, 0x00, 0x1D,
        0x00, 0x20,
        0x04, 0x01, 0x04, 0x03, 0xEE, 0xEE, 0xED, 0xED,
        0x03, 0x01, 0x03, 0x03, 0x02, 0x01, 0x02, 0x03,
        0x04, 0x01, 0x04, 0x03, 0xEE, 0xEE, 0xED, 0xED,
        0x03, 0x01, 0x03, 0x03, 0x02, 0x01, 0x02, 0x03
    };

    private static byte[] valid_TlsClientHelloNoExtensions =
    {
        0x16, 0x03, 0x03, 0x00, 0x39, 0x01, 0x00, 0x00,
        0x35, 0x03, 0x03, 0x62, 0x5d, 0x50, 0x2a, 0x41,
        0x2f, 0xd8, 0xc3, 0x65, 0x35, 0xea, 0x01, 0x70,
        0x03, 0x7e, 0x7e, 0x2d, 0xd4, 0xfe, 0x93, 0x39,
        0xa4, 0x04, 0x66, 0xbb, 0x46, 0x91, 0x41, 0xc3,
        0x48, 0x87, 0x3d, 0x00, 0x00, 0x0e, 0x00, 0x3d,
        0x00, 0x3c, 0x00, 0x0a, 0x00, 0x35, 0x00, 0x2f,
        0x00, 0x05, 0x00, 0x04, 0x01, 0x00
    };

    private static byte[] invalid_TlsClientHelloHeader =
    {
        // Handshake - incorrect
        0x01,
        // ProtocolVersion
        0x03, 0x04,
        // SslPlainText.length
        0x00, 0xCB,
        // Handshake.msg_type (client hello)
        0x01,
        // Handshake.length
        0x00, 0x00, 0xC7,
    };

    private static byte[] invalid_3BytesMessage =
    {
        // Handshake
        0x016,
        // Protocol Version
        0x03, 0x01,
        // not enough data - so incorrect
    };

    private static byte[] invalid_9BytesMessage =
    {
        // 0x16 = Handshake
        0x16,
        // 0x0301 = TLS 1.0
        0x03, 0x01,
        // length = 0x0020 (32 bytes)
        0x00, 0x20,
        // Handshake.msg_type (client hello)
        0x01,
        // should have 31 bytes (zeros for simplicity)
        0, 0, 0
        // no other data here - incorrect
    };

    private static byte[] invalid_UnknownProtocolVersion1 =
    {
        // Handshake
        0x016,
        // ProtocolVersion - incorrect
        0x02, 0x05,
        // SslPlainText.length
        0x00, 0xCB,
        // Handshake.msg_type (client hello)
        0x01,
        // Handshake.length
        0x00, 0x00, 0xC7,
    };

    private static byte[] invalid_UnknownProtocolVersion2 =
    {
        // Handshake
        0x016,
        // ProtocolVersion - incorrect
        0x02, 0x01,
        // SslPlainText.length
        0x00, 0xCB,
        // Handshake.msg_type (client hello)
        0x01,
        // Handshake.length
        0x00, 0x00, 0xC7,
    };

    private static byte[] invalid_IncorrectHandshakeMessageType =
    {
        // Handshake
        0x016,
        // ProtocolVersion
        0x02, 0x00,
        // SslPlainText.length
        0x00, 0xCB,
        // Handshake.msg_type (client hello) - incorrect
        0x02,
        // Handshake.length
        0x00, 0x00, 0xC7,
    };

    private static List<byte[]> valid_collection = new List<byte[]>()
    {
        valid_clientHelloHeader, valid_ClientHelloStandard, valid_Tls12ClientHello, valid_Tls13ClientHello, valid_TlsClientHelloNoExtensions
    };

    private static List<byte[]> invalid_collection = new List<byte[]>()
    {
        invalid_TlsClientHelloHeader, invalid_3BytesMessage, invalid_9BytesMessage,
        invalid_UnknownProtocolVersion1, invalid_UnknownProtocolVersion2, invalid_IncorrectHandshakeMessageType
    };
}
