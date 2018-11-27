// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Internal;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR.Protocol
{
    /// <summary>
    /// A helper class for working with SignalR handshakes.
    /// </summary>
    public static class HandshakeProtocol
    {
        private const string ProtocolPropertyName = "protocol";
        private const string ProtocolVersionPropertyName = "version";
        private const string ErrorPropertyName = "error";
        private const string TypePropertyName = "type";

        /// <summary>
        /// The serialized representation of a success handshake.
        /// </summary>
        public static ReadOnlyMemory<byte> SuccessHandshakeData;

        static HandshakeProtocol()
        {
            var memoryBufferWriter = MemoryBufferWriter.Get();
            try
            {
                WriteResponseMessage(HandshakeResponseMessage.Empty, memoryBufferWriter);
                SuccessHandshakeData = memoryBufferWriter.ToArray();
            }
            finally
            {
                MemoryBufferWriter.Return(memoryBufferWriter);
            }
        }

        /// <summary>
        /// Writes the serialized representation of a <see cref="HandshakeRequestMessage"/> to the specified writer.
        /// </summary>
        /// <param name="requestMessage">The message to write.</param>
        /// <param name="output">The output writer.</param>
        public static void WriteRequestMessage(HandshakeRequestMessage requestMessage, IBufferWriter<byte> output)
        {
            var textWriter = Utf8BufferTextWriter.Get(output);
            try
            {
                using (var writer = JsonUtils.CreateJsonTextWriter(textWriter))
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName(ProtocolPropertyName);
                    writer.WriteValue(requestMessage.Protocol);
                    writer.WritePropertyName(ProtocolVersionPropertyName);
                    writer.WriteValue(requestMessage.Version);
                    writer.WriteEndObject();
                    writer.Flush();
                }
            }
            finally
            {
                Utf8BufferTextWriter.Return(textWriter);
            }

            TextMessageFormatter.WriteRecordSeparator(output);
        }

        /// <summary>
        /// Writes the serialized representation of a <see cref="HandshakeResponseMessage"/> to the specified writer.
        /// </summary>
        /// <param name="responseMessage">The message to write.</param>
        /// <param name="output">The output writer.</param>
        public static void WriteResponseMessage(HandshakeResponseMessage responseMessage, IBufferWriter<byte> output)
        {
            var textWriter = Utf8BufferTextWriter.Get(output);
            try
            {
                using (var writer = JsonUtils.CreateJsonTextWriter(textWriter))
                {
                    writer.WriteStartObject();
                    if (!string.IsNullOrEmpty(responseMessage.Error))
                    {
                        writer.WritePropertyName(ErrorPropertyName);
                        writer.WriteValue(responseMessage.Error);
                    }

                    writer.WriteEndObject();
                    writer.Flush();
                }
            }
            finally
            {
                Utf8BufferTextWriter.Return(textWriter);
            }

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

            var textReader = Utf8BufferTextReader.Get(payload);

            try
            {
                using (var reader = JsonUtils.CreateJsonTextReader(textReader))
                {
                    JsonUtils.CheckRead(reader);
                    JsonUtils.EnsureObjectStart(reader);

                    string error = null;

                    var completed = false;
                    while (!completed && JsonUtils.CheckRead(reader))
                    {
                        switch (reader.TokenType)
                        {
                            case JsonToken.PropertyName:
                                var memberName = reader.Value.ToString();

                                switch (memberName)
                                {
                                    case TypePropertyName:
                                        // a handshake response does not have a type
                                        // check the incoming message was not any other type of message
                                        throw new InvalidDataException("Handshake response should not have a 'type' value.");
                                    case ErrorPropertyName:
                                        error = JsonUtils.ReadAsString(reader, ErrorPropertyName);
                                        break;
                                    default:
                                        reader.Skip();
                                        break;
                                }
                                break;
                            case JsonToken.EndObject:
                                completed = true;
                                break;
                            default:
                                throw new InvalidDataException($"Unexpected token '{reader.TokenType}' when reading handshake response JSON.");
                        }
                    };

                    responseMessage = (error != null) ? new HandshakeResponseMessage(error) : HandshakeResponseMessage.Empty;
                    return true;
                }
            }
            finally
            {
                Utf8BufferTextReader.Return(textReader);
            }
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

            var textReader = Utf8BufferTextReader.Get(payload);
            try
            {
                using (var reader = JsonUtils.CreateJsonTextReader(textReader))
                {
                    JsonUtils.CheckRead(reader);
                    JsonUtils.EnsureObjectStart(reader);

                    string protocol = null;
                    int? protocolVersion = null;

                    var completed = false;
                    while (!completed && JsonUtils.CheckRead(reader))
                    {
                        switch (reader.TokenType)
                        {
                            case JsonToken.PropertyName:
                                var memberName = reader.Value.ToString();

                                switch (memberName)
                                {
                                    case ProtocolPropertyName:
                                        protocol = JsonUtils.ReadAsString(reader, ProtocolPropertyName);
                                        break;
                                    case ProtocolVersionPropertyName:
                                        protocolVersion = JsonUtils.ReadAsInt32(reader, ProtocolVersionPropertyName);
                                        break;
                                    default:
                                        reader.Skip();
                                        break;
                                }
                                break;
                            case JsonToken.EndObject:
                                completed = true;
                                break;
                            default:
                                throw new InvalidDataException($"Unexpected token '{reader.TokenType}' when reading handshake request JSON.");
                        }
                    }

                    if (protocol == null)
                    {
                        throw new InvalidDataException($"Missing required property '{ProtocolPropertyName}'.");
                    }
                    if (protocolVersion == null)
                    {
                        throw new InvalidDataException($"Missing required property '{ProtocolVersionPropertyName}'.");
                    }

                    requestMessage = new HandshakeRequestMessage(protocol, protocolVersion.Value);
                }
            }
            finally
            {
                Utf8BufferTextReader.Return(textReader);
            }

            return true;
        }
    }
}
