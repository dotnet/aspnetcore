// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using MessagePack;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.Components.Server.BlazorPack
{
    /// <summary>
    /// Implements the SignalR Hub Protocol using MessagePack with limited type support.
    /// </summary>
    [NonDefaultHubProtocol]
    internal sealed class BlazorPackHubProtocol : IHubProtocol
    {
        internal const string ProtocolName = "blazorpack";
        private const int ErrorResult = 1;
        private const int VoidResult = 2;
        private const int NonVoidResult = 3;

        private static readonly int ProtocolVersion = 1;

        /// <inheritdoc />
        public string Name => ProtocolName;

        /// <inheritdoc />
        public int Version => ProtocolVersion;

        /// <inheritdoc />
        public TransferFormat TransferFormat => TransferFormat.Binary;

        /// <inheritdoc />
        public bool IsVersionSupported(int version)
        {
            return version == Version;
        }

        /// <inheritdoc />
        public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, out HubMessage message)
        {
            if (!BinaryMessageParser.TryParseMessage(ref input, out var payload))
            {
                message = null;
                return false;
            }

            var reader = new MessagePackReader(payload);

            var itemCount = reader.ReadArrayHeader();
            var messageType = ReadInt32(ref reader, "messageType");

            switch (messageType)
            {
                case HubProtocolConstants.InvocationMessageType:
                    message = CreateInvocationMessage(ref reader, binder, itemCount);
                    return true;
                case HubProtocolConstants.StreamInvocationMessageType:
                    message = CreateStreamInvocationMessage(ref reader, binder, itemCount);
                    return true;
                case HubProtocolConstants.StreamItemMessageType:
                    message = CreateStreamItemMessage(ref reader, binder);
                    return true;
                case HubProtocolConstants.CompletionMessageType:
                    message = CreateCompletionMessage(ref reader, binder);
                    return true;
                case HubProtocolConstants.CancelInvocationMessageType:
                    message = CreateCancelInvocationMessage(ref reader);
                    return true;
                case HubProtocolConstants.PingMessageType:
                    message = PingMessage.Instance;
                    return true;
                case HubProtocolConstants.CloseMessageType:
                    message = CreateCloseMessage(ref reader, itemCount);
                    return true;
                default:
                    // Future protocol changes can add message types, old clients can ignore them
                    message = null;
                    return false;
            }
        }

        private static HubMessage CreateInvocationMessage(ref MessagePackReader reader, IInvocationBinder binder, int itemCount)
        {
            var headers = ReadHeaders(ref reader);
            var invocationId = ReadString(ref reader, "invocationId");

            // For MsgPack, we represent an empty invocation ID as an empty string,
            // so we need to normalize that to "null", which is what indicates a non-blocking invocation.
            if (string.IsNullOrEmpty(invocationId))
            {
                invocationId = null;
            }

            var target = ReadString(ref reader, "target");

            object[] arguments;
            try
            {
                var parameterTypes = binder.GetParameterTypes(target);
                arguments = BindArguments(ref reader, parameterTypes);
            }
            catch (Exception ex)
            {
                return new InvocationBindingFailureMessage(invocationId, target, ExceptionDispatchInfo.Capture(ex));
            }

            string[] streams = null;
            // Previous clients will send 5 items, so we check if they sent a stream array or not
            if (itemCount > 5)
            {
                streams = ReadStreamIds(ref reader);
            }

            return ApplyHeaders(headers, new InvocationMessage(invocationId, target, arguments, streams));
        }

        private static HubMessage CreateStreamInvocationMessage(ref MessagePackReader reader, IInvocationBinder binder, int itemCount)
        {
            var headers = ReadHeaders(ref reader);
            var invocationId = ReadString(ref reader, "invocationId");
            var target = ReadString(ref reader, "target"); ;

            object[] arguments;
            try
            {
                var parameterTypes = binder.GetParameterTypes(target);
                arguments = BindArguments(ref reader, parameterTypes);
            }
            catch (Exception ex)
            {
                return new InvocationBindingFailureMessage(invocationId, target, ExceptionDispatchInfo.Capture(ex));
            }

            string[] streams = null;
            // Previous clients will send 5 items, so we check if they sent a stream array or not
            if (itemCount > 5)
            {
                streams = ReadStreamIds(ref reader);
            }

            return ApplyHeaders(headers, new StreamInvocationMessage(invocationId, target, arguments, streams));
        }

        private static StreamItemMessage CreateStreamItemMessage(ref MessagePackReader reader, IInvocationBinder binder)
        {
            var headers = ReadHeaders(ref reader);
            var invocationId = ReadString(ref reader, "invocationId");

            var itemType = binder.GetStreamItemType(invocationId);
            var value = DeserializeObject(ref reader, itemType, "item");
            return ApplyHeaders(headers, new StreamItemMessage(invocationId, value));
        }

        private static CompletionMessage CreateCompletionMessage(ref MessagePackReader reader, IInvocationBinder binder)
        {
            var headers = ReadHeaders(ref reader);
            var invocationId = ReadString(ref reader, "invocationId");
            var resultKind = ReadInt32(ref reader, "resultKind");

            string error = null;
            object result = null;
            var hasResult = false;

            switch (resultKind)
            {
                case ErrorResult:
                    error = ReadString(ref reader, "error");
                    break;
                case NonVoidResult:
                    var itemType = binder.GetReturnType(invocationId);
                    result = DeserializeObject(ref reader, itemType, "argument");
                    hasResult = true;
                    break;
                case VoidResult:
                    hasResult = false;
                    break;
                default:
                    throw new InvalidDataException("Invalid invocation result kind.");
            }

            return ApplyHeaders(headers, new CompletionMessage(invocationId, error, result, hasResult));
        }

        private static CancelInvocationMessage CreateCancelInvocationMessage(ref MessagePackReader reader)
        {
            var headers = ReadHeaders(ref reader);
            var invocationId = ReadString(ref reader, "invocationId");
            return ApplyHeaders(headers, new CancelInvocationMessage(invocationId));
        }

        private static CloseMessage CreateCloseMessage(ref MessagePackReader reader, int itemCount)
        {
            var error = ReadString(ref reader, "error");
            var allowReconnect = false;

            if (itemCount > 2)
            {
                allowReconnect = ReadBoolean(ref reader, "allowReconnect");
            }

            // An empty string is still an error
            if (error == null && !allowReconnect)
            {
                return CloseMessage.Empty;
            }

            return new CloseMessage(error, allowReconnect);
        }

        private static Dictionary<string, string> ReadHeaders(ref MessagePackReader reader)
        {
            var headerCount = ReadMapHeader(ref reader, "headers");
            if (headerCount == 0)
            {
                return null;
            }

            var headers = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = 0; i < headerCount; i++)
            {
                var key = ReadString(ref reader, $"headers[{i}].Key");
                var value = ReadString(ref reader, $"headers[{i}].Value");

                headers[key] = value;
            }

            return headers;
        }

        private static string[] ReadStreamIds(ref MessagePackReader reader)
        {
            var streamIdCount = ReadArrayHeader(ref reader, "streamIds");

            if (streamIdCount == 0)
            {
                return null;
            }

            var streams = new List<string>();
            for (var i = 0; i < streamIdCount; i++)
            {
                streams.Add(reader.ReadString());
            }

            return streams.ToArray();
        }

        private static object[] BindArguments(ref MessagePackReader reader, IReadOnlyList<Type> parameterTypes)
        {
            var argumentCount = ReadArrayHeader(ref reader, "arguments");

            if (parameterTypes.Count != argumentCount)
            {
                throw new InvalidDataException(
                    $"Invocation provides {argumentCount} argument(s) but target expects {parameterTypes.Count}.");
            }

            try
            {
                var arguments = new object[argumentCount];
                for (var i = 0; i < argumentCount; i++)
                {
                    arguments[i] = DeserializeObject(ref reader, parameterTypes[i], "argument");
                }

                return arguments;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Error binding arguments. Make sure that the types of the provided values match the types of the hub method being invoked.", ex);
            }
        }

        /// <inheritdoc />
        public void WriteMessage(HubMessage message, IBufferWriter<byte> output)
        {
            var writer = MemoryBufferWriter.Get();

            try
            {
                // Write message to a buffer so we can get its length
                WriteMessageCore(message, writer);

                // Write length then message to output
                BinaryMessageFormatter.WriteLengthPrefix(writer.Length, output);
                writer.CopyTo(output);
            }
            finally
            {
                MemoryBufferWriter.Return(writer);
            }
        }

        ///// <inheritdoc />
        public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message)
        {
            using var writer = new ArrayBufferWriter<byte>();

            // Write message to a buffer so we can get its length
            WriteMessageCore(message, writer);

            var memory = writer.WrittenMemory;

            var dataLength = memory.Length;
            var prefixLength = BinaryMessageFormatter.LengthPrefixLength(dataLength);

            var array = new byte[dataLength + prefixLength];
            var span = array.AsSpan();

            // Write length then message to output
            var written = BinaryMessageFormatter.WriteLengthPrefix(dataLength, span);
            Debug.Assert(written == prefixLength);

            memory.Span.CopyTo(span.Slice(prefixLength));

            return array;
        }

        private void WriteMessageCore(HubMessage message, IBufferWriter<byte> bufferWriter)
        {
            var writer = new MessagePackWriter(bufferWriter);

            switch (message)
            {
                case InvocationMessage invocationMessage:
                    WriteInvocationMessage(invocationMessage, ref writer);
                    break;
                case StreamInvocationMessage streamInvocationMessage:
                    WriteStreamInvocationMessage(streamInvocationMessage, ref writer);
                    break;
                case StreamItemMessage streamItemMessage:
                    WriteStreamingItemMessage(streamItemMessage, ref writer);
                    break;
                case CompletionMessage completionMessage:
                    WriteCompletionMessage(completionMessage, ref writer);
                    break;
                case CancelInvocationMessage cancelInvocationMessage:
                    WriteCancelInvocationMessage(cancelInvocationMessage, ref writer);
                    break;
                case PingMessage pingMessage:
                    WritePingMessage(pingMessage, ref writer);
                    break;
                case CloseMessage closeMessage:
                    WriteCloseMessage(closeMessage, ref writer);
                    break;
                default:
                    throw new InvalidDataException($"Unexpected message type: {message.GetType().Name}");
            }

            writer.Flush();
        }

        private void WriteInvocationMessage(InvocationMessage message, ref MessagePackWriter writer)
        {
            writer.WriteArrayHeader(6);

            writer.Write(HubProtocolConstants.InvocationMessageType);
            PackHeaders(ref writer, message.Headers);
            if (string.IsNullOrEmpty(message.InvocationId))
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(message.InvocationId);
            }
            writer.Write(message.Target);
            writer.WriteArrayHeader(message.Arguments.Length);
            foreach (var arg in message.Arguments)
            {
                SerializeArgument(ref writer, arg);
            }

            WriteStreamIds(message.StreamIds, ref writer);
        }

        private void WriteStreamInvocationMessage(StreamInvocationMessage message, ref MessagePackWriter writer)
        {
            writer.WriteArrayHeader(6);

            writer.Write(HubProtocolConstants.StreamInvocationMessageType);
            PackHeaders(ref writer, message.Headers);
            writer.Write(message.InvocationId);
            writer.Write(message.Target);

            writer.WriteArrayHeader(message.Arguments.Length);
            foreach (var arg in message.Arguments)
            {
                SerializeArgument(ref writer, arg);
            }

            WriteStreamIds(message.StreamIds, ref writer);
        }

        private void WriteStreamingItemMessage(StreamItemMessage message, ref MessagePackWriter writer)
        {
            writer.WriteArrayHeader(4);
            writer.Write(HubProtocolConstants.StreamItemMessageType);
            PackHeaders(ref writer, message.Headers);
            writer.Write(message.InvocationId);
            SerializeArgument(ref writer, message.Item);
        }

        private void SerializeArgument(ref MessagePackWriter writer, object argument)
        {
            switch (argument)
            {
                case null:
                    writer.WriteNil();
                    break;

                case bool boolValue:
                    writer.Write(boolValue);
                    break;

                case string stringValue:
                    writer.Write(stringValue);
                    break;

                case int intValue:
                    writer.Write(intValue);
                    break;

                case long longValue:
                    writer.Write(longValue);
                    break;

                case float floatValue:
                    writer.Write(floatValue);
                    break;

                case ArraySegment<byte> bytes:
                    writer.Write(bytes);
                    break;

                default:
                    throw new FormatException($"Unsupported argument type {argument.GetType()}");
            }
        }

        private static object DeserializeObject(ref MessagePackReader reader, Type type, string field)
        {
            try
            {
                if (type == typeof(string))
                {
                    return ReadString(ref reader, "argument");
                }
                else if (type == typeof(bool))
                {
                    return reader.ReadBoolean();
                }
                else if (type == typeof(int))
                {
                    return reader.ReadInt32();
                }
                else if (type == typeof(long))
                {
                    return reader.ReadInt64();
                }
                else if (type == typeof(float))
                {
                    return reader.ReadSingle();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Deserializing object of the `{type.Name}` type for '{field}' failed.", ex);
            }

            throw new FormatException($"Type {type} is not supported");
        }

        private void WriteStreamIds(string[] streamIds, ref MessagePackWriter writer)
        {
            if (streamIds != null)
            {
                writer.WriteArrayHeader(streamIds.Length);
                foreach (var streamId in streamIds)
                {
                    writer.Write(streamId);
                }
            }
            else
            {
                writer.WriteArrayHeader(0);
            }
        }

        private void WriteCompletionMessage(CompletionMessage message, ref MessagePackWriter writer)
        {
            var resultKind =
                message.Error != null ? ErrorResult :
                message.HasResult ? NonVoidResult :
                VoidResult;

            writer.WriteArrayHeader(4 + (resultKind != VoidResult ? 1 : 0));
            writer.Write(HubProtocolConstants.CompletionMessageType);
            PackHeaders(ref writer, message.Headers);
            writer.Write(message.InvocationId);
            writer.Write(resultKind);
            switch (resultKind)
            {
                case ErrorResult:
                    writer.Write(message.Error);
                    break;
                case NonVoidResult:
                    SerializeArgument(ref writer, message.Result);
                    break;
            }
        }

        private void WriteCancelInvocationMessage(CancelInvocationMessage message, ref MessagePackWriter writer)
        {
            writer.WriteArrayHeader(3);
            writer.Write(HubProtocolConstants.CancelInvocationMessageType);
            PackHeaders(ref writer, message.Headers);
            writer.Write(message.InvocationId);
        }

        private void WriteCloseMessage(CloseMessage message, ref MessagePackWriter writer)
        {
            writer.WriteArrayHeader(3);
            writer.Write(HubProtocolConstants.CloseMessageType);
            if (string.IsNullOrEmpty(message.Error))
            {
                writer.WriteNil();
            }
            else
            {
                writer.Write(message.Error);
            }

            writer.Write(message.AllowReconnect);
        }

        private void WritePingMessage(PingMessage _, ref MessagePackWriter writer)
        {
            writer.WriteArrayHeader(1);
            writer.Write(HubProtocolConstants.PingMessageType);
        }

        private void PackHeaders(ref MessagePackWriter writer, IDictionary<string, string> headers)
        {
            if (headers == null)
            {
                writer.WriteMapHeader(0);
                return;
            }

            writer.WriteMapHeader(headers.Count);
            foreach (var header in headers)
            {
                writer.Write(header.Key);
                writer.Write(header.Value);
            }
        }

        private static T ApplyHeaders<T>(IDictionary<string, string> source, T destination) where T : HubInvocationMessage
        {
            if (source != null && source.Count > 0)
            {
                destination.Headers = source;
            }

            return destination;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ReadBoolean(ref MessagePackReader reader, string field)
        {
            if (reader.End || reader.NextMessagePackType != MessagePackType.Boolean)
            {
                ThrowInvalidDataException(field, "Boolean");
            }

            return reader.ReadBoolean();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ReadInt32(ref MessagePackReader reader, string field)
        {
            if (reader.End || reader.NextMessagePackType != MessagePackType.Integer)
            {
                ThrowInvalidDataException(field, "Int32");
            }

            return reader.ReadInt32();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string ReadString(ref MessagePackReader reader, string field)
        {
            if (reader.End)
            {
                ThrowInvalidDataException(field, "String");
            }

            if (reader.IsNil)
            {
                reader.ReadNil();
                return null;
            }
            else if (reader.NextMessagePackType == MessagePackType.String)
            {
                return reader.ReadString();
            }

            ThrowInvalidDataException(field, "String");
            return null; //This should never be reached.
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ReadArrayHeader(ref MessagePackReader reader, string field)
        {
            if (reader.End || reader.NextMessagePackType != MessagePackType.Array)
            {
                ThrowInvalidCollectionLengthException(field, "array");
            }

            return reader.ReadArrayHeader();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ReadMapHeader(ref MessagePackReader reader, string field)
        {
            if (reader.End || reader.NextMessagePackType != MessagePackType.Map)
            {
                ThrowInvalidCollectionLengthException(field, "map");
            }

            return reader.ReadMapHeader();
        }

        private static void ThrowInvalidDataException(string field, string targetType)
        {
            throw new InvalidDataException($"Reading '{field}' as {targetType} failed.");
        }

        private static void ThrowInvalidCollectionLengthException(string field, string collection)
        {
            throw new InvalidDataException($"Reading {collection} length for '{field}' failed.");
        }
    }
}
