// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections;
using System.IO;
using Microsoft.AspNetCore.SignalR.Internal.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.SignalR.Internal.Protocol
{
    public static class NegotiationProtocol
    {
        private const string ProtocolPropertyName = "protocol";

        public static void WriteMessage(NegotiationMessage negotiationMessage, Stream output)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var writer = new JsonTextWriter(new StreamWriter(memoryStream)))
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName(ProtocolPropertyName);
                    writer.WriteValue(negotiationMessage.Protocol);
                    writer.WriteEndObject();
                }

                TextMessageFormatter.WriteMessage(memoryStream.ToArray(), output);
            }
        }

        public static bool TryParseMessage(ReadOnlySpan<byte> input, out NegotiationMessage negotiationMessage)
        {
            if (!TextMessageParser.TryParseMessage(ref input, out var payload))
            {
                throw new InvalidDataException("Unable to parse payload as a negotiation message.");
            }

            using (var memoryStream = new MemoryStream(payload.ToArray()))
            {
                using (var reader = new JsonTextReader(new StreamReader(memoryStream)))
                {
                    var token = JToken.ReadFrom(reader);
                    if (token == null || token.Type != JTokenType.Object)
                    {
                        throw new InvalidDataException($"Unexpected JSON Token Type '{token?.Type}'. Expected a JSON Object.");
                    }

                    var negotiationJObject = (JObject)token;
                    var protocol = JsonUtils.GetRequiredProperty<string>(negotiationJObject, ProtocolPropertyName);
                    negotiationMessage = new NegotiationMessage(protocol);
                }
            }
            return true;
        }

        public static bool TryParseMessage(ReadOnlyBuffer<byte> buffer, out NegotiationMessage negotiationMessage, out SequencePosition consumed, out SequencePosition examined)
        {
            var separator = buffer.PositionOf(TextMessageFormatter.RecordSeparator);
            if (separator == null)
            {
                // Haven't seen the entire negotiate message so bail
                consumed = buffer.Start;
                examined = buffer.End;
                negotiationMessage = null;
                return false;
            }
            else
            {
                consumed = buffer.GetPosition(separator.Value, 1);
                examined = consumed;
            }

            var memory = buffer.IsSingleSegment ? buffer.First : buffer.ToArray();

            return TryParseMessage(memory.Span, out negotiationMessage);
        }
    }
}
