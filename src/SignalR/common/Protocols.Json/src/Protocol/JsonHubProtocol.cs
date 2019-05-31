// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR.Protocol
{
    /// <summary>
    /// Implements the SignalR Hub Protocol using System.Text.Json.
    /// </summary>
    public sealed class JsonHubProtocol : IHubProtocol
    {
        private const string ResultPropertyName = "result";
        private static JsonEncodedText ResultPropertyNameBytes = JsonEncodedText.Encode(ResultPropertyName);
        private const string ItemPropertyName = "item";
        private static JsonEncodedText ItemPropertyNameBytes = JsonEncodedText.Encode(ItemPropertyName);
        private const string InvocationIdPropertyName = "invocationId";
        private static JsonEncodedText InvocationIdPropertyNameBytes = JsonEncodedText.Encode(InvocationIdPropertyName);
        private const string StreamIdsPropertyName = "streamIds";
        private static JsonEncodedText StreamIdsPropertyNameBytes = JsonEncodedText.Encode(StreamIdsPropertyName);
        private const string TypePropertyName = "type";
        private static JsonEncodedText TypePropertyNameBytes = JsonEncodedText.Encode(TypePropertyName);
        private const string ErrorPropertyName = "error";
        private static JsonEncodedText ErrorPropertyNameBytes = JsonEncodedText.Encode(ErrorPropertyName);
        private const string TargetPropertyName = "target";
        private static JsonEncodedText TargetPropertyNameBytes = JsonEncodedText.Encode(TargetPropertyName);
        private const string ArgumentsPropertyName = "arguments";
        private static JsonEncodedText ArgumentsPropertyNameBytes = JsonEncodedText.Encode(ArgumentsPropertyName);
        private const string HeadersPropertyName = "headers";
        private static JsonEncodedText HeadersPropertyNameBytes = JsonEncodedText.Encode(HeadersPropertyName);

        private static readonly string ProtocolName = "json";
        private static readonly int ProtocolVersion = 1;

        /// <summary>
        /// Gets the serializer used to serialize invocation arguments and return values.
        /// </summary>
        private readonly JsonSerializerOptions _payloadSerializerOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonHubProtocol"/> class.
        /// </summary>
        public JsonHubProtocol() : this(Options.Create(new JsonHubProtocolOptions()))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonHubProtocol"/> class.
        /// </summary>
        /// <param name="options">The options used to initialize the protocol.</param>
        public JsonHubProtocol(IOptions<JsonHubProtocolOptions> options)
        {
            _payloadSerializerOptions = options.Value.PayloadSerializerOptions;
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

            message = ParseMessage(payload, binder);

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

        private HubMessage ParseMessage(ReadOnlySequence<byte> input, IInvocationBinder binder)
        {
            try
            {
                // We parse using the Utf8JsonReader directly but this has a problem. Some of our properties are dependent on other properties
                // and since reading the json might be unordered, we need to store the parsed content as JsonDocument to re-parse when true types are known.
                // if we're lucky and the state we need to directly parse is available, then we'll use it.

                int? type = null;
                string invocationId = null;
                string target = null;
                string error = null;
                var hasItem = false;
                object item = null;
                var hasResult = false;
                object result = null;
                var hasArguments = false;
                object[] arguments = null;
                string[] streamIds = null;
                JsonDocument argumentsToken = null;
                JsonDocument itemsToken = null;
                JsonDocument resultToken = null;
                ExceptionDispatchInfo argumentBindingException = null;
                Dictionary<string, string> headers = null;
                var completed = false;

                var reader = new Utf8JsonReader(input, isFinalBlock: true, state: default);

                reader.CheckRead();

                // We're always parsing a JSON object
                reader.EnsureObjectStart();

                do
                {
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.PropertyName:
                            if (reader.TextEquals(TypePropertyNameBytes.EncodedUtf8Bytes))
                            {
                                type = reader.ReadAsInt32(TypePropertyName);

                                if (type == null)
                                {
                                    throw new InvalidDataException($"Expected '{TypePropertyName}' to be of type {JsonTokenType.Number}.");
                                }
                            }
                            else if (reader.TextEquals(InvocationIdPropertyNameBytes.EncodedUtf8Bytes))
                            {
                                invocationId = reader.ReadAsString(InvocationIdPropertyName);
                            }
                            else if (reader.TextEquals(StreamIdsPropertyNameBytes.EncodedUtf8Bytes))
                            {
                                reader.CheckRead();

                                if (reader.TokenType != JsonTokenType.StartArray)
                                {
                                    throw new InvalidDataException(
                                        $"Expected '{StreamIdsPropertyName}' to be of type {SystemTextJsonExtensions.GetTokenString(JsonTokenType.StartArray)}.");
                                }

                                var newStreamIds = new List<string>();
                                reader.Read();
                                while (reader.TokenType != JsonTokenType.EndArray)
                                {
                                    newStreamIds.Add(reader.GetString());
                                    reader.Read();
                                }

                                streamIds = newStreamIds.ToArray();
                            }
                            else if (reader.TextEquals(TargetPropertyNameBytes.EncodedUtf8Bytes))
                            {
                                target = reader.ReadAsString(TargetPropertyName);
                            }
                            else if (reader.TextEquals(ErrorPropertyNameBytes.EncodedUtf8Bytes))
                            {
                                error = reader.ReadAsString(ErrorPropertyName);
                            }
                            else if (reader.TextEquals(ResultPropertyNameBytes.EncodedUtf8Bytes))
                            {
                                hasResult = true;

                                reader.CheckRead();

                                if (string.IsNullOrEmpty(invocationId))
                                {
                                    // If we don't have an invocation id then we need to store it as a JsonDocument so we can parse it later
                                    resultToken = JsonDocument.ParseValue(ref reader);
                                }
                                else
                                {
                                    // If we have an invocation id already we can parse the end result
                                    var returnType = binder.GetReturnType(invocationId);
                                    using var token = JsonDocument.ParseValue(ref reader);
                                    result = BindType(token.RootElement, returnType);
                                }
                            }
                            else if (reader.TextEquals(ItemPropertyNameBytes.EncodedUtf8Bytes))
                            {
                                reader.CheckRead();

                                hasItem = true;

                                string id = null;
                                if (!string.IsNullOrEmpty(invocationId))
                                {
                                    id = invocationId;
                                }
                                else
                                {
                                    // If we don't have an id yet then we need to store it as a JsonDocument to parse later
                                    itemsToken = JsonDocument.ParseValue(ref reader);
                                    continue;
                                }

                                try
                                {
                                    var itemType = binder.GetStreamItemType(id);
                                    using var token = JsonDocument.ParseValue(ref reader);
                                    item = BindType(token.RootElement, itemType);
                                }
                                catch (Exception ex)
                                {
                                    return new StreamBindingFailureMessage(id, ExceptionDispatchInfo.Capture(ex));
                                }
                            }
                            else if (reader.TextEquals(ArgumentsPropertyNameBytes.EncodedUtf8Bytes))
                            {
                                reader.CheckRead();

                                int initialDepth = reader.CurrentDepth;
                                if (reader.TokenType != JsonTokenType.StartArray)
                                {
                                    throw new InvalidDataException($"Expected '{ArgumentsPropertyName}' to be of type {SystemTextJsonExtensions.GetTokenString(JsonTokenType.StartArray)}.");
                                }

                                hasArguments = true;

                                if (string.IsNullOrEmpty(target))
                                {
                                    // We don't know the method name yet so just store the array in JsonDocument
                                    argumentsToken = JsonDocument.ParseValue(ref reader);
                                }
                                else
                                {
                                    try
                                    {
                                        var paramTypes = binder.GetParameterTypes(target);
                                        using var token = JsonDocument.ParseValue(ref reader);
                                        arguments = BindTypes(token.RootElement, paramTypes);
                                    }
                                    catch (Exception ex)
                                    {
                                        argumentBindingException = ExceptionDispatchInfo.Capture(ex);

                                        // Could be at any point in argument array JSON when an error is thrown
                                        // Read until the end of the argument JSON array
                                        while (reader.CurrentDepth == initialDepth && reader.TokenType == JsonTokenType.StartArray ||
                                                reader.CurrentDepth > initialDepth)
                                        {
                                            reader.CheckRead();
                                        }
                                    }
                                }
                            }
                            else if (reader.TextEquals(HeadersPropertyNameBytes.EncodedUtf8Bytes))
                            {
                                reader.CheckRead();
                                headers = ReadHeaders(ref reader);
                            }
                            else
                            {
                                reader.CheckRead();
                                reader.Skip();
                            }
                            break;
                        case JsonTokenType.EndObject:
                            completed = true;
                            break;
                    }
                }
                while (!completed && reader.CheckRead());

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
                                    arguments = BindTypes(argumentsToken.RootElement, paramTypes);
                                }
                                catch (Exception ex)
                                {
                                    argumentBindingException = ExceptionDispatchInfo.Capture(ex);
                                }
                                finally
                                {
                                    argumentsToken.Dispose();
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
                                    arguments = BindTypes(argumentsToken.RootElement, paramTypes);
                                }
                                catch (Exception ex)
                                {
                                    argumentBindingException = ExceptionDispatchInfo.Capture(ex);
                                }
                                finally
                                {
                                    argumentsToken.Dispose();
                                }
                            }

                            message = argumentBindingException != null
                                ? new InvocationBindingFailureMessage(invocationId, target, argumentBindingException)
                                : BindStreamInvocationMessage(invocationId, target, arguments, hasArguments, streamIds, binder);
                        }
                        break;
                    case HubProtocolConstants.StreamItemMessageType:
                        if (itemsToken != null)
                        {
                            try
                            {
                                var returnType = binder.GetStreamItemType(invocationId);
                                item = BindType(itemsToken.RootElement, returnType);
                            }
                            catch (JsonException ex)
                            {
                                message = new StreamBindingFailureMessage(invocationId, ExceptionDispatchInfo.Capture(ex));
                                break;
                            }
                            finally
                            {
                                itemsToken.Dispose();
                            }
                        }

                        message = BindStreamItemMessage(invocationId, item, hasItem, binder);
                        break;
                    case HubProtocolConstants.CompletionMessageType:
                        if (resultToken != null)
                        {
                            try
                            {
                                var returnType = binder.GetReturnType(invocationId);
                                result = BindType(resultToken.RootElement, returnType);
                            }
                            finally
                            {
                                resultToken.Dispose();
                            }
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
            catch (JsonException jrex)
            {
                throw new InvalidDataException("Error reading JSON.", jrex);
            }
        }

        private Dictionary<string, string> ReadHeaders(ref Utf8JsonReader reader)
        {
            var headers = new Dictionary<string, string>(StringComparer.Ordinal);

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new InvalidDataException($"Expected '{HeadersPropertyName}' to be of type {JsonTokenType.StartObject}.");
            }

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        var propertyName = reader.GetString();

                        reader.CheckRead();

                        if (reader.TokenType != JsonTokenType.String)
                        {
                            throw new InvalidDataException($"Expected header '{propertyName}' to be of type {JsonTokenType.String}.");
                        }

                        headers[propertyName] = reader.GetString();
                        break;
                    case JsonTokenType.Comment:
                        break;
                    case JsonTokenType.EndObject:
                        return headers;
                }
            }

            throw new InvalidDataException("Unexpected end when reading message headers");
        }

        private void WriteMessageCore(HubMessage message, IBufferWriter<byte> stream)
        {
            var reusableWriter = ReusableUtf8JsonWriter.Get(stream);

            try
            {
                var writer = reusableWriter.GetJsonWriter();
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
                Debug.Assert(writer.CurrentDepth == 0);
            }
            finally
            {
                ReusableUtf8JsonWriter.Return(reusableWriter);
            }
        }

        private void WriteHeaders(Utf8JsonWriter writer, HubInvocationMessage message)
        {
            if (message.Headers != null && message.Headers.Count > 0)
            {
                writer.WriteStartObject(HeadersPropertyNameBytes);
                foreach (var value in message.Headers)
                {
                    writer.WriteString(value.Key, value.Value);
                }
                writer.WriteEndObject();
            }
        }

        private void WriteCompletionMessage(CompletionMessage message, Utf8JsonWriter writer)
        {
            WriteInvocationId(message, writer);
            if (!string.IsNullOrEmpty(message.Error))
            {
                writer.WriteString(ErrorPropertyNameBytes, message.Error);
            }
            else if (message.HasResult)
            {
                using var token = GetParsedObject(message.Result, message.Result?.GetType());
                token.RootElement.WriteAsProperty(ResultPropertyNameBytes.EncodedUtf8Bytes, writer);
            }
        }

        private void WriteCancelInvocationMessage(CancelInvocationMessage message, Utf8JsonWriter writer)
        {
            WriteInvocationId(message, writer);
        }

        private void WriteStreamItemMessage(StreamItemMessage message, Utf8JsonWriter writer)
        {
            WriteInvocationId(message, writer);

            using var token = GetParsedObject(message.Item, message.Item?.GetType());
            token.RootElement.WriteAsProperty(ItemPropertyNameBytes.EncodedUtf8Bytes, writer);
        }

        private void WriteInvocationMessage(InvocationMessage message, Utf8JsonWriter writer)
        {
            WriteInvocationId(message, writer);
            writer.WriteString(TargetPropertyNameBytes, message.Target);

            WriteArguments(message.Arguments, writer);

            WriteStreamIds(message.StreamIds, writer);
        }

        private void WriteStreamInvocationMessage(StreamInvocationMessage message, Utf8JsonWriter writer)
        {
            WriteInvocationId(message, writer);
            writer.WriteString(TargetPropertyNameBytes, message.Target);

            WriteArguments(message.Arguments, writer);

            WriteStreamIds(message.StreamIds, writer);
        }

        private void WriteCloseMessage(CloseMessage message, Utf8JsonWriter writer)
        {
            if (message.Error != null)
            {
                writer.WriteString(ErrorPropertyNameBytes, message.Error);
            }
        }

        private void WriteArguments(object[] arguments, Utf8JsonWriter writer)
        {
            writer.WriteStartArray(ArgumentsPropertyNameBytes);
            foreach (var argument in arguments)
            {
                var type = argument?.GetType();
                if (type == typeof(DateTime))
                {
                    writer.WriteStringValue((DateTime)argument);
                }
                else if (type == typeof(DateTimeOffset))
                {
                    writer.WriteStringValue((DateTimeOffset)argument);
                }
                else
                {
                    using var token = GetParsedObject(argument, type);
                    token.RootElement.WriteAsValue(writer);
                }
            }
            writer.WriteEndArray();
        }

        private JsonDocument GetParsedObject(object obj, Type type)
        {
            var bytes = JsonSerializer.ToUtf8Bytes(obj, type, _payloadSerializerOptions);
            var token = JsonDocument.Parse(bytes);
            return token;
        }

        private void WriteStreamIds(string[] streamIds, Utf8JsonWriter writer)
        {
            if (streamIds == null)
            {
                return;
            }

            writer.WriteStartArray(StreamIdsPropertyNameBytes);
            foreach (var streamId in streamIds)
            {
                writer.WriteStringValue(streamId);
            }
            writer.WriteEndArray();
        }

        private static void WriteInvocationId(HubInvocationMessage message, Utf8JsonWriter writer)
        {
            if (!string.IsNullOrEmpty(message.InvocationId))
            {
                writer.WriteString(InvocationIdPropertyNameBytes, message.InvocationId);
            }
        }

        private static void WriteMessageType(Utf8JsonWriter writer, int type)
        {
            writer.WriteNumber(TypePropertyNameBytes, type);
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

        private object BindType(JsonElement jsonObject, Type type)
        {
            if (type == typeof(DateTime))
            {
                return jsonObject.GetDateTime();
            }
            else if (type == typeof(DateTimeOffset))
            {
                return jsonObject.GetDateTimeOffset();
            }

            return JsonSerializer.Parse(jsonObject.GetRawText(), type, _payloadSerializerOptions);
        }

        private object[] BindTypes(JsonElement jsonArray, IReadOnlyList<Type> paramTypes)
        {
            object[] arguments = null;
            var paramIndex = 0;
            var argumentsCount = jsonArray.GetArrayLength();
            var paramCount = paramTypes.Count;

            if (argumentsCount != paramCount)
            {
                throw new InvalidDataException($"Invocation provides {argumentsCount} argument(s) but target expects {paramCount}.");
            }

            foreach (var element in jsonArray.EnumerateArray())
            {
                if (arguments == null)
                {
                    arguments = new object[paramCount];
                }

                try
                {
                    arguments[paramIndex] = BindType(element, paramTypes[paramIndex]);
                    paramIndex++;
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException("Error binding arguments. Make sure that the types of the provided values match the types of the hub method being invoked.", ex);
                }
            }

            return arguments ?? Array.Empty<object>();
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

        private HubMessage ApplyHeaders(HubMessage message, Dictionary<string, string> headers)
        {
            if (headers != null && message is HubInvocationMessage invocationMessage)
            {
                invocationMessage.Headers = headers;
            }

            return message;
        }

        internal static JsonSerializerOptions CreateDefaultSerializerSettings()
        {
            var options = new JsonSerializerOptions();
            options.WriteIndented = false;
            options.ReadCommentHandling = JsonCommentHandling.Disallow;
            options.AllowTrailingCommas = false;
            options.IgnoreNullValues = false;
            options.IgnoreReadOnlyProperties = false;
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.PropertyNameCaseInsensitive = false;
            options.MaxDepth = 64;
            options.DictionaryKeyPolicy = null;
            options.DefaultBufferSize = 16 * 1024;

            return options;
        }
    }
}
