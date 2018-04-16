// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.SignalR.Protocol
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
        private const string HeadersPropertyName = "headers";

        private static readonly string ProtocolName = "json";
        private static readonly int ProtocolVersion = 1;

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

        public int Version => ProtocolVersion;

        public TransferFormat TransferFormat => TransferFormat.Text;

        public bool IsVersionSupported(int version)
        {
            return version == Version;
        }

        public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, out HubMessage message)
        {
            if (!TextMessageParser.TryParseMessage(ref input, out var payload))
            {
                message = null;
                return false;
            }

            var textReader = Utf8BufferTextReader.Get(payload);

            try
            {
                message = ParseMessage(textReader, binder);
            }
            finally
            {
                Utf8BufferTextReader.Return(textReader);
            }

            return message != null;
        }

        public void WriteMessage(HubMessage message, IBufferWriter<byte> output)
        {
            WriteMessageCore(message, output);
            TextMessageFormatter.WriteRecordSeparator(output);
        }

        public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message)
        {
            return HubProtocolExtensions.GetMessageBytes(this, message);
        }

        private HubMessage ParseMessage(Utf8BufferTextReader textReader, IInvocationBinder binder)
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
                var hasArguments = false;
                object[] arguments = null;
                JArray argumentsToken = null;
                ExceptionDispatchInfo argumentBindingException = null;
                Dictionary<string, string> headers = null;
                var completed = false;

                using (var reader = JsonUtils.CreateJsonTextReader(textReader))
                {
                    reader.DateParseHandling = DateParseHandling.None;

                    JsonUtils.CheckRead(reader);

                    // We're always parsing a JSON object
                    JsonUtils.EnsureObjectStart(reader);

                    do
                    {
                        switch (reader.TokenType)
                        {
                            case JsonToken.PropertyName:
                                var memberName = reader.Value.ToString();

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
                        // Future protocol changes can add message types, old clients can ignore them
                        return null;
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
            var headers = new Dictionary<string, string>(StringComparer.Ordinal);

            if (reader.TokenType != JsonToken.StartObject)
            {
                throw new InvalidDataException($"Expected '{HeadersPropertyName}' to be of type {JTokenType.Object}.");
            }

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.PropertyName:
                        var propertyName = reader.Value.ToString();

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

        private void WriteMessageCore(HubMessage message, IBufferWriter<byte> stream)
        {
            var textWriter = Utf8BufferTextWriter.Get(stream);
            try
            {
                using (var writer = JsonUtils.CreateJsonTextWriter(textWriter))
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
                    writer.Flush();
                }
            }
            finally
            {
                Utf8BufferTextWriter.Return(textWriter);
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
            if (message.Error != null)
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

            return new StreamInvocationMessage(invocationId, target, argumentBindingException, arguments);
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

            return new InvocationMessage(invocationId, target, argumentBindingException, arguments);
        }

        private object[] BindArguments(JsonTextReader reader, IReadOnlyList<Type> paramTypes)
        {
            object[] arguments = null;
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

                    return arguments ?? Array.Empty<object>();
                }

                if (arguments == null)
                {
                    arguments = new object[paramTypes.Count];
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
            // An empty string is still an error
            if (error == null)
            {
                return CloseMessage.Empty;
            }

            var message = new CloseMessage(error);
            return message;
        }

        private object[] BindArguments(JArray args, IReadOnlyList<Type> paramTypes)
        {
            if (paramTypes.Count != args.Count)
            {
                throw new InvalidDataException($"Invocation provides {args.Count} argument(s) but target expects {paramTypes.Count}.");
            }

            if (paramTypes.Count == 0)
            {
                return Array.Empty<object>();
            }

            var arguments = new object[args.Count];

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
