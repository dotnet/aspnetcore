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
    /// <summary>
    /// Implements the SignalR Hub Protocol using Newtonsoft.Json.
    /// </summary>
    public class NewtonsoftJsonHubProtocol : IHubProtocol
    {
        private const string ResultPropertyName = "result";
        private const string ItemPropertyName = "item";
        private const string InvocationIdPropertyName = "invocationId";
        private const string StreamIdsPropertyName = "streamIds";
        private const string TypePropertyName = "type";
        private const string ErrorPropertyName = "error";
        private const string TargetPropertyName = "target";
        private const string ArgumentsPropertyName = "arguments";
        private const string HeadersPropertyName = "headers";
        private const string AllowReconnectPropertyName = "allowReconnect";

        private static readonly string ProtocolName = "json";
        private static readonly int ProtocolVersion = 1;

        /// <summary>
        /// Gets the serializer used to serialize invocation arguments and return values.
        /// </summary>
        public JsonSerializer PayloadSerializer { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NewtonsoftJsonHubProtocol"/> class.
        /// </summary>
        public NewtonsoftJsonHubProtocol() : this(Options.Create(new NewtonsoftJsonHubProtocolOptions()))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NewtonsoftJsonHubProtocol"/> class.
        /// </summary>
        /// <param name="options">The options used to initialize the protocol.</param>
        public NewtonsoftJsonHubProtocol(IOptions<NewtonsoftJsonHubProtocolOptions> options)
        {
            PayloadSerializer = JsonSerializer.Create(options.Value.PayloadSerializerSettings);
        }

        /// <inheritdoc />
        public string Name => ProtocolName;

        /// <inheritdoc />
        public int Version => ProtocolVersion;

        /// <inheritdoc />
        public TransferFormat TransferFormat => TransferFormat.Text;

        /// <inheritdoc />
        public bool IsVersionSupported(int version)
        {
            return version == Version;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void WriteMessage(HubMessage message, IBufferWriter<byte> output)
        {
            WriteMessageCore(message, output);
            TextMessageFormatter.WriteRecordSeparator(output);
        }

        /// <inheritdoc />
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
                string[] streamIds = null;
                JArray argumentsToken = null;
                ExceptionDispatchInfo argumentBindingException = null;
                Dictionary<string, string> headers = null;
                var completed = false;
                var allowReconnect = false;

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
                                    case StreamIdsPropertyName:
                                        JsonUtils.CheckRead(reader);

                                        if (reader.TokenType != JsonToken.StartArray)
                                        {
                                            throw new InvalidDataException($"Expected '{StreamIdsPropertyName}' to be of type {JTokenType.Array}.");
                                        }

                                        var newStreamIds = new List<string>();
                                        reader.Read();
                                        while (reader.TokenType != JsonToken.EndArray)
                                        {
                                            newStreamIds.Add(reader.Value?.ToString());
                                            reader.Read();
                                        }

                                        streamIds = newStreamIds.ToArray();
                                        break;
                                    case TargetPropertyName:
                                        target = JsonUtils.ReadAsString(reader, TargetPropertyName);
                                        break;
                                    case ErrorPropertyName:
                                        error = JsonUtils.ReadAsString(reader, ErrorPropertyName);
                                        break;
                                    case AllowReconnectPropertyName:
                                        allowReconnect = JsonUtils.ReadAsBoolean(reader, AllowReconnectPropertyName);
                                        break;
                                    case ResultPropertyName:
                                        hasResult = true;

                                        if (string.IsNullOrEmpty(invocationId))
                                        {
                                            JsonUtils.CheckRead(reader);

                                            // If we don't have an invocation id then we need to store it as a JToken so we can parse it later
                                            resultToken = JToken.Load(reader);
                                        }
                                        else
                                        {
                                            // If we have an invocation id already we can parse the end result
                                            var returnType = binder.GetReturnType(invocationId);

                                            if (!JsonUtils.ReadForType(reader, returnType))
                                            {
                                                throw new JsonReaderException("Unexpected end when reading JSON");
                                            }

                                            result = PayloadSerializer.Deserialize(reader, returnType);
                                        }
                                        break;
                                    case ItemPropertyName:
                                        JsonUtils.CheckRead(reader);

                                        hasItem = true;


                                        string id = null;
                                        if (!string.IsNullOrEmpty(invocationId))
                                        {
                                            id = invocationId;
                                        }
                                        else
                                        {
                                            // If we don't have an id yet then we need to store it as a JToken to parse later
                                            itemToken = JToken.Load(reader);
                                            break;
                                        }

                                        try
                                        {
                                            var itemType = binder.GetStreamItemType(id);
                                            item = PayloadSerializer.Deserialize(reader, itemType);
                                        }
                                        catch (Exception ex)
                                        {
                                            return new StreamBindingFailureMessage(id, ExceptionDispatchInfo.Capture(ex));
                                        }
                                        break;
                                    case ArgumentsPropertyName:
                                        JsonUtils.CheckRead(reader);

                                        int initialDepth = reader.Depth;
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

                                                // Could be at any point in argument array JSON when an error is thrown
                                                // Read until the end of the argument JSON array
                                                while (reader.Depth == initialDepth && reader.TokenType == JsonToken.StartArray ||
                                                       reader.Depth > initialDepth)
                                                {
                                                    JsonUtils.CheckRead(reader);
                                                }
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
                                // We weren't able to bind the arguments because they came before the 'target', so try to bind now that we've read everything.
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

                            message = argumentBindingException != null
                                ? new InvocationBindingFailureMessage(invocationId, target, argumentBindingException)
                                : BindInvocationMessage(invocationId, target, arguments, hasArguments, streamIds, binder);
                        }
                        break;
                    case HubProtocolConstants.StreamInvocationMessageType:
                        {
                            if (argumentsToken != null)
                            {
                                // We weren't able to bind the arguments because they came before the 'target', so try to bind now that we've read everything.
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

                            message = argumentBindingException != null
                                ? new InvocationBindingFailureMessage(invocationId, target, argumentBindingException)
                                : BindStreamInvocationMessage(invocationId, target, arguments, hasArguments, streamIds, binder);
                        }
                        break;
                    case HubProtocolConstants.StreamItemMessageType:
                        if (itemToken != null)
                        {
                            try
                            {
                                var itemType = binder.GetStreamItemType(invocationId);
                                item = itemToken.ToObject(itemType, PayloadSerializer);
                            }
                            catch (Exception ex)
                            {
                                message =  new StreamBindingFailureMessage(invocationId, ExceptionDispatchInfo.Capture(ex));
                                break;
                            };
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
                        return BindCloseMessage(error, allowReconnect);
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

            WriteStreamIds(message.StreamIds, writer);
        }

        private void WriteStreamInvocationMessage(StreamInvocationMessage message, JsonTextWriter writer)
        {
            WriteInvocationId(message, writer);
            writer.WritePropertyName(TargetPropertyName);
            writer.WriteValue(message.Target);

            WriteArguments(message.Arguments, writer);

            WriteStreamIds(message.StreamIds, writer);
        }

        private void WriteCloseMessage(CloseMessage message, JsonTextWriter writer)
        {
            if (message.Error != null)
            {
                writer.WritePropertyName(ErrorPropertyName);
                writer.WriteValue(message.Error);
            }

            if (message.AllowReconnect)
            {
                writer.WritePropertyName(AllowReconnectPropertyName);
                writer.WriteValue(true);
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

        private void WriteStreamIds(string[] streamIds, JsonTextWriter writer)
        {
            if (streamIds == null)
            {
                return;
            }

            writer.WritePropertyName(StreamIdsPropertyName);
            writer.WriteStartArray();
            foreach (var streamId in streamIds)
            {
                writer.WriteValue(streamId);
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

        private HubMessage BindStreamInvocationMessage(string invocationId, string target, object[] arguments, bool hasArguments, string[] streamIds, IInvocationBinder binder)
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

            return new StreamInvocationMessage(invocationId, target, arguments, streamIds);
        }

        private HubMessage BindInvocationMessage(string invocationId, string target, object[] arguments, bool hasArguments, string[] streamIds, IInvocationBinder binder)
        {
            if (string.IsNullOrEmpty(target))
            {
                throw new InvalidDataException($"Missing required property '{TargetPropertyName}'.");
            }

            if (!hasArguments)
            {
                throw new InvalidDataException($"Missing required property '{ArgumentsPropertyName}'.");
            }

            return new InvocationMessage(invocationId, target, arguments, streamIds);
        }

        private bool ReadArgumentAsType(JsonTextReader reader, IReadOnlyList<Type> paramTypes, int paramIndex)
        {
            if (paramIndex < paramTypes.Count)
            {
                var paramType = paramTypes[paramIndex];

                return JsonUtils.ReadForType(reader, paramType);
            }

            return reader.Read();
        }

        private object[] BindArguments(JsonTextReader reader, IReadOnlyList<Type> paramTypes)
        {
            object[] arguments = null;
            var paramIndex = 0;
            var argumentsCount = 0;
            var paramCount = paramTypes.Count;

            while (ReadArgumentAsType(reader, paramTypes, paramIndex))
            {
                if (reader.TokenType == JsonToken.EndArray)
                {
                    if (argumentsCount != paramCount)
                    {
                        throw new InvalidDataException($"Invocation provides {argumentsCount} argument(s) but target expects {paramCount}.");
                    }

                    return arguments ?? Array.Empty<object>();
                }

                if (arguments == null)
                {
                    arguments = new object[paramCount];
                }

                try
                {
                    if (paramIndex < paramCount)
                    {
                        arguments[paramIndex] = PayloadSerializer.Deserialize(reader, paramTypes[paramIndex]);
                    }
                    else
                    {
                        reader.Skip();
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

        private CloseMessage BindCloseMessage(string error, bool allowReconnect)
        {
            // An empty string is still an error
            if (error == null && !allowReconnect)
            {
                return CloseMessage.Empty;
            }

            return new CloseMessage(error, allowReconnect);
        }

        private object[] BindArguments(JArray args, IReadOnlyList<Type> paramTypes)
        {
            var paramCount = paramTypes.Count;
            var argCount = args.Count;
            if (paramCount != argCount)
            {
                throw new InvalidDataException($"Invocation provides {argCount} argument(s) but target expects {paramCount}.");
            }

            if (paramCount == 0)
            {
                return Array.Empty<object>();
            }

            var arguments = new object[argCount];

            try
            {
                for (var i = 0; i < paramCount; i++)
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
