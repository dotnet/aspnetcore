// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using MessagePack;
using MessagePack.Formatters;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Internal;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR.Protocol
{
    /// <summary>
    /// Implements the SignalR Hub Protocol using MessagePack.
    /// </summary>
    public class MessagePackHubProtocol : IHubProtocol
    {
        private const int ErrorResult = 1;
        private const int VoidResult = 2;
        private const int NonVoidResult = 3;

        private IFormatterResolver _resolver;

        private static readonly string ProtocolName = "messagepack";
        private static readonly int ProtocolVersion = 1;

        /// <inheritdoc />
        public string Name => ProtocolName;

        /// <inheritdoc />
        public int Version => ProtocolVersion;

        /// <inheritdoc />
        public TransferFormat TransferFormat => TransferFormat.Binary;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackHubProtocol"/> class.
        /// </summary>
        public MessagePackHubProtocol()
            : this(Options.Create(new MessagePackHubProtocolOptions()))
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackHubProtocol"/> class.
        /// </summary>
        /// <param name="options">The options used to initialize the protocol.</param>
        public MessagePackHubProtocol(IOptions<MessagePackHubProtocolOptions> options)
        {
            var msgPackOptions = options.Value;
            SetupResolver(msgPackOptions);
        }

        private void SetupResolver(MessagePackHubProtocolOptions options)
        {
            // if counts don't match then we know users customized resolvers so we set up the options
            // with the provided resolvers
            if (options.FormatterResolvers.Count != SignalRResolver.Resolvers.Count)
            {
                _resolver = new CombinedResolvers(options.FormatterResolvers);
                return;
            }

            for (var i = 0; i < options.FormatterResolvers.Count; i++)
            {
                // check if the user customized the resolvers
                if (options.FormatterResolvers[i] != SignalRResolver.Resolvers[i])
                {
                    _resolver = new CombinedResolvers(options.FormatterResolvers);
                    return;
                }
            }

            // Use optimized cached resolver if the default is chosen
            _resolver = SignalRResolver.Instance;
        }

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

            var arraySegment = GetArraySegment(payload);

            message = ParseMessage(arraySegment.Array, arraySegment.Offset, binder, _resolver);
            return true;
        }

        private static ArraySegment<byte> GetArraySegment(in ReadOnlySequence<byte> input)
        {
            if (input.IsSingleSegment)
            {
                var isArray = MemoryMarshal.TryGetArray(input.First, out var arraySegment);
                // This will never be false unless we started using un-managed buffers
                Debug.Assert(isArray);
                return arraySegment;
            }

            // Should be rare
            return new ArraySegment<byte>(input.ToArray());
        }

        private static HubMessage ParseMessage(byte[] input, int startOffset, IInvocationBinder binder, IFormatterResolver resolver)
        {
            var itemCount = MessagePackBinary.ReadArrayHeader(input, startOffset, out var readSize);
            startOffset += readSize;

            var messageType = ReadInt32(input, ref startOffset, "messageType");

            switch (messageType)
            {
                case HubProtocolConstants.InvocationMessageType:
                    return CreateInvocationMessage(input, ref startOffset, binder, resolver, itemCount);
                case HubProtocolConstants.StreamInvocationMessageType:
                    return CreateStreamInvocationMessage(input, ref startOffset, binder, resolver, itemCount);
                case HubProtocolConstants.StreamItemMessageType:
                    return CreateStreamItemMessage(input, ref startOffset, binder, resolver);
                case HubProtocolConstants.CompletionMessageType:
                    return CreateCompletionMessage(input, ref startOffset, binder, resolver);
                case HubProtocolConstants.CancelInvocationMessageType:
                    return CreateCancelInvocationMessage(input, ref startOffset);
                case HubProtocolConstants.PingMessageType:
                    return PingMessage.Instance;
                case HubProtocolConstants.CloseMessageType:
                    return CreateCloseMessage(input, ref startOffset);
                default:
                    // Future protocol changes can add message types, old clients can ignore them
                    return null;
            }
        }

        private static HubMessage CreateInvocationMessage(byte[] input, ref int offset, IInvocationBinder binder, IFormatterResolver resolver, int itemCount)
        {
            var headers = ReadHeaders(input, ref offset);
            var invocationId = ReadInvocationId(input, ref offset);

            // For MsgPack, we represent an empty invocation ID as an empty string,
            // so we need to normalize that to "null", which is what indicates a non-blocking invocation.
            if (string.IsNullOrEmpty(invocationId))
            {
                invocationId = null;
            }

            var target = ReadString(input, ref offset, "target");

            object[] arguments = null;
            try
            {
                var parameterTypes = binder.GetParameterTypes(target);
                arguments = BindArguments(input, ref offset, parameterTypes, resolver);
            }
            catch (Exception ex)
            {
                return new InvocationBindingFailureMessage(invocationId, target, ExceptionDispatchInfo.Capture(ex));
            }

            string[] streams = null;
            // Previous clients will send 5 items, so we check if they sent a stream array or not
            if (itemCount > 5)
            {
                streams = ReadStreamIds(input, ref offset);
            }

            return ApplyHeaders(headers, new InvocationMessage(invocationId, target, arguments, streams));
        }

        private static HubMessage CreateStreamInvocationMessage(byte[] input, ref int offset, IInvocationBinder binder, IFormatterResolver resolver, int itemCount)
        {
            var headers = ReadHeaders(input, ref offset);
            var invocationId = ReadInvocationId(input, ref offset);
            var target = ReadString(input, ref offset, "target");

            object[] arguments = null;
            try
            {
                var parameterTypes = binder.GetParameterTypes(target);
                arguments = BindArguments(input, ref offset, parameterTypes, resolver);
            }
            catch (Exception ex)
            {
                return new InvocationBindingFailureMessage(invocationId, target, ExceptionDispatchInfo.Capture(ex));
            }

            string[] streams = null;
            // Previous clients will send 5 items, so we check if they sent a stream array or not
            if (itemCount > 5)
            {
                streams = ReadStreamIds(input, ref offset);
            }

            return ApplyHeaders(headers, new StreamInvocationMessage(invocationId, target, arguments, streams));
        }

        private static HubMessage CreateStreamItemMessage(byte[] input, ref int offset, IInvocationBinder binder, IFormatterResolver resolver)
        {
            var headers = ReadHeaders(input, ref offset);
            var invocationId = ReadInvocationId(input, ref offset);
            object value;
            try
            {
                var itemType = binder.GetStreamItemType(invocationId);
                value = DeserializeObject(input, ref offset, itemType, "item", resolver);
            }
            catch (Exception ex)
            {
                return new StreamBindingFailureMessage(invocationId, ExceptionDispatchInfo.Capture(ex));
            }

            return ApplyHeaders(headers, new StreamItemMessage(invocationId, value));
        }

        private static CompletionMessage CreateCompletionMessage(byte[] input, ref int offset, IInvocationBinder binder, IFormatterResolver resolver)
        {
            var headers = ReadHeaders(input, ref offset);
            var invocationId = ReadInvocationId(input, ref offset);
            var resultKind = ReadInt32(input, ref offset, "resultKind");

            string error = null;
            object result = null;
            var hasResult = false;

            switch (resultKind)
            {
                case ErrorResult:
                    error = ReadString(input, ref offset, "error");
                    break;
                case NonVoidResult:
                    var itemType = binder.GetReturnType(invocationId);
                    result = DeserializeObject(input, ref offset, itemType, "argument", resolver);
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

        private static CancelInvocationMessage CreateCancelInvocationMessage(byte[] input, ref int offset)
        {
            var headers = ReadHeaders(input, ref offset);
            var invocationId = ReadInvocationId(input, ref offset);
            return ApplyHeaders(headers, new CancelInvocationMessage(invocationId));
        }

        private static CloseMessage CreateCloseMessage(byte[] input, ref int offset)
        {
            var error = ReadString(input, ref offset, "error");
            return new CloseMessage(error);
        }

        private static Dictionary<string, string> ReadHeaders(byte[] input, ref int offset)
        {
            var headerCount = ReadMapLength(input, ref offset, "headers");
            if (headerCount > 0)
            {
                var headers = new Dictionary<string, string>(StringComparer.Ordinal);

                for (var i = 0; i < headerCount; i++)
                {
                    var key = ReadString(input, ref offset, $"headers[{i}].Key");
                    var value = ReadString(input, ref offset, $"headers[{i}].Value");
                    headers.Add(key, value);
                }
                return headers;
            }
            else
            {
                return null;
            }
        }

        private static string[] ReadStreamIds(byte[] input, ref int offset)
        {
            var streamIdCount = ReadArrayLength(input, ref offset, "streamIds");
            List<string> streams = null;

            if (streamIdCount > 0)
            {
                streams = new List<string>();
                for (var i = 0; i < streamIdCount; i++)
                {
                    streams.Add(MessagePackBinary.ReadString(input, offset, out var read));
                    offset += read;
                }
            }

            return streams?.ToArray();
        }

        private static object[] BindArguments(byte[] input, ref int offset, IReadOnlyList<Type> parameterTypes, IFormatterResolver resolver)
        {
            var argumentCount = ReadArrayLength(input, ref offset, "arguments");

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
                    arguments[i] = DeserializeObject(input, ref offset, parameterTypes[i], "argument", resolver);
                }

                return arguments;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Error binding arguments. Make sure that the types of the provided values match the types of the hub method being invoked.", ex);
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

        /// <inheritdoc />
        public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message)
        {
            var writer = MemoryBufferWriter.Get();

            try
            {
                // Write message to a buffer so we can get its length
                WriteMessageCore(message, writer);

                var dataLength = writer.Length;
                var prefixLength = BinaryMessageFormatter.LengthPrefixLength(writer.Length);

                var array = new byte[dataLength + prefixLength];
                var span = array.AsSpan();

                // Write length then message to output
                var written = BinaryMessageFormatter.WriteLengthPrefix(writer.Length, span);
                Debug.Assert(written == prefixLength);
                writer.CopyTo(span.Slice(prefixLength));

                return array;
            }
            finally
            {
                MemoryBufferWriter.Return(writer);
            }
        }

        private void WriteMessageCore(HubMessage message, Stream packer)
        {
            switch (message)
            {
                case InvocationMessage invocationMessage:
                    WriteInvocationMessage(invocationMessage, packer);
                    break;
                case StreamInvocationMessage streamInvocationMessage:
                    WriteStreamInvocationMessage(streamInvocationMessage, packer);
                    break;
                case StreamItemMessage streamItemMessage:
                    WriteStreamingItemMessage(streamItemMessage, packer);
                    break;
                case CompletionMessage completionMessage:
                    WriteCompletionMessage(completionMessage, packer);
                    break;
                case CancelInvocationMessage cancelInvocationMessage:
                    WriteCancelInvocationMessage(cancelInvocationMessage, packer);
                    break;
                case PingMessage pingMessage:
                    WritePingMessage(pingMessage, packer);
                    break;
                case CloseMessage closeMessage:
                    WriteCloseMessage(closeMessage, packer);
                    break;
                default:
                    throw new InvalidDataException($"Unexpected message type: {message.GetType().Name}");
            }
        }

        private void WriteInvocationMessage(InvocationMessage message, Stream packer)
        {
            MessagePackBinary.WriteArrayHeader(packer, 6);

            MessagePackBinary.WriteInt32(packer, HubProtocolConstants.InvocationMessageType);
            PackHeaders(packer, message.Headers);
            if (string.IsNullOrEmpty(message.InvocationId))
            {
                MessagePackBinary.WriteNil(packer);
            }
            else
            {
                MessagePackBinary.WriteString(packer, message.InvocationId);
            }
            MessagePackBinary.WriteString(packer, message.Target);
            MessagePackBinary.WriteArrayHeader(packer, message.Arguments.Length);
            foreach (var arg in message.Arguments)
            {
                WriteArgument(arg, packer);
            }

            WriteStreamIds(message.StreamIds, packer);
        }

        private void WriteStreamInvocationMessage(StreamInvocationMessage message, Stream packer)
        {
            MessagePackBinary.WriteArrayHeader(packer, 6);

            MessagePackBinary.WriteInt16(packer, HubProtocolConstants.StreamInvocationMessageType);
            PackHeaders(packer, message.Headers);
            MessagePackBinary.WriteString(packer, message.InvocationId);
            MessagePackBinary.WriteString(packer, message.Target);

            MessagePackBinary.WriteArrayHeader(packer, message.Arguments.Length);
            foreach (var arg in message.Arguments)
            {
                WriteArgument(arg, packer);
            }

            WriteStreamIds(message.StreamIds, packer);
        }

        private void WriteStreamingItemMessage(StreamItemMessage message, Stream packer)
        {
            MessagePackBinary.WriteArrayHeader(packer, 4);
            MessagePackBinary.WriteInt16(packer, HubProtocolConstants.StreamItemMessageType);
            PackHeaders(packer, message.Headers);
            MessagePackBinary.WriteString(packer, message.InvocationId);
            WriteArgument(message.Item, packer);
        }

        private void WriteArgument(object argument, Stream stream)
        {
            if (argument == null)
            {
                MessagePackBinary.WriteNil(stream);
            }
            else
            {
                MessagePackSerializer.NonGeneric.Serialize(argument.GetType(), stream, argument, _resolver);
            }
        }

        private void WriteStreamIds(string[] streamIds, Stream packer)
        {
            if (streamIds != null)
            {
                MessagePackBinary.WriteArrayHeader(packer, streamIds.Length);
                foreach (var streamId in streamIds)
                {
                    MessagePackBinary.WriteString(packer, streamId);
                }
            }
            else
            {
                MessagePackBinary.WriteArrayHeader(packer, 0);
            }
        }

        private void WriteCompletionMessage(CompletionMessage message, Stream packer)
        {
            var resultKind =
                message.Error != null ? ErrorResult :
                message.HasResult ? NonVoidResult :
                VoidResult;

            MessagePackBinary.WriteArrayHeader(packer, 4 + (resultKind != VoidResult ? 1 : 0));
            MessagePackBinary.WriteInt32(packer, HubProtocolConstants.CompletionMessageType);
            PackHeaders(packer, message.Headers);
            MessagePackBinary.WriteString(packer, message.InvocationId);
            MessagePackBinary.WriteInt32(packer, resultKind);
            switch (resultKind)
            {
                case ErrorResult:
                    MessagePackBinary.WriteString(packer, message.Error);
                    break;
                case NonVoidResult:
                    WriteArgument(message.Result, packer);
                    break;
            }
        }

        private void WriteCancelInvocationMessage(CancelInvocationMessage message, Stream packer)
        {
            MessagePackBinary.WriteArrayHeader(packer, 3);
            MessagePackBinary.WriteInt16(packer, HubProtocolConstants.CancelInvocationMessageType);
            PackHeaders(packer, message.Headers);
            MessagePackBinary.WriteString(packer, message.InvocationId);
        }

        private void WriteCloseMessage(CloseMessage message, Stream packer)
        {
            MessagePackBinary.WriteArrayHeader(packer, 2);
            MessagePackBinary.WriteInt16(packer, HubProtocolConstants.CloseMessageType);
            if (string.IsNullOrEmpty(message.Error))
            {
                MessagePackBinary.WriteNil(packer);
            }
            else
            {
                MessagePackBinary.WriteString(packer, message.Error);
            }
        }

        private void WritePingMessage(PingMessage pingMessage, Stream packer)
        {
            MessagePackBinary.WriteArrayHeader(packer, 1);
            MessagePackBinary.WriteInt32(packer, HubProtocolConstants.PingMessageType);
        }

        private void PackHeaders(Stream packer, IDictionary<string, string> headers)
        {
            if (headers != null)
            {
                MessagePackBinary.WriteMapHeader(packer, headers.Count);
                if (headers.Count > 0)
                {
                    foreach (var header in headers)
                    {
                        MessagePackBinary.WriteString(packer, header.Key);
                        MessagePackBinary.WriteString(packer, header.Value);
                    }
                }
            }
            else
            {
                MessagePackBinary.WriteMapHeader(packer, 0);
            }
        }

        private static string ReadInvocationId(byte[] input, ref int offset)
        {
            return ReadString(input, ref offset, "invocationId");
        }

        private static int ReadInt32(byte[] input, ref int offset, string field)
        {
            Exception msgPackException = null;
            try
            {
                var readInt = MessagePackBinary.ReadInt32(input, offset, out var readSize);
                offset += readSize;
                return readInt;
            }
            catch (Exception e)
            {
                msgPackException = e;
            }

            throw new InvalidDataException($"Reading '{field}' as Int32 failed.", msgPackException);
        }

        private static string ReadString(byte[] input, ref int offset, string field)
        {
            Exception msgPackException = null;
            try
            {
                var readString = MessagePackBinary.ReadString(input, offset, out var readSize);
                offset += readSize;
                return readString;
            }
            catch (Exception e)
            {
                msgPackException = e;
            }

            throw new InvalidDataException($"Reading '{field}' as String failed.", msgPackException);
        }

        private static long ReadMapLength(byte[] input, ref int offset, string field)
        {
            Exception msgPackException = null;
            try
            {
                var readMap = MessagePackBinary.ReadMapHeader(input, offset, out var readSize);
                offset += readSize;
                return readMap;
            }
            catch (Exception e)
            {
                msgPackException = e;
            }

            throw new InvalidDataException($"Reading map length for '{field}' failed.", msgPackException);
        }

        private static long ReadArrayLength(byte[] input, ref int offset, string field)
        {
            Exception msgPackException = null;
            try
            {
                var readArray = MessagePackBinary.ReadArrayHeader(input, offset, out var readSize);
                offset += readSize;
                return readArray;
            }
            catch (Exception e)
            {
                msgPackException = e;
            }

            throw new InvalidDataException($"Reading array length for '{field}' failed.", msgPackException);
        }

        private static object DeserializeObject(byte[] input, ref int offset, Type type, string field, IFormatterResolver resolver)
        {
            Exception msgPackException = null;
            try
            {
                var obj = MessagePackSerializer.NonGeneric.Deserialize(type, new ArraySegment<byte>(input, offset, input.Length - offset), resolver);
                offset += MessagePackBinary.ReadNextBlock(input, offset);
                return obj;
            }
            catch (Exception ex)
            {
                msgPackException = ex;
            }

            throw new InvalidDataException($"Deserializing object of the `{type.Name}` type for '{field}' failed.", msgPackException);
        }

        internal static List<IFormatterResolver> CreateDefaultFormatterResolvers()
        {
            // Copy to allow users to add/remove resolvers without changing the static SignalRResolver list
            return new List<IFormatterResolver>(SignalRResolver.Resolvers);
        }

        internal class SignalRResolver : IFormatterResolver
        {
            public static readonly IFormatterResolver Instance = new SignalRResolver();

            public static readonly IList<IFormatterResolver> Resolvers = new[]
            {
                MessagePack.Resolvers.DynamicEnumAsStringResolver.Instance,
                MessagePack.Resolvers.ContractlessStandardResolver.Instance,
            };

            public IMessagePackFormatter<T> GetFormatter<T>()
            {
                return Cache<T>.Formatter;
            }

            private static class Cache<T>
            {
                public static readonly IMessagePackFormatter<T> Formatter;

                static Cache()
                {
                    foreach (var resolver in Resolvers)
                    {
                        Formatter = resolver.GetFormatter<T>();
                        if (Formatter != null)
                        {
                            return;
                        }
                    }
                }
            }
        }

        // Support for users making their own Formatter lists
        internal class CombinedResolvers : IFormatterResolver
        {
            private readonly IList<IFormatterResolver> _resolvers;

            public CombinedResolvers(IList<IFormatterResolver> resolvers)
            {
                _resolvers = resolvers;
            }

            public IMessagePackFormatter<T> GetFormatter<T>()
            {
                foreach (var resolver in _resolvers)
                {
                    var formatter = resolver.GetFormatter<T>();
                    if (formatter != null)
                    {
                        return formatter;
                    }
                }

                return null;
            }
        }
    }
}
