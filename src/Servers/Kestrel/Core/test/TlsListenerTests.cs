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
using Microsoft.VisualStudio.TestPlatform;
using Moq;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Tests;

public class TlsListenerTests
{
    [Theory]
    [MemberData(nameof(ValidClientHelloData))]
    public Task OnTlsClientHelloAsync_ValidData(int id, byte[] packetBytes)
        => RunTlsClientHelloCallbackTest(id, packetBytes, tlsClientHelloCallbackExpected: true);

    [Theory]
    [MemberData(nameof(InvalidClientHelloData))]
    public Task OnTlsClientHelloAsync_InvalidData(int id, byte[] packetBytes)
        => RunTlsClientHelloCallbackTest(id, packetBytes, tlsClientHelloCallbackExpected: false);

    [Theory]
    [MemberData(nameof(ValidClientHelloData_Segmented))]
    public Task OnTlsClientHelloAsync_ValidData_MultipleSegments(int id, List<byte[]> packets)
        => RunTlsClientHelloCallbackTest_WithMultipleSegments(id, packets, tlsClientHelloCallbackExpected: true);

    [Theory]
    [MemberData(nameof(InvalidClientHelloData_Segmented))]
    public Task OnTlsClientHelloAsync_InvalidData_MultipleSegments(int id, List<byte[]> packets)
        => RunTlsClientHelloCallbackTest_WithMultipleSegments(id, packets, tlsClientHelloCallbackExpected: false);

    [Fact]
    public async Task RunTlsClientHelloCallbackTest_WithExtraShortLastingToken()
    {
        var serviceContext = new TestServiceContext();

        var pipe = new Pipe();
        var writer = pipe.Writer;
        var reader = new ObservablePipeReader(pipe.Reader);

        var transport = new DuplexPipe(reader, writer);
        var transportConnection = new DefaultConnectionContext("test", transport, transport);

        var tlsClientHelloCallbackInvoked = false;
        var listener = new TlsListener((ctx, data) => { tlsClientHelloCallbackInvoked = true; });

        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(3));

        await writer.WriteAsync(new byte[1] { 0x16 });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => listener.OnTlsClientHelloAsync(transportConnection, cts.Token));
        Assert.False(tlsClientHelloCallbackInvoked);
    }

    [Fact]
    public async Task RunTlsClientHelloCallbackTest_WithPreCanceledToken()
    {
        var serviceContext = new TestServiceContext();

        var pipe = new Pipe();
        var writer = pipe.Writer;
        var reader = new ObservablePipeReader(pipe.Reader);

        var transport = new DuplexPipe(reader, writer);
        var transportConnection = new DefaultConnectionContext("test", transport, transport);

        var tlsClientHelloCallbackInvoked = false;
        var listener = new TlsListener((ctx, data) => { tlsClientHelloCallbackInvoked = true; });

        var cts = new CancellationTokenSource();
        cts.Cancel();

        await writer.WriteAsync(new byte[1] { 0x16 });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => listener.OnTlsClientHelloAsync(transportConnection, cts.Token));
        Assert.False(tlsClientHelloCallbackInvoked);
    }

    [Fact]
    public async Task RunTlsClientHelloCallbackTest_WithPendingCancellation()
    {
        var serviceContext = new TestServiceContext();

        var pipe = new Pipe();
        var writer = pipe.Writer;
        var reader = new ObservablePipeReader(pipe.Reader);

        var transport = new DuplexPipe(reader, writer);
        var transportConnection = new DefaultConnectionContext("test", transport, transport);

        var tlsClientHelloCallbackInvoked = false;
        var listener = new TlsListener((ctx, data) => { tlsClientHelloCallbackInvoked = true; });

        var cts = new CancellationTokenSource();
        await writer.WriteAsync(new byte[1] { 0x16 });
        var listenerTask = listener.OnTlsClientHelloAsync(transportConnection, cts.Token);
        await writer.WriteAsync(new byte[2] { 0x03, 0x01 });
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => listenerTask);
        Assert.False(tlsClientHelloCallbackInvoked);
    }

    [Fact]
    public async Task RunTlsClientHelloCallbackTest_DeterministicallyReads()
    {
        /* Current test ensures that we read the input stream only a limited number of times.
         * It is a guard against incorrect transport.AdvanceTo() usage leading to infinite loop / more reads than should happen.
         */

        var serviceContext = new TestServiceContext();

        var pipe = new Pipe();
        var writer = pipe.Writer;
        var reader = new ObservablePipeReader(pipe.Reader);

        var transport = new DuplexPipe(reader, writer);
        var transportConnection = new DefaultConnectionContext("test", transport, transport);

        var tlsClientHelloCallbackInvoked = false;
        var listener = new TlsListener((ctx, data) => { tlsClientHelloCallbackInvoked = true; });

        await writer.WriteAsync(new byte[1] { 0x16 });
        var listenerTask = listener.OnTlsClientHelloAsync(transportConnection, CancellationToken.None);
        await writer.WriteAsync(new byte[2] { 0x03, 0x01 });
        await writer.WriteAsync(new byte[2] { 0x00, 0x20 });
        await writer.CompleteAsync();

        await listenerTask;
        Assert.False(tlsClientHelloCallbackInvoked);

        var readResult = await reader.ReadAsync();
        Assert.Equal(5, readResult.Buffer.Length);

        // ensuring that we have read limited number of times
        Assert.True(reader.ReadAsyncCounter is >= 2 && reader.ReadAsyncCounter is <= 5,
            $"Expected ReadAsync() to happen about 2-5 times. Actually happened {reader.ReadAsyncCounter} times.");
    }

    private async Task RunTlsClientHelloCallbackTest_WithMultipleSegments(
        int id,
        List<byte[]> packets,
        bool tlsClientHelloCallbackExpected)
    {
        var pipe = new Pipe();
        var writer = pipe.Writer;

        var transport = new DuplexPipe(pipe.Reader, writer);
        var transportConnection = new DefaultConnectionContext("test", transport, transport);

        var tlsClientHelloCallbackActual = false;

        var fullLength = packets.Sum(p => p.Length);

        var listener = new TlsListener(
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
        var listenerTask = listener.OnTlsClientHelloAsync(transportConnection, CancellationToken.None);

        /* It is a race condition (middleware's loop and writes here).
         * We don't know specifically how many packets will be read by middleware's loop
         * (possibly there are even 2 packets - the first and all others combined).
         * The goal here is to try simulate multi-segmented approach and test more cases
         */

        // write all other packets
        foreach (var packet in packets.Skip(1))
        {
            await writer.WriteAsync(packet);
        }
        await writer.CompleteAsync();
        await listenerTask;

        Assert.Equal(tlsClientHelloCallbackExpected, tlsClientHelloCallbackActual);

        if (tlsClientHelloCallbackActual)
        {
            var readResult = await pipe.Reader.ReadAsync();
            Assert.Equal(fullLength, readResult.Buffer.Length);
        }
    }

    private async Task RunTlsClientHelloCallbackTest(
        int id,
        byte[] packetBytes,
        bool tlsClientHelloCallbackExpected)
    {
        var pipe = new Pipe();
        var writer = pipe.Writer;

        var transport = new DuplexPipe(pipe.Reader, writer);
        var transportConnection = new DefaultConnectionContext("test", transport, transport);

        var tlsClientHelloCallbackActual = false;

        var listener = new TlsListener(
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
        await listener.OnTlsClientHelloAsync(transportConnection, CancellationToken.None);

        Assert.Equal(tlsClientHelloCallbackExpected, tlsClientHelloCallbackActual);

        var readResult = await pipe.Reader.ReadAsync();
        Assert.Equal(packetBytes.Length, readResult.Buffer.Length);
    }

    public static IEnumerable<object[]> ValidClientHelloData()
    {
        int id = 0;
        foreach (var clientHello in _validCollection)
        {
            yield return new object[] { id++, clientHello };
        }
    }

    public static IEnumerable<object[]> InvalidClientHelloData()
    {
        int id = 0;
        foreach (byte[] clientHello in _invalidCollection)
        {
            yield return new object[] { id++, clientHello };
        }
    }

    public static IEnumerable<object[]> ValidClientHelloData_Segmented()
    {
        int id = 0;
        foreach (var clientHello in _validCollection)
        {
            var clientHelloSegments = new List<byte[]>
            {
                clientHello.Take(1).ToArray(),
                clientHello.Skip(1).Take(2).ToArray(),
                clientHello.Skip(3).Take(2).ToArray(),
                clientHello.Skip(5).Take(1).ToArray(),
                clientHello.Skip(6).Take(clientHello.Length - 6).ToArray()
            };

            yield return new object[] { id++, clientHelloSegments };
        }
    }

    public static IEnumerable<object[]> InvalidClientHelloData_Segmented()
    {
        int id = 0;
        foreach (var clientHello in _invalidCollection)
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

            yield return new object[] { id++, clientHelloSegments };
        }
    }

    private static byte[] _validClientHelloHeader =
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

    private static byte[] _validSsl3ClientHello =
    {
        0x16, 0x03, 0x00,             // ContentType: Handshake, Version: SSL 3.0
        0x00, 0x2F,                   // Length: 47 bytes
        0x01,                         // Handshake Type: ClientHello
        0x00, 0x00, 0x2B,             // Length: 43 bytes
        0x03, 0x00,                   // Client Version: SSL 3.0
        // Random (32 bytes)
        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
        0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
        0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
        0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
        0x00,                         // Session ID Length
        0x00, 0x04,                   // Cipher Suites Length
        0x00, 0x2F, 0x00, 0x35,       // Cipher Suites
        0x01, 0x00                    // Compression Methods: null
    };

    private static byte[] _validTls10ClientHello =
    {
        0x16, 0x03, 0x01,             // ContentType: Handshake, Version: TLS 1.0
        0x00, 0x2F,                   // Length: 47 bytes
        0x01,                         // Handshake Type: ClientHello
        0x00, 0x00, 0x2B,             // Length: 43 bytes
        0x03, 0x01,                   // Client Version: TLS 1.0
        // Random (32 bytes)
        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
        0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
        0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
        0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
        0x00,                         // Session ID Length
        0x00, 0x04,                   // Cipher Suites Length
        0x00, 0x2F, 0x00, 0x35,       // Cipher Suites
        0x01, 0x00                    // Compression Methods: null
    };

    private static byte[] _validTls11ClientHello =
    {
        0x16, 0x03, 0x02,             // ContentType: Handshake, Version: TLS 1.1
        0x00, 0x2F,                   // Length: 47 bytes
        0x01,                         // Handshake Type: ClientHello
        0x00, 0x00, 0x2B,             // Length: 43 bytes
        0x03, 0x02,                   // Client Version: TLS 1.1
        // Random (32 bytes)
        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
        0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
        0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,
        0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
        0x00,                         // Session ID Length
        0x00, 0x04,                   // Cipher Suites Length
        0x00, 0x2F, 0x00, 0x35,       // Cipher Suites: TLS_RSA_WITH_AES_128_CBC_SHA, TLS_RSA_WITH_AES_256_CBC_SHA
        0x01, 0x00                    // Compression Methods: null
    };

    private static byte[] _validTls12ClientHello =
    {
        // SslPlainText.(ContentType+ProtocolVersion)
        0x16, 0x03, 0x03,
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

    private static byte[] _validTls13ClientHello =
    {
        // SslPlainText.(ContentType+ProtocolVersion)
        0x16, 0x03, 0x04,
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

    private static byte[] _validTlsClientHelloNoExtensions =
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

    private static byte[] _invalidTlsClientHelloHeader =
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

    private static byte[] _invalid3BytesMessage =
    {
        // Handshake
        0x016,
        // Protocol Version
        0x03, 0x01,
        // not enough data - so incorrect
    };

    private static byte[] _invalid9BytesMessage =
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

    private static byte[] _invalidUnknownProtocolVersion1 =
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

    private static byte[] _invalidUnknownProtocolVersion2 =
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

    private static byte[] _invalidIncorrectHandshakeMessageType =
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

    private static List<byte[]> _validCollection = new List<byte[]>()
    {
        _validClientHelloHeader, _validSsl3ClientHello, _validTls10ClientHello,
        _validTls11ClientHello, _validTls12ClientHello, _validTls13ClientHello,
        _validTlsClientHelloNoExtensions
    };

    private static List<byte[]> _invalidCollection = new List<byte[]>()
    {
        _invalidTlsClientHelloHeader, _invalid3BytesMessage, _invalid9BytesMessage,
        _invalidUnknownProtocolVersion1, _invalidUnknownProtocolVersion2, _invalidIncorrectHandshakeMessageType
    };
}
