// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable IDE0005 // This file is shared across multiple projects making it ugly to ignore unused usings

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.ExceptionServices;
using MessagePack;
using Microsoft.AspNetCore.Internal;

namespace Microsoft.AspNetCore.SignalR.Protocol;

/// <summary>
/// Implements support for MessagePackHubProtocol. This code is shared between SignalR and Blazor.
/// </summary>
internal abstract class MessagePackHubProtocolWorker
{
    private const int ErrorResult = 1;
    private const int VoidResult = 2;
    private const int NonVoidResult = 3;

    public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, [NotNullWhen(true)] out HubMessage? message)
    {
        if (!BinaryMessageParser.TryParseMessage(ref input, out var payload))
        {
            message = null;
            return false;
        }

        var reader = new MessagePackReader(payload);
        message = ParseMessage(ref reader, binder);
        return message != null;
    }

    private HubMessage? ParseMessage(ref MessagePackReader reader, IInvocationBinder binder)
    {
        var itemCount = reader.ReadArrayHeader();

        var messageType = ReadInt32(ref reader, "messageType");

        switch (messageType)
        {
            case HubProtocolConstants.InvocationMessageType:
                return CreateInvocationMessage(ref reader, binder, itemCount);
            case HubProtocolConstants.StreamInvocationMessageType:
                return CreateStreamInvocationMessage(ref reader, binder, itemCount);
            case HubProtocolConstants.StreamItemMessageType:
                return CreateStreamItemMessage(ref reader, binder);
            case HubProtocolConstants.CompletionMessageType:
                return CreateCompletionMessage(ref reader, binder);
            case HubProtocolConstants.CancelInvocationMessageType:
                return CreateCancelInvocationMessage(ref reader);
            case HubProtocolConstants.PingMessageType:
                return PingMessage.Instance;
            case HubProtocolConstants.CloseMessageType:
                return CreateCloseMessage(ref reader, itemCount);
            default:
                // Future protocol changes can add message types, old clients can ignore them
                return null;
        }
    }

    private HubMessage CreateInvocationMessage(ref MessagePackReader reader, IInvocationBinder binder, int itemCount)
    {
        var headers = ReadHeaders(ref reader);
        var invocationId = ReadInvocationId(ref reader);

        // For MsgPack, we represent an empty invocation ID as an empty string,
        // so we need to normalize that to "null", which is what indicates a non-blocking invocation.
        if (string.IsNullOrEmpty(invocationId))
        {
            invocationId = null;
        }

        var target = ReadString(ref reader, "target");

        object[]? arguments;
        try
        {
            var parameterTypes = binder.GetParameterTypes(target);
            arguments = BindArguments(ref reader, parameterTypes);
        }
        catch (Exception ex)
        {
            return new InvocationBindingFailureMessage(invocationId, target, ExceptionDispatchInfo.Capture(ex));
        }

        string[]? streams = null;
        // Previous clients will send 5 items, so we check if they sent a stream array or not
        if (itemCount > 5)
        {
            streams = ReadStreamIds(ref reader);
        }

        return ApplyHeaders(headers, new InvocationMessage(invocationId, target, arguments, streams));
    }

    private HubMessage CreateStreamInvocationMessage(ref MessagePackReader reader, IInvocationBinder binder, int itemCount)
    {
        var headers = ReadHeaders(ref reader);
        var invocationId = ReadInvocationId(ref reader);
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

        string[]? streams = null;
        // Previous clients will send 5 items, so we check if they sent a stream array or not
        if (itemCount > 5)
        {
            streams = ReadStreamIds(ref reader);
        }

        return ApplyHeaders(headers, new StreamInvocationMessage(invocationId, target, arguments, streams));
    }

    private HubMessage CreateStreamItemMessage(ref MessagePackReader reader, IInvocationBinder binder)
    {
        var headers = ReadHeaders(ref reader);
        var invocationId = ReadInvocationId(ref reader);
        object value;
        try
        {
            var itemType = binder.GetStreamItemType(invocationId);
            value = DeserializeObject(ref reader, itemType, "item");
        }
        catch (Exception ex)
        {
            return new StreamBindingFailureMessage(invocationId, ExceptionDispatchInfo.Capture(ex));
        }

        return ApplyHeaders(headers, new StreamItemMessage(invocationId, value));
    }

    private CompletionMessage CreateCompletionMessage(ref MessagePackReader reader, IInvocationBinder binder)
    {
        var headers = ReadHeaders(ref reader);
        var invocationId = ReadInvocationId(ref reader);
        var resultKind = ReadInt32(ref reader, "resultKind");

        string? error = null;
        object? result = null;
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
        var invocationId = ReadInvocationId(ref reader);
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

    private static Dictionary<string, string>? ReadHeaders(ref MessagePackReader reader)
    {
        var headerCount = ReadMapLength(ref reader, "headers");
        if (headerCount > 0)
        {
            var headers = new Dictionary<string, string>(StringComparer.Ordinal);

            for (var i = 0; i < headerCount; i++)
            {
                var key = ReadString(ref reader, $"headers[{i}].Key");
                var value = ReadString(ref reader, $"headers[{i}].Value");
                headers.Add(key, value);
            }
            return headers;
        }
        else
        {
            return null;
        }
    }

    private static string[]? ReadStreamIds(ref MessagePackReader reader)
    {
        var streamIdCount = ReadArrayLength(ref reader, "streamIds");
        List<string>? streams = null;

        if (streamIdCount > 0)
        {
            streams = new List<string>();
            for (var i = 0; i < streamIdCount; i++)
            {
                streams.Add(reader.ReadString());
            }
        }

        return streams?.ToArray();
    }

    private object[] BindArguments(ref MessagePackReader reader, IReadOnlyList<Type> parameterTypes)
    {
        var argumentCount = ReadArrayLength(ref reader, "arguments");

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

    protected abstract object DeserializeObject(ref MessagePackReader reader, Type type, string field);

    private static T ApplyHeaders<T>(IDictionary<string, string>? source, T destination) where T : HubInvocationMessage
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
        var memoryBufferWriter = MemoryBufferWriter.Get();

        try
        {
            var writer = new MessagePackWriter(memoryBufferWriter);

            // Write message to a buffer so we can get its length
            WriteMessageCore(message, ref writer);

            // Write length then message to output
            BinaryMessageFormatter.WriteLengthPrefix(memoryBufferWriter.Length, output);
            memoryBufferWriter.CopyTo(output);
        }
        finally
        {
            MemoryBufferWriter.Return(memoryBufferWriter);
        }
    }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message)
    {
        var memoryBufferWriter = MemoryBufferWriter.Get();

        try
        {
            var writer = new MessagePackWriter(memoryBufferWriter);

            // Write message to a buffer so we can get its length
            WriteMessageCore(message, ref writer);

            var dataLength = memoryBufferWriter.Length;
            var prefixLength = BinaryMessageFormatter.LengthPrefixLength(memoryBufferWriter.Length);

            var array = new byte[dataLength + prefixLength];
            var span = array.AsSpan();

            // Write length then message to output
            var written = BinaryMessageFormatter.WriteLengthPrefix(memoryBufferWriter.Length, span);
            Debug.Assert(written == prefixLength);
            memoryBufferWriter.CopyTo(span.Slice(prefixLength));

            return array;
        }
        finally
        {
            MemoryBufferWriter.Return(memoryBufferWriter);
        }
    }

    private void WriteMessageCore(HubMessage message, ref MessagePackWriter writer)
    {
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
        PackHeaders(message.Headers, ref writer);
        if (string.IsNullOrEmpty(message.InvocationId))
        {
            writer.WriteNil();
        }
        else
        {
            writer.Write(message.InvocationId);
        }
        writer.Write(message.Target);

        if (message.Arguments is null)
        {
            writer.WriteArrayHeader(0);
        }
        else
        {
            writer.WriteArrayHeader(message.Arguments.Length);
            foreach (var arg in message.Arguments)
            {
                WriteArgument(arg, ref writer);
            }
        }

        WriteStreamIds(message.StreamIds, ref writer);
    }

    private void WriteStreamInvocationMessage(StreamInvocationMessage message, ref MessagePackWriter writer)
    {
        writer.WriteArrayHeader(6);

        writer.Write(HubProtocolConstants.StreamInvocationMessageType);
        PackHeaders(message.Headers, ref writer);
        writer.Write(message.InvocationId);
        writer.Write(message.Target);

        if (message.Arguments is null)
        {
            writer.WriteArrayHeader(0);
        }
        else
        {
            writer.WriteArrayHeader(message.Arguments.Length);
            foreach (var arg in message.Arguments)
            {
                WriteArgument(arg, ref writer);
            }
        }

        WriteStreamIds(message.StreamIds, ref writer);
    }

    private void WriteStreamingItemMessage(StreamItemMessage message, ref MessagePackWriter writer)
    {
        writer.WriteArrayHeader(4);
        writer.Write(HubProtocolConstants.StreamItemMessageType);
        PackHeaders(message.Headers, ref writer);
        writer.Write(message.InvocationId);
        WriteArgument(message.Item, ref writer);
    }

    private void WriteArgument(object? argument, ref MessagePackWriter writer)
    {
        if (argument == null)
        {
            writer.WriteNil();
        }
        else
        {
            Serialize(ref writer, argument.GetType(), argument);
        }
    }

    protected abstract void Serialize(ref MessagePackWriter writer, Type type, object value);

    private static void WriteStreamIds(string[]? streamIds, ref MessagePackWriter writer)
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
        PackHeaders(message.Headers, ref writer);
        writer.Write(message.InvocationId);
        writer.Write(resultKind);
        switch (resultKind)
        {
            case ErrorResult:
                writer.Write(message.Error);
                break;
            case NonVoidResult:
                WriteArgument(message.Result, ref writer);
                break;
        }
    }

    private static void WriteCancelInvocationMessage(CancelInvocationMessage message, ref MessagePackWriter writer)
    {
        writer.WriteArrayHeader(3);
        writer.Write(HubProtocolConstants.CancelInvocationMessageType);
        PackHeaders(message.Headers, ref writer);
        writer.Write(message.InvocationId);
    }

    private static void WriteCloseMessage(CloseMessage message, ref MessagePackWriter writer)
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

    private static void WritePingMessage(PingMessage pingMessage, ref MessagePackWriter writer)
    {
        writer.WriteArrayHeader(1);
        writer.Write(HubProtocolConstants.PingMessageType);
    }

    private static void PackHeaders(IDictionary<string, string>? headers, ref MessagePackWriter writer)
    {
        if (headers != null)
        {
            writer.WriteMapHeader(headers.Count);
            if (headers.Count > 0)
            {
                foreach (var header in headers)
                {
                    writer.Write(header.Key);
                    writer.Write(header.Value);
                }
            }
        }
        else
        {
            writer.WriteMapHeader(0);
        }
    }

    private static string ReadInvocationId(ref MessagePackReader reader) =>
        ReadString(ref reader, "invocationId");

    private static bool ReadBoolean(ref MessagePackReader reader, string field)
    {
        try
        {
            return reader.ReadBoolean();
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Reading '{field}' as Boolean failed.", ex);
        }
    }

    private static int ReadInt32(ref MessagePackReader reader, string field)
    {
        try
        {
            return reader.ReadInt32();
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Reading '{field}' as Int32 failed.", ex);
        }
    }

    protected static string ReadString(ref MessagePackReader reader, string field)
    {
        try
        {
            return reader.ReadString();
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Reading '{field}' as String failed.", ex);
        }
    }

    private static long ReadMapLength(ref MessagePackReader reader, string field)
    {
        try
        {
            return reader.ReadMapHeader();
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Reading map length for '{field}' failed.", ex);
        }
    }

    private static long ReadArrayLength(ref MessagePackReader reader, string field)
    {
        try
        {
            return reader.ReadArrayHeader();
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Reading array length for '{field}' failed.", ex);
        }
    }
}
