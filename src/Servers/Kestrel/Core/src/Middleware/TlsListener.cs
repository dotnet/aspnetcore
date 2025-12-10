// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Middleware;

internal sealed class TlsListener
{
    private readonly Action<ConnectionContext, ReadOnlySequence<byte>> _tlsClientHelloBytesCallback;

    public TlsListener(Action<ConnectionContext, ReadOnlySequence<byte>> tlsClientHelloBytesCallback)
    {
        _tlsClientHelloBytesCallback = tlsClientHelloBytesCallback;
    }

    /// <summary>
    /// Sniffs the TLS Client Hello message, and invokes a callback if found.
    /// </summary>
    internal async Task OnTlsClientHelloAsync(ConnectionContext connection, CancellationToken cancellationToken)
    {
        var input = connection.Transport.Input;
        ClientHelloParseState parseState = ClientHelloParseState.NotEnoughData;
        short recordLength = -1; // remembers the length of TLS record to not re-parse header on every iteration

        while (true)
        {
            var result = await input.ReadAsync(cancellationToken);
            var buffer = result.Buffer;

            try
            {
                // If the buffer length is less than 6 bytes (handshake + version + length + client-hello byte)
                // and no more data is coming, we can't block in a loop here because we will not get more data
                if (result.IsCompleted && buffer.Length < 6)
                {
                    break;
                }

                parseState = TryParseClientHello(buffer, ref recordLength, out var clientHelloBytes);
                if (parseState == ClientHelloParseState.NotEnoughData)
                {
                    // if no data will be added, and we still lack enough bytes
                    // we can't block in a loop, so just exit
                    if (result.IsCompleted)
                    {
                        break;
                    }

                    continue;
                }

                if (parseState == ClientHelloParseState.ValidTlsClientHello)
                {
                    _tlsClientHelloBytesCallback(connection, clientHelloBytes);
                }

                Debug.Assert(parseState is ClientHelloParseState.ValidTlsClientHello or ClientHelloParseState.NotTlsClientHello);
                break; // We can continue with the middleware pipeline
            }
            finally
            {
                if (parseState is ClientHelloParseState.NotEnoughData)
                {
                    input.AdvanceTo(buffer.Start, buffer.End);
                }
                else
                {
                    // ready to continue middleware pipeline, reset the buffer to initial state
                    input.AdvanceTo(buffer.Start);
                }
            }
        }
    }

    /// <summary>
    /// RFCs
    /// ----
    /// TLS 1.1: https://datatracker.ietf.org/doc/html/rfc4346#section-6.2
    /// TLS 1.2: https://datatracker.ietf.org/doc/html/rfc5246#section-6.2
    /// TLS 1.3: https://datatracker.ietf.org/doc/html/rfc8446#section-5.1
    /// </summary>
    private static ClientHelloParseState TryParseClientHello(ReadOnlySequence<byte> buffer, ref short recordLength, out ReadOnlySequence<byte> clientHelloBytes)
    {
        clientHelloBytes = default;

        // in case bad actor will be sending a TLS client hello one byte at a time
        // and we know the expected length of TLS client hello,
        // we can check and fail quickly here instead of re-parsing the TLS client hello "header" on each iteration
        if (recordLength != -1 && buffer.Length < 5 + recordLength)
        {
            return ClientHelloParseState.NotEnoughData;
        }

        // this means we finally got a full tls record, so we can return without parsing again
        if (recordLength != -1)
        {
            clientHelloBytes = buffer.Slice(0, 5 + recordLength);
            return ClientHelloParseState.ValidTlsClientHello;
        }

        if (buffer.Length < 6)
        {
            return ClientHelloParseState.NotEnoughData;
        }

        var reader = new SequenceReader<byte>(buffer);

        // Content type must be 0x16 for TLS Handshake
        if (!reader.TryRead(out byte contentType) || contentType != 0x16)
        {
            return ClientHelloParseState.NotTlsClientHello;
        }

        // Protocol version
        if (!reader.TryReadBigEndian(out short version) || !IsValidProtocolVersion(version))
        {
            return ClientHelloParseState.NotTlsClientHello;
        }

        // Record length
        if (!reader.TryReadBigEndian(out recordLength))
        {
            return ClientHelloParseState.NotTlsClientHello;
        }

        // byte 6: handshake message type (must be 0x01 for ClientHello)
        if (!reader.TryRead(out byte handshakeType) || handshakeType != 0x01)
        {
            return ClientHelloParseState.NotTlsClientHello;
        }

        // 5 bytes are
        // 1) Handshake (1 byte)
        // 2) Protocol version (2 bytes)
        // 3) Record length (2 bytes)
        if (buffer.Length < 5 + recordLength)
        {
            return ClientHelloParseState.NotEnoughData;
        }

        clientHelloBytes = buffer.Slice(0, 5 + recordLength);
        return ClientHelloParseState.ValidTlsClientHello;
    }

    private static bool IsValidProtocolVersion(short version)
        => version is 0x0300  // SSL 3.0 (0x0300)
                   or 0x0301  // TLS 1.0 (0x0301)
                   or 0x0302  // TLS 1.1 (0x0302)
                   or 0x0303  // TLS 1.2 (0x0303)
                   or 0x0304; // TLS 1.3 (0x0304)

    private enum ClientHelloParseState : byte
    {
        NotEnoughData,
        NotTlsClientHello,
        ValidTlsClientHello
    }
}
