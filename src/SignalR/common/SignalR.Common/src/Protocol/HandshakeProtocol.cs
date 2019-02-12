// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Internal;

namespace Microsoft.AspNetCore.SignalR.Protocol
{
    /// <summary>
    /// A helper class for working with SignalR handshakes.
    /// </summary>
    public static class HandshakeProtocol
    {
        // Use C#7.3's ReadOnlySpan<byte> optimization for static data https://vcsjones.com/2019/02/01/csharp-readonly-span-bytes-static/
        private const string ProtocolPropertyName = "protocol";
        private static ReadOnlySpan<byte> ProtocolPropertyNameBytes => new byte[] { (byte)'p', (byte)'r', (byte)'o', (byte)'t', (byte)'o', (byte)'c', (byte)'o', (byte)'l' };
        private const string ProtocolVersionPropertyName = "version";
        private static ReadOnlySpan<byte> ProtocolVersionPropertyNameBytes => new byte[] { (byte)'v', (byte)'e', (byte)'r', (byte)'s', (byte)'i', (byte)'o', (byte)'n' };
        private const string MinorVersionPropertyName = "minorVersion";
        private static ReadOnlySpan<byte> MinorVersionPropertyNameBytes => new byte[] { (byte)'m', (byte)'i', (byte)'n', (byte)'o', (byte)'r', (byte)'V', (byte)'e', (byte)'r', (byte)'s', (byte)'i', (byte)'o', (byte)'n' };
        private const string ErrorPropertyName = "error";
        private static ReadOnlySpan<byte> ErrorPropertyNameBytes => new byte[] { (byte)'e', (byte)'r', (byte)'r', (byte)'o', (byte)'r' };
        private const string TypePropertyName = "type";
        private static ReadOnlySpan<byte> TypePropertyNameBytes => new byte[] { (byte)'t', (byte)'y', (byte)'p', (byte)'e' };

        private static ConcurrentDictionary<IHubProtocol, ReadOnlyMemory<byte>> _messageCache = new ConcurrentDictionary<IHubProtocol, ReadOnlyMemory<byte>>();

        public static ReadOnlySpan<byte> GetSuccessfulHandshake(IHubProtocol protocol)
        {
            ReadOnlyMemory<byte> result;
            if (!_messageCache.TryGetValue(protocol, out result))
            {
                var memoryBufferWriter = MemoryBufferWriter.Get();
                try
                {
                    WriteResponseMessage(new HandshakeResponseMessage(protocol.MinorVersion), memoryBufferWriter);
                    result = memoryBufferWriter.ToArray();
                    _messageCache.TryAdd(protocol, result);
                }
                finally
                {
                    MemoryBufferWriter.Return(memoryBufferWriter);
                }
            }

            return result.Span;
        }

        /// <summary>
        /// Writes the serialized representation of a <see cref="HandshakeRequestMessage"/> to the specified writer.
        /// </summary>
        /// <param name="requestMessage">The message to write.</param>
        /// <param name="output">The output writer.</param>
        public static void WriteRequestMessage(HandshakeRequestMessage requestMessage, IBufferWriter<byte> output)
        {
            var writer = new Utf8JsonWriter(output, new JsonWriterState(new JsonWriterOptions() { SkipValidation = true }));

            writer.WriteStartObject();
            writer.WriteString(ProtocolPropertyNameBytes, requestMessage.Protocol, escape: false);
            writer.WriteNumber(ProtocolVersionPropertyNameBytes, requestMessage.Version, escape: false);
            writer.WriteEndObject();
            writer.Flush(isFinalBlock: true);

            TextMessageFormatter.WriteRecordSeparator(output);
        }

        /// <summary>
        /// Writes the serialized representation of a <see cref="HandshakeResponseMessage"/> to the specified writer.
        /// </summary>
        /// <param name="responseMessage">The message to write.</param>
        /// <param name="output">The output writer.</param>
        public static void WriteResponseMessage(HandshakeResponseMessage responseMessage, IBufferWriter<byte> output)
        {
            var writer = new Utf8JsonWriter(output, new JsonWriterState(new JsonWriterOptions() { SkipValidation = true }));

            writer.WriteStartObject();
            if (!string.IsNullOrEmpty(responseMessage.Error))
            {
                writer.WriteString(ErrorPropertyNameBytes, responseMessage.Error);
            }

            writer.WriteNumber(MinorVersionPropertyNameBytes, responseMessage.MinorVersion, escape: false);

            writer.WriteEndObject();
            writer.Flush(isFinalBlock: true);

            TextMessageFormatter.WriteRecordSeparator(output);
        }

        /// <summary>
        /// Creates a new <see cref="HandshakeResponseMessage"/> from the specified serialized representation.
        /// </summary>
        /// <param name="buffer">The serialized representation of the message.</param>
        /// <param name="responseMessage">When this method returns, contains the parsed message.</param>
        /// <returns>A value that is <c>true</c> if the <see cref="HandshakeResponseMessage"/> was successfully parsed; otherwise, <c>false</c>.</returns>
        public static bool TryParseResponseMessage(ref ReadOnlySequence<byte> buffer, out HandshakeResponseMessage responseMessage)
        {
            if (!TextMessageParser.TryParseMessage(ref buffer, out var payload))
            {
                responseMessage = null;
                return false;
            }

            var reader = new Utf8JsonReader(in payload, isFinalBlock: true, state: default);

            reader.CheckRead();
            reader.EnsureObjectStart();

            int? minorVersion = null;
            string error = null;

            while (reader.CheckRead())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var memberName = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;

                    if (memberName.SequenceEqual(TypePropertyNameBytes))
                    {
                        // a handshake response does not have a type
                        // check the incoming message was not any other type of message
                        throw new InvalidDataException("Expected a handshake response from the server.");
                    }
                    else if (memberName.SequenceEqual(ErrorPropertyNameBytes))
                    {
                        error = reader.ReadAsString(ErrorPropertyName);
                    }
                    else if (memberName.SequenceEqual(MinorVersionPropertyNameBytes))
                    {
                        minorVersion = reader.ReadAsInt32(MinorVersionPropertyName);
                    }
                    else
                    {
                        reader.Skip();
                    }
                }
                else if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }
                else
                {
                    throw new InvalidDataException($"Unexpected token '{reader.TokenType}' when reading handshake response JSON.");
                }
            };

            responseMessage = new HandshakeResponseMessage(minorVersion, error);
            return true;
        }

        /// <summary>
        /// Creates a new <see cref="HandshakeRequestMessage"/> from the specified serialized representation.
        /// </summary>
        /// <param name="buffer">The serialized representation of the message.</param>
        /// <param name="requestMessage">When this method returns, contains the parsed message.</param>
        /// <returns>A value that is <c>true</c> if the <see cref="HandshakeRequestMessage"/> was successfully parsed; otherwise, <c>false</c>.</returns>
        public static bool TryParseRequestMessage(ref ReadOnlySequence<byte> buffer, out HandshakeRequestMessage requestMessage)
        {
            if (!TextMessageParser.TryParseMessage(ref buffer, out var payload))
            {
                requestMessage = null;
                return false;
            }

            var reader = new Utf8JsonReader(in payload, isFinalBlock: true, state: default);

            reader.CheckRead();
            reader.EnsureObjectStart();

            string protocol = null;
            int? protocolVersion = null;

            while (reader.CheckRead())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var memberName = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;

                    if (memberName.SequenceEqual(ProtocolPropertyNameBytes))
                    {
                        protocol = reader.ReadAsString(ProtocolPropertyName);
                    }
                    else if (memberName.SequenceEqual(ProtocolVersionPropertyNameBytes))
                    {
                        protocolVersion = reader.ReadAsInt32(ProtocolVersionPropertyName);
                    }
                    else
                    {
                        reader.Skip();
                    }
                }
                else if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }
                else
                {
                    throw new InvalidDataException($"Unexpected token '{reader.TokenType}' when reading handshake request JSON. Message content: {GetPayloadAsString()}");
                }
            }

            if (protocol == null)
            {
                throw new InvalidDataException($"Missing required property '{ProtocolPropertyName}'. Message content: {GetPayloadAsString()}");
            }
            if (protocolVersion == null)
            {
                throw new InvalidDataException($"Missing required property '{ProtocolVersionPropertyName}'. Message content: {GetPayloadAsString()}");
            }

            requestMessage = new HandshakeRequestMessage(protocol, protocolVersion.Value);

            // For error messages, we want to print the payload as text
            string GetPayloadAsString()
            {
                // REVIEW: Should we show hex for binary charaters?
                return Encoding.UTF8.GetString(payload.ToArray());
            }

            return true;
        }
    }
}
