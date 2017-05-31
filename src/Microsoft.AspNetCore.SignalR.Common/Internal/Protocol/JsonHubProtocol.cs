// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO;
using Microsoft.AspNetCore.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.SignalR.Internal.Protocol
{
    public class JsonHubProtocol : IHubProtocol
    {
        private const string ResultPropertyName = "result";
        private const string ItemPropertyName = "item";
        private const string InvocationIdPropertyName = "invocationId";
        private const string TypePropertyName = "type";
        private const string ErrorPropertyName = "error";
        private const string TargetPropertyName = "target";
        private const string NonBlockingPropertyName = "nonBlocking";
        private const string ArgumentsPropertyName = "arguments";

        private const int InvocationMessageType = 1;
        private const int ResultMessageType = 2;
        private const int CompletionMessageType = 3;

        // ONLY to be used for application payloads (args, return values, etc.)
        private JsonSerializer _payloadSerializer;

        public MessageType MessageType => MessageType.Text;

        /// <summary>
        /// Creates an instance of the <see cref="JsonHubProtocol"/> using the specified <see cref="JsonSerializer"/>
        /// to serialize application payloads (arguments, results, etc.). The serialization of the outer protocol can
        /// NOT be changed using this serializer.
        /// </summary>
        /// <param name="payloadSerializer">The <see cref="JsonSerializer"/> to use to serialize application payloads (arguments, results, etc.).</param>
        public JsonHubProtocol(JsonSerializer payloadSerializer)
        {
            if (payloadSerializer == null)
            {
                throw new ArgumentNullException(nameof(payloadSerializer));
            }

            _payloadSerializer = payloadSerializer;
        }

        public HubMessage ParseMessage(ReadOnlySpan<byte> input, IInvocationBinder binder)
        {
            // TODO: Need a span-native JSON parser!
            using (var memoryStream = new MemoryStream(input.ToArray()))
            {
                return ParseMessage(memoryStream, binder);
            }
        }

        public bool TryWriteMessage(HubMessage message, IOutput output)
        {
            // TODO: Need IOutput-compatible JSON serializer!
            using (var memoryStream = new MemoryStream())
            {
                WriteMessage(message, memoryStream);
                memoryStream.Flush();

                return output.TryWrite(memoryStream.ToArray());
            }
        }

        private HubMessage ParseMessage(Stream input, IInvocationBinder binder)
        {
            using (var reader = new JsonTextReader(new StreamReader(input)))
            {
                try
                {
                    // PERF: Could probably use the JsonTextReader directly for better perf and fewer allocations
                    var token = JToken.ReadFrom(reader);
                    if (token == null)
                    {
                        return null;
                    }

                    if (token.Type != JTokenType.Object)
                    {
                        throw new FormatException($"Unexpected JSON Token Type '{token.Type}'. Expected a JSON Object.");
                    }

                    var json = (JObject)token;

                    // Determine the type of the message
                    var type = GetRequiredProperty<int>(json, TypePropertyName, JTokenType.Integer);
                    switch (type)
                    {
                        case InvocationMessageType:
                            return BindInvocationMessage(json, binder);
                        case ResultMessageType:
                            return BindResultMessage(json, binder);
                        case CompletionMessageType:
                            return BindCompletionMessage(json, binder);
                        default:
                            throw new FormatException($"Unknown message type: {type}");
                    }
                }
                catch (JsonReaderException jrex)
                {
                    throw new FormatException("Error reading JSON.", jrex);
                }
            }
        }

        private void WriteMessage(HubMessage message, Stream stream)
        {
            using (var writer = new JsonTextWriter(new StreamWriter(stream)))
            {
                switch (message)
                {
                    case InvocationMessage m:
                        WriteInvocationMessage(m, writer);
                        break;
                    case StreamItemMessage m:
                        WriteStreamItemMessage(m, writer);
                        break;
                    case CompletionMessage m:
                        WriteCompletionMessage(m, writer);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported message type: {message.GetType().FullName}");
                }
            }
        }

        private void WriteCompletionMessage(CompletionMessage message, JsonTextWriter writer)
        {
            writer.WriteStartObject();
            WriteHubMessageCommon(message, writer, CompletionMessageType);
            if (!string.IsNullOrEmpty(message.Error))
            {
                writer.WritePropertyName(ErrorPropertyName);
                writer.WriteValue(message.Error);
            }
            else if (message.HasResult)
            {
                writer.WritePropertyName(ResultPropertyName);
                _payloadSerializer.Serialize(writer, message.Result);
            }
            writer.WriteEndObject();
        }

        private void WriteStreamItemMessage(StreamItemMessage message, JsonTextWriter writer)
        {
            writer.WriteStartObject();
            WriteHubMessageCommon(message, writer, ResultMessageType);
            writer.WritePropertyName(ItemPropertyName);
            _payloadSerializer.Serialize(writer, message.Item);
            writer.WriteEndObject();
        }

        private void WriteInvocationMessage(InvocationMessage message, JsonTextWriter writer)
        {
            writer.WriteStartObject();
            WriteHubMessageCommon(message, writer, InvocationMessageType);
            writer.WritePropertyName(TargetPropertyName);
            writer.WriteValue(message.Target);

            if (message.NonBlocking)
            {
                writer.WritePropertyName(NonBlockingPropertyName);
                writer.WriteValue(message.NonBlocking);
            }

            writer.WritePropertyName(ArgumentsPropertyName);
            writer.WriteStartArray();
            foreach (var argument in message.Arguments)
            {
                _payloadSerializer.Serialize(writer, argument);
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }

        private static void WriteHubMessageCommon(HubMessage message, JsonTextWriter writer, int type)
        {
            writer.WritePropertyName(InvocationIdPropertyName);
            writer.WriteValue(message.InvocationId);
            writer.WritePropertyName(TypePropertyName);
            writer.WriteValue(type);
        }

        private InvocationMessage BindInvocationMessage(JObject json, IInvocationBinder binder)
        {
            var invocationId = GetRequiredProperty<string>(json, InvocationIdPropertyName, JTokenType.String);
            var target = GetRequiredProperty<string>(json, TargetPropertyName, JTokenType.String);
            var nonBlocking = GetOptionalProperty<bool>(json, NonBlockingPropertyName, JTokenType.Boolean);

            var args = GetRequiredProperty<JArray>(json, ArgumentsPropertyName, JTokenType.Array);

            var paramTypes = binder.GetParameterTypes(target);
            var arguments = new object[args.Count];
            if (paramTypes.Length != arguments.Length)
            {
                throw new FormatException($"Invocation provides {arguments.Length} argument(s) but target expects {paramTypes.Length}.");
            }

            for (var i = 0; i < paramTypes.Length; i++)
            {
                var paramType = paramTypes[i];

                // TODO(anurse): We can add some DI magic here to allow users to provide their own serialization
                // Related Bug: https://github.com/aspnet/SignalR/issues/261
                arguments[i] = args[i].ToObject(paramType, _payloadSerializer);
            }

            return new InvocationMessage(invocationId, nonBlocking, target, arguments);
        }

        private StreamItemMessage BindResultMessage(JObject json, IInvocationBinder binder)
        {
            var invocationId = GetRequiredProperty<string>(json, InvocationIdPropertyName, JTokenType.String);
            var result = GetRequiredProperty<JToken>(json, ItemPropertyName);

            var returnType = binder.GetReturnType(invocationId);
            return new StreamItemMessage(invocationId, result?.ToObject(returnType, _payloadSerializer));
        }

        private CompletionMessage BindCompletionMessage(JObject json, IInvocationBinder binder)
        {
            var invocationId = GetRequiredProperty<string>(json, InvocationIdPropertyName, JTokenType.String);
            var error = GetOptionalProperty<string>(json, ErrorPropertyName, JTokenType.String);
            var resultProp = json.Property(ResultPropertyName);

            if (error != null && resultProp != null)
            {
                throw new FormatException("The 'error' and 'result' properties are mutually exclusive.");
            }

            if (resultProp == null)
            {
                return new CompletionMessage(invocationId, error, result: null, hasResult: false);
            }
            else
            {
                var returnType = binder.GetReturnType(invocationId);
                var payload = resultProp.Value?.ToObject(returnType, _payloadSerializer);
                return new CompletionMessage(invocationId, error, result: payload, hasResult: true);
            }
        }

        private T GetOptionalProperty<T>(JObject json, string property, JTokenType expectedType = JTokenType.None, T defaultValue = default(T))
        {
            var prop = json[property];

            if (prop == null)
            {
                return defaultValue;
            }

            return GetValue<T>(property, expectedType, prop);
        }

        private T GetRequiredProperty<T>(JObject json, string property, JTokenType expectedType = JTokenType.None)
        {
            var prop = json[property];

            if (prop == null)
            {
                throw new FormatException($"Missing required property '{property}'.");
            }

            return GetValue<T>(property, expectedType, prop);
        }

        private static T GetValue<T>(string property, JTokenType expectedType, JToken prop)
        {
            if (expectedType != JTokenType.None && prop.Type != expectedType)
            {
                throw new FormatException($"Expected '{property}' to be of type {expectedType}.");
            }
            return prop.Value<T>();
        }
    }
}
