// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.SignalR.Internal.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.SignalR.Internal.Protocol
{
    public static class HandshakeProtocol
    {
        private const string ProtocolPropertyName = "protocol";
        private const string ProtocolVersionName = "version";
        private const string ErrorPropertyName = "error";
        private const string TypePropertyName = "type";

        public static void WriteRequestMessage(HandshakeRequestMessage requestMessage, IBufferWriter<byte> output)
        {
            var textWriter = Utf8BufferTextWriter.Get(output);
            try
            {
                using (var writer = CreateJsonTextWriter(textWriter))
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName(ProtocolPropertyName);
                    writer.WriteValue(requestMessage.Protocol);
                    writer.WritePropertyName(ProtocolVersionName);
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

        public static void WriteResponseMessage(HandshakeResponseMessage responseMessage, IBufferWriter<byte> output)
        {
            var textWriter = Utf8BufferTextWriter.Get(output);
            try
            {
                using (var writer = CreateJsonTextWriter(textWriter))
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

        private static JsonTextWriter CreateJsonTextWriter(TextWriter textWriter)
        {
            var writer = new JsonTextWriter(textWriter);
            writer.CloseOutput = false;

            return writer;
        }

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
                    var token = JToken.ReadFrom(reader);
                    var handshakeJObject = JsonUtils.GetObject(token);

                    // a handshake response does not have a type
                    // check the incoming message was not any other type of message
                    var type = JsonUtils.GetOptionalProperty<string>(handshakeJObject, TypePropertyName);
                    if (!string.IsNullOrEmpty(type))
                    {
                        throw new InvalidOperationException("Handshake response should not have a 'type' value.");
                    }

                    var error = JsonUtils.GetOptionalProperty<string>(handshakeJObject, ErrorPropertyName);
                    responseMessage = new HandshakeResponseMessage(error);
                    return true;
                }
            }
            finally
            {
                Utf8BufferTextReader.Return(textReader);
            }
        }

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
                    var token = JToken.ReadFrom(reader);
                    var handshakeJObject = JsonUtils.GetObject(token);
                    var protocol = JsonUtils.GetRequiredProperty<string>(handshakeJObject, ProtocolPropertyName);
                    var protocolVersion = JsonUtils.GetRequiredProperty<int>(handshakeJObject, ProtocolVersionName, JTokenType.Integer);
                    requestMessage = new HandshakeRequestMessage(protocol, protocolVersion);
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
