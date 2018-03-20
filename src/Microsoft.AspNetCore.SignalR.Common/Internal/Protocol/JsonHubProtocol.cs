// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text;
using Microsoft.AspNetCore.Protocols;
using Microsoft.AspNetCore.SignalR.Internal.Formatters;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.SignalR.Internal.Protocol
{
    public class JsonHubProtocol : IHubProtocol
    {
        private static readonly UTF8Encoding _utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private const string ResultPropertyName = "result";
        private const string ItemPropertyName = "item";
        private const string InvocationIdPropertyName = "invocationId";
        private const string TypePropertyName = "type";
        private const string ErrorPropertyName = "error";
        private const string TargetPropertyName = "target";
        private const string ArgumentsPropertyName = "arguments";
        private const string HeadersPropertyName = "headers";

        public static readonly string ProtocolName = "json";

        // ONLY to be used for application payloads (args, return values, etc.)
        public JsonSerializer PayloadSerializer { get; }

        public JsonHubProtocol() : this(Options.Create(new JsonHubProtocolOptions()))
        {
        }

        public JsonHubProtocol(IOptions<JsonHubProtocolOptions> options)
        {
            PayloadSerializer = JsonSerializer.Create(options.Value.PayloadSerializerSettings);
        }

        public string Name => ProtocolName;

        public TransferFormat TransferFormat => TransferFormat.Text;

        public bool TryParseMessages(ReadOnlyMemory<byte> input, IInvocationBinder binder, IList<HubMessage> messages)
        {
            while (TextMessageParser.TryParseMessage(ref input, out var payload))
            {
                var textReader = new Utf8BufferTextReader(payload);
                messages.Add(ParseMessage(textReader, binder));
            }

            return messages.Count > 0;
        }

        public void WriteMessage(HubMessage message, Stream output)
        {
            WriteMessageCore(message, output);
            TextMessageFormatter.WriteRecordSeparator(output);
        }

        private HubMessage ParseMessage(TextReader textReader, IInvocationBinder binder)
        {
            try
            {
                // We parse using the JsonTextReader directly but this has a problem. Some of our properties are dependent on other properties
                // and since reading the json might be unordered, we need to store the parsed content as JToken to re-parse when true types are known.
                // if we're lucky and the state we need to directly parse is available, then we'll use it.

                int? type = null;
                string invocationId = null;
                string target = null;
                string error = null;
                var hasItem = false;
                object item = null;
                JToken itemToken = null;
                var hasResult = false;
                object result = null;
                JToken resultToken = null;
                bool hasArguments = false;
                object[] arguments = null;
                JArray argumentsToken = null;
                ExceptionDispatchInfo argumentBindingException = null;
                Dictionary<string, string> headers = null;
                var completed = false;

                using (var reader = new JsonTextReader(textReader))
                {
                    reader.ArrayPool = JsonArrayPool<char>.Shared;

                    JsonUtils.CheckRead(reader);

                    // We're always parsing a JSON object
                    if (reader.TokenType != JsonToken.StartObject)
                    {
                        throw new InvalidDataException($"Unexpected JSON Token Type '{JsonUtils.GetTokenString(reader.TokenType)}'. Expected a JSON Object.");
                    }

                    do
                    {
                        switch (reader.TokenType)
                        {
                            case JsonToken.PropertyName:
                                string memberName = reader.Value.ToString();

                                switch (memberName)
                                {
                                    case TypePropertyName:
                                        var messageType = JsonUtils.ReadAsInt32(reader, TypePropertyName);

                                        if (messageType == null)
                                        {
                                            throw new InvalidDataException($"Missing required property '{TypePropertyName}'.");
                                        }

                                        type = messageType.Value;
                                        break;
                                    case InvocationIdPropertyName:
                                        invocationId = JsonUtils.ReadAsString(reader, InvocationIdPropertyName);
                                        break;
                                    case TargetPropertyName:
                                        target = JsonUtils.ReadAsString(reader, TargetPropertyName);
                                        break;
                                    case ErrorPropertyName:
                                        error = JsonUtils.ReadAsString(reader, ErrorPropertyName);
                                        break;
                                    case ResultPropertyName:
                                        JsonUtils.CheckRead(reader);

                                        hasResult = true;

                                        if (string.IsNullOrEmpty(invocationId))
                                        {
                                            // If we don't have an invocation id then we need to store it as a JToken so we can parse it later
                                            resultToken = JToken.Load(reader);
                                        }
                                        else
                                        {
                                            // If we have an invocation id already we can parse the end result
                                            var returnType = binder.GetReturnType(invocationId);
                                            result = PayloadSerializer.Deserialize(reader, returnType);
                                        }
                                        break;
                                    case ItemPropertyName:
                                        JsonUtils.CheckRead(reader);

                                        hasItem = true;

                                        if (string.IsNullOrEmpty(invocationId))
                                        {
                                            // If we don't have an invocation id then we need to store it as a JToken so we can parse it later
                                            itemToken = JToken.Load(reader);
                                        }
                                        else
                                        {
                                            var returnType = binder.GetReturnType(invocationId);
                                            item = PayloadSerializer.Deserialize(reader, returnType);
                                        }
                                        break;
                                    case ArgumentsPropertyName:
                                        JsonUtils.CheckRead(reader);

                                        if (reader.TokenType != JsonToken.StartArray)
                                        {
                                            throw new InvalidDataException($"Expected '{ArgumentsPropertyName}' to be of type {JTokenType.Array}.");
                                        }

                                        hasArguments = true;

                                        if (string.IsNullOrEmpty(target))
                                        {
                                            // We don't know the method name yet so just parse an array of generic JArray
                                            argumentsToken = JArray.Load(reader);
                                        }
                                        else
                                        {
                                            try
                                            {
                                                var paramTypes = binder.GetParameterTypes(target);
                                                arguments = BindArguments(reader, paramTypes);
                                            }
                                            catch (Exception ex)
                                            {
                                                argumentBindingException = ExceptionDispatchInfo.Capture(ex);
                                            }
                                        }
                                        break;
                                    case HeadersPropertyName:
                                        JsonUtils.CheckRead(reader);
                                        headers = ReadHeaders(reader);
                                        break;
                                    default:
                                        // Skip read the property name
                                        JsonUtils.CheckRead(reader);
                                        // Skip the value for this property
                                        reader.Skip();
                                        break;
                                }
                                break;
                            case JsonToken.EndObject:
                                completed = true;
                                break;
                        }
                    }
                    while (!completed && JsonUtils.CheckRead(reader));
                }

                HubMessage message;

                switch (type)
                {
                    case HubProtocolConstants.InvocationMessageType:
                        {
                            if (argumentsToken != null)
                            {
                                try
                                {
                                    var paramTypes = binder.GetParameterTypes(target);
                                    arguments = BindArguments(argumentsToken, paramTypes);
                                }
                                catch (Exception ex)
                                {
                                    argumentBindingException = ExceptionDispatchInfo.Capture(ex);
                                }
                            }

                            message = BindInvocationMessage(invocationId, target, argumentBindingException, arguments, hasArguments, binder);
                        }
                        break;
                    case HubProtocolConstants.StreamInvocationMessageType:
                        {
                            if (argumentsToken != null)
                            {
                                try
                                {
                                    var paramTypes = binder.GetParameterTypes(target);
                                    arguments = BindArguments(argumentsToken, paramTypes);
                                }
                                catch (Exception ex)
                                {
                                    argumentBindingException = ExceptionDispatchInfo.Capture(ex);
                                }
                            }

                            message = BindStreamInvocationMessage(invocationId, target, argumentBindingException, arguments, hasArguments, binder);
                        }
                        break;
                    case HubProtocolConstants.StreamItemMessageType:
                        if (itemToken != null)
                        {
                            var returnType = binder.GetReturnType(invocationId);
                            item = itemToken.ToObject(returnType, PayloadSerializer);
                        }

                        message = BindStreamItemMessage(invocationId, item, hasItem, binder);
                        break;
                    case HubProtocolConstants.CompletionMessageType:
                        if (resultToken != null)
                        {
                            var returnType = binder.GetReturnType(invocationId);
                            result = resultToken.ToObject(returnType, PayloadSerializer);
                        }

                        message = BindCompletionMessage(invocationId, error, result, hasResult, binder);
                        break;
                    case HubProtocolConstants.CancelInvocationMessageType:
                        message = BindCancelInvocationMessage(invocationId);
                        break;
                    case HubProtocolConstants.PingMessageType:
                        return PingMessage.Instance;
                    case HubProtocolConstants.CloseMessageType:
                        return BindCloseMessage(error);
                    case null:
                        throw new InvalidDataException($"Missing required property '{TypePropertyName}'.");
                    default:
                        throw new InvalidDataException($"Unknown message type: {type}");
                }

                return ApplyHeaders(message, headers);
            }
            catch (JsonReaderException jrex)
            {
                throw new InvalidDataException("Error reading JSON.", jrex);
            }
        }

        private Dictionary<string, string> ReadHeaders(JsonTextReader reader)
        {
            var headers = new Dictionary<string, string>();

            if (reader.TokenType != JsonToken.StartObject)
            {
                throw new InvalidDataException($"Expected '{HeadersPropertyName}' to be of type {JTokenType.Object}.");
            }

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.PropertyName:
                        string propertyName = reader.Value.ToString();

                        JsonUtils.CheckRead(reader);

                        if (reader.TokenType != JsonToken.String)
                        {
                            throw new InvalidDataException($"Expected header '{propertyName}' to be of type {JTokenType.String}.");
                        }

                        headers[propertyName] = reader.Value?.ToString();
                        break;
                    case JsonToken.Comment:
                        break;
                    case JsonToken.EndObject:
                        return headers;
                }
            }

            throw new JsonReaderException("Unexpected end when reading message headers");
        }

        private void WriteMessageCore(HubMessage message, Stream stream)
        {
            using (var writer = new JsonTextWriter(new StreamWriter(stream, _utf8NoBom, 1024, leaveOpen: true)))
            {
                writer.WriteStartObject();
                switch (message)
                {
                    case InvocationMessage m:
                        WriteMessageType(writer, HubProtocolConstants.InvocationMessageType);
                        WriteHeaders(writer, m);
                        WriteInvocationMessage(m, writer);
                        break;
                    case StreamInvocationMessage m:
                        WriteMessageType(writer, HubProtocolConstants.StreamInvocationMessageType);
                        WriteHeaders(writer, m);
                        WriteStreamInvocationMessage(m, writer);
                        break;
                    case StreamItemMessage m:
                        WriteMessageType(writer, HubProtocolConstants.StreamItemMessageType);
                        WriteHeaders(writer, m);
                        WriteStreamItemMessage(m, writer);
                        break;
                    case CompletionMessage m:
                        WriteMessageType(writer, HubProtocolConstants.CompletionMessageType);
                        WriteHeaders(writer, m);
                        WriteCompletionMessage(m, writer);
                        break;
                    case CancelInvocationMessage m:
                        WriteMessageType(writer, HubProtocolConstants.CancelInvocationMessageType);
                        WriteHeaders(writer, m);
                        WriteCancelInvocationMessage(m, writer);
                        break;
                    case PingMessage _:
                        WriteMessageType(writer, HubProtocolConstants.PingMessageType);
                        break;
                    case CloseMessage m:
                        WriteMessageType(writer, HubProtocolConstants.CloseMessageType);
                        WriteCloseMessage(m, writer);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported message type: {message.GetType().FullName}");
                }
                writer.WriteEndObject();
            }
        }

        private void WriteHeaders(JsonTextWriter writer, HubInvocationMessage message)
        {
            if (message.Headers != null && message.Headers.Count > 0)
            {
                writer.WritePropertyName(HeadersPropertyName);
                writer.WriteStartObject();
                foreach (var value in message.Headers)
                {
                    writer.WritePropertyName(value.Key);
                    writer.WriteValue(value.Value);
                }
                writer.WriteEndObject();
            }
        }

        private void WriteCompletionMessage(CompletionMessage message, JsonTextWriter writer)
        {
            WriteInvocationId(message, writer);
            if (!string.IsNullOrEmpty(message.Error))
            {
                writer.WritePropertyName(ErrorPropertyName);
                writer.WriteValue(message.Error);
            }
            else if (message.HasResult)
            {
                writer.WritePropertyName(ResultPropertyName);
                PayloadSerializer.Serialize(writer, message.Result);
            }
        }

        private void WriteCancelInvocationMessage(CancelInvocationMessage message, JsonTextWriter writer)
        {
            WriteInvocationId(message, writer);
        }

        private void WriteStreamItemMessage(StreamItemMessage message, JsonTextWriter writer)
        {
            WriteInvocationId(message, writer);
            writer.WritePropertyName(ItemPropertyName);
            PayloadSerializer.Serialize(writer, message.Item);
        }

        private void WriteInvocationMessage(InvocationMessage message, JsonTextWriter writer)
        {
            WriteInvocationId(message, writer);
            writer.WritePropertyName(TargetPropertyName);
            writer.WriteValue(message.Target);

            WriteArguments(message.Arguments, writer);
        }

        private void WriteStreamInvocationMessage(StreamInvocationMessage message, JsonTextWriter writer)
        {
            WriteInvocationId(message, writer);
            writer.WritePropertyName(TargetPropertyName);
            writer.WriteValue(message.Target);

            WriteArguments(message.Arguments, writer);
        }

        private void WriteCloseMessage(CloseMessage message, JsonTextWriter writer)
        {
            if (!string.IsNullOrEmpty(message.Error))
            {
                writer.WritePropertyName(ErrorPropertyName);
                writer.WriteValue(message.Error);
            }
        }

        private void WriteArguments(object[] arguments, JsonTextWriter writer)
        {
            writer.WritePropertyName(ArgumentsPropertyName);
            writer.WriteStartArray();
            foreach (var argument in arguments)
            {
                PayloadSerializer.Serialize(writer, argument);
            }
            writer.WriteEndArray();
        }

        private static void WriteInvocationId(HubInvocationMessage message, JsonTextWriter writer)
        {
            if (!string.IsNullOrEmpty(message.InvocationId))
            {
                writer.WritePropertyName(InvocationIdPropertyName);
                writer.WriteValue(message.InvocationId);
            }
        }

        private static void WriteMessageType(JsonTextWriter writer, int type)
        {
            writer.WritePropertyName(TypePropertyName);
            writer.WriteValue(type);
        }

        private HubMessage BindCancelInvocationMessage(string invocationId)
        {
            if (string.IsNullOrEmpty(invocationId))
            {
                throw new InvalidDataException($"Missing required property '{InvocationIdPropertyName}'.");
            }

            return new CancelInvocationMessage(invocationId);
        }

        private HubMessage BindCompletionMessage(string invocationId, string error, object result, bool hasResult, IInvocationBinder binder)
        {
            if (string.IsNullOrEmpty(invocationId))
            {
                throw new InvalidDataException($"Missing required property '{InvocationIdPropertyName}'.");
            }

            if (error != null && hasResult)
            {
                throw new InvalidDataException("The 'error' and 'result' properties are mutually exclusive.");
            }

            if (hasResult)
            {
                return new CompletionMessage(invocationId, error, result, hasResult: true);
            }

            return new CompletionMessage(invocationId, error, result: null, hasResult: false);
        }

        private HubMessage BindStreamItemMessage(string invocationId, object item, bool hasItem, IInvocationBinder binder)
        {
            if (string.IsNullOrEmpty(invocationId))
            {
                throw new InvalidDataException($"Missing required property '{InvocationIdPropertyName}'.");
            }

            if (!hasItem)
            {
                throw new InvalidDataException($"Missing required property '{ItemPropertyName}'.");
            }

            return new StreamItemMessage(invocationId, item);
        }

        private HubMessage BindStreamInvocationMessage(string invocationId, string target, ExceptionDispatchInfo argumentBindingException, object[] arguments, bool hasArguments, IInvocationBinder binder)
        {
            if (string.IsNullOrEmpty(invocationId))
            {
                throw new InvalidDataException($"Missing required property '{InvocationIdPropertyName}'.");
            }

            if (!hasArguments)
            {
                throw new InvalidDataException($"Missing required property '{ArgumentsPropertyName}'.");
            }

            if (string.IsNullOrEmpty(target))
            {
                throw new InvalidDataException($"Missing required property '{TargetPropertyName}'.");
            }

            return new StreamInvocationMessage(invocationId, target, argumentBindingException: argumentBindingException, arguments: arguments);
        }

        private HubMessage BindInvocationMessage(string invocationId, string target, ExceptionDispatchInfo argumentBindingException, object[] arguments, bool hasArguments, IInvocationBinder binder)
        {
            if (string.IsNullOrEmpty(target))
            {
                throw new InvalidDataException($"Missing required property '{TargetPropertyName}'.");
            }

            if (!hasArguments)
            {
                throw new InvalidDataException($"Missing required property '{ArgumentsPropertyName}'.");
            }

            return new InvocationMessage(invocationId, target, argumentBindingException: argumentBindingException, arguments: arguments);
        }

        private object[] BindArguments(JsonTextReader reader, IReadOnlyList<Type> paramTypes)
        {
            var arguments = new object[paramTypes.Count];
            var paramIndex = 0;
            var argumentsCount = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndArray)
                {
                    if (argumentsCount != paramTypes.Count)
                    {
                        throw new InvalidDataException($"Invocation provides {argumentsCount} argument(s) but target expects {paramTypes.Count}.");
                    }

                    return arguments;
                }

                try
                {
                    if (paramIndex < paramTypes.Count)
                    {
                        // Set all known arguments
                        arguments[paramIndex] = PayloadSerializer.Deserialize(reader, paramTypes[paramIndex]);
                    }

                    argumentsCount++;
                    paramIndex++;
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException("Error binding arguments. Make sure that the types of the provided values match the types of the hub method being invoked.", ex);
                }
            }

            throw new JsonReaderException("Unexpected end when reading JSON");
        }

        private CloseMessage BindCloseMessage(string error)
        {
            if (string.IsNullOrEmpty(error))
            {
                return CloseMessage.Empty;
            }

            var message = new CloseMessage(error);
            return message;
        }

        private object[] BindArguments(JArray args, IReadOnlyList<Type> paramTypes)
        {
            var arguments = new object[args.Count];
            if (paramTypes.Count != arguments.Length)
            {
                throw new InvalidDataException($"Invocation provides {arguments.Length} argument(s) but target expects {paramTypes.Count}.");
            }

            try
            {
                for (var i = 0; i < paramTypes.Count; i++)
                {
                    var paramType = paramTypes[i];
                    arguments[i] = args[i].ToObject(paramType, PayloadSerializer);
                }

                return arguments;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Error binding arguments. Make sure that the types of the provided values match the types of the hub method being invoked.", ex);
            }
        }

        private HubMessage ApplyHeaders(HubMessage message, Dictionary<string, string> headers)
        {
            if (headers != null && message is HubInvocationMessage invocationMessage)
            {
                invocationMessage.Headers = headers;
            }

            return message;
        }

        internal static JsonSerializerSettings CreateDefaultSerializerSettings()
        {
            return new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
        }
    }
}
