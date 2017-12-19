// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.SignalR.Internal.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

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
        private const string ArgumentsPropertyName = "arguments";
        private const string PayloadPropertyName = "payload";

        // ONLY to be used for application payloads (args, return values, etc.)
        private JsonSerializer _payloadSerializer;

        /// <summary>
        /// Creates an instance of the <see cref="JsonHubProtocol"/> using the default <see cref="JsonSerializer"/>
        /// to serialize application payloads (arguments, results, etc.). The serialization of the outer protocol can
        /// NOT be changed using this serializer.
        /// </summary>
        public JsonHubProtocol()
            : this(JsonSerializer.Create(CreateDefaultSerializerSettings()))
        { }

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

        public string Name => "json";

        public ProtocolType Type => ProtocolType.Text;

        public bool TryParseMessages(ReadOnlySpan<byte> input, IInvocationBinder binder, out IList<HubMessage> messages)
        {
            messages = new List<HubMessage>();

            while (TextMessageParser.TryParseMessage(ref input, out var payload))
            {
                // TODO: Need a span-native JSON parser!
                using (var memoryStream = new MemoryStream(payload.ToArray()))
                {
                    messages.Add(ParseMessage(memoryStream, binder));
                }
            }

            return messages.Count > 0;
        }

        public void WriteMessage(HubMessage message, Stream output)
        {
            using (var memoryStream = new MemoryStream())
            {
                WriteMessageCore(message, memoryStream);
                memoryStream.Flush();

                TextMessageFormatter.WriteMessage(memoryStream.ToArray(), output);
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

                    if (token == null || token.Type != JTokenType.Object)
                    {
                        throw new FormatException($"Unexpected JSON Token Type '{token?.Type}'. Expected a JSON Object.");
                    }

                    var json = (JObject)token;

                    // Determine the type of the message
                    var type = JsonUtils.GetRequiredProperty<int>(json, TypePropertyName, JTokenType.Integer);
                    switch (type)
                    {
                        case HubProtocolConstants.InvocationMessageType:
                            return BindInvocationMessage(json, binder);
                        case HubProtocolConstants.StreamInvocationMessageType:
                            return BindStreamInvocationMessage(json, binder);
                        case HubProtocolConstants.StreamItemMessageType:
                            return BindStreamItemMessage(json, binder);
                        case HubProtocolConstants.CompletionMessageType:
                            return BindCompletionMessage(json, binder);
                        case HubProtocolConstants.CancelInvocationMessageType:
                            return BindCancelInvocationMessage(json);
                        case HubProtocolConstants.PingMessageType:
                            return PingMessage.Instance;
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

        private void WriteMessageCore(HubMessage message, Stream stream)
        {
            using (var writer = new JsonTextWriter(new StreamWriter(stream)))
            {
                switch (message)
                {
                    case InvocationMessage m:
                        WriteInvocationMessage(m, writer);
                        break;
                    case StreamInvocationMessage m:
                        WriteStreamInvocationMessage(m, writer);
                        break;
                    case StreamItemMessage m:
                        WriteStreamItemMessage(m, writer);
                        break;
                    case CompletionMessage m:
                        WriteCompletionMessage(m, writer);
                        break;
                    case CancelInvocationMessage m:
                        WriteCancelInvocationMessage(m, writer);
                        break;
                    case PingMessage m:
                        WritePingMessage(m, writer);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported message type: {message.GetType().FullName}");
                }
            }
        }

        private void WriteCompletionMessage(CompletionMessage message, JsonTextWriter writer)
        {
            writer.WriteStartObject();
            WriteHubInvocationMessageCommon(message, writer, HubProtocolConstants.CompletionMessageType);
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

        private void WriteCancelInvocationMessage(CancelInvocationMessage message, JsonTextWriter writer)
        {
            writer.WriteStartObject();
            WriteHubInvocationMessageCommon(message, writer, HubProtocolConstants.CancelInvocationMessageType);
            writer.WriteEndObject();
        }

        private void WriteStreamItemMessage(StreamItemMessage message, JsonTextWriter writer)
        {
            writer.WriteStartObject();
            WriteHubInvocationMessageCommon(message, writer, HubProtocolConstants.StreamItemMessageType);
            writer.WritePropertyName(ItemPropertyName);
            _payloadSerializer.Serialize(writer, message.Item);
            writer.WriteEndObject();
        }

        private void WriteInvocationMessage(InvocationMessage message, JsonTextWriter writer)
        {
            writer.WriteStartObject();
            WriteHubInvocationMessageCommon(message, writer, HubProtocolConstants.InvocationMessageType);
            writer.WritePropertyName(TargetPropertyName);
            writer.WriteValue(message.Target);

            WriteArguments(message.Arguments, writer);

            writer.WriteEndObject();
        }

        private void WriteStreamInvocationMessage(StreamInvocationMessage message, JsonTextWriter writer)
        {
            writer.WriteStartObject();
            WriteHubInvocationMessageCommon(message, writer, HubProtocolConstants.StreamInvocationMessageType);
            writer.WritePropertyName(TargetPropertyName);
            writer.WriteValue(message.Target);

            WriteArguments(message.Arguments, writer);

            writer.WriteEndObject();
        }

        private void WriteArguments(object[] arguments, JsonTextWriter writer)
        {
            writer.WritePropertyName(ArgumentsPropertyName);
            writer.WriteStartArray();
            foreach (var argument in arguments)
            {
                _payloadSerializer.Serialize(writer, argument);
            }
            writer.WriteEndArray();
        }

        private void WritePingMessage(PingMessage m, JsonTextWriter writer)
        {
            writer.WriteStartObject();
            WriteMessageType(writer, HubProtocolConstants.PingMessageType);
            writer.WriteEndObject();
        }

        private static void WriteHubInvocationMessageCommon(HubInvocationMessage message, JsonTextWriter writer, int type)
        {
            if (!string.IsNullOrEmpty(message.InvocationId))
            {
                writer.WritePropertyName(InvocationIdPropertyName);
                writer.WriteValue(message.InvocationId);
            }
            WriteMessageType(writer, type);
        }

        private static void WriteMessageType(JsonTextWriter writer, int type)
        {
            writer.WritePropertyName(TypePropertyName);
            writer.WriteValue(type);
        }

        private InvocationMessage BindInvocationMessage(JObject json, IInvocationBinder binder)
        {
            var invocationId = JsonUtils.GetOptionalProperty<string>(json, InvocationIdPropertyName, JTokenType.String);
            var target = JsonUtils.GetRequiredProperty<string>(json, TargetPropertyName, JTokenType.String);

            var args = JsonUtils.GetRequiredProperty<JArray>(json, ArgumentsPropertyName, JTokenType.Array);

            var paramTypes = binder.GetParameterTypes(target);

            try
            {
                var arguments = BindArguments(args, paramTypes);
                return new InvocationMessage(invocationId, target, argumentBindingException: null, arguments: arguments);
            }
            catch (Exception ex)
            {
                return new InvocationMessage(invocationId, target, ExceptionDispatchInfo.Capture(ex));
            }
        }

        private StreamInvocationMessage BindStreamInvocationMessage(JObject json, IInvocationBinder binder)
        {
            var invocationId = JsonUtils.GetRequiredProperty<string>(json, InvocationIdPropertyName, JTokenType.String);
            var target = JsonUtils.GetRequiredProperty<string>(json, TargetPropertyName, JTokenType.String);

            var args = JsonUtils.GetRequiredProperty<JArray>(json, ArgumentsPropertyName, JTokenType.Array);

            var paramTypes = binder.GetParameterTypes(target);

            try
            {
                var arguments = BindArguments(args, paramTypes);
                return new StreamInvocationMessage(invocationId, target, argumentBindingException: null, arguments: arguments);
            }
            catch (Exception ex)
            {
                return new StreamInvocationMessage(invocationId, target, ExceptionDispatchInfo.Capture(ex));
            }
        }

        private object[] BindArguments(JArray args, Type[] paramTypes)
        {
            var arguments = new object[args.Count];
            if (paramTypes.Length != arguments.Length)
            {
                throw new FormatException($"Invocation provides {arguments.Length} argument(s) but target expects {paramTypes.Length}.");
            }

            try
            {
                for (var i = 0; i < paramTypes.Length; i++)
                {
                    var paramType = paramTypes[i];
                    arguments[i] = args[i].ToObject(paramType, _payloadSerializer);
                }

                return arguments;
            }
            catch (Exception ex)
            {
                throw new FormatException("Error binding arguments. Make sure that the types of the provided values match the types of the hub method being invoked.", ex);
            }
        }

        private StreamItemMessage BindStreamItemMessage(JObject json, IInvocationBinder binder)
        {
            var invocationId = JsonUtils.GetRequiredProperty<string>(json, InvocationIdPropertyName, JTokenType.String);
            var result = JsonUtils.GetRequiredProperty<JToken>(json, ItemPropertyName);

            var returnType = binder.GetReturnType(invocationId);
            return new StreamItemMessage(invocationId, result?.ToObject(returnType, _payloadSerializer));
        }

        private CompletionMessage BindCompletionMessage(JObject json, IInvocationBinder binder)
        {
            var invocationId = JsonUtils.GetRequiredProperty<string>(json, InvocationIdPropertyName, JTokenType.String);
            var error = JsonUtils.GetOptionalProperty<string>(json, ErrorPropertyName, JTokenType.String);
            var resultProp = json.Property(ResultPropertyName);

            if (error != null && resultProp != null)
            {
                throw new FormatException("The 'error' and 'result' properties are mutually exclusive.");
            }

            if (resultProp == null)
            {
                return new CompletionMessage(invocationId, error, result: null, hasResult: false);
            }

            var returnType = binder.GetReturnType(invocationId);
            var payload = resultProp.Value?.ToObject(returnType, _payloadSerializer);
            return new CompletionMessage(invocationId, error, result: payload, hasResult: true);
        }

        private CancelInvocationMessage BindCancelInvocationMessage(JObject json)
        {
            var invocationId = JsonUtils.GetRequiredProperty<string>(json, InvocationIdPropertyName, JTokenType.String);
            return new CancelInvocationMessage(invocationId);
        }

        public static JsonSerializerSettings CreateDefaultSerializerSettings()
        {
            return new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
        }
    }
}
