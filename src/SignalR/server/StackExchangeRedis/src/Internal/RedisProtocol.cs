// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MessagePack;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Internal;

internal sealed class RedisProtocol
{
    private readonly DefaultHubMessageSerializer _messageSerializer;

    public RedisProtocol(DefaultHubMessageSerializer messageSerializer)
    {
        _messageSerializer = messageSerializer;
    }

    // The Redis Protocol:
    // * The message type is known in advance because messages are sent to different channels based on type
    // * Invocations are sent to the All, Group, Connection and User channels
    // * Group Commands are sent to the GroupManagement channel
    // * Acks are sent to the Acknowledgement channel.
    // * Completion messages (client results) are sent to the server specific Result channel
    // * See the Write[type] methods for a description of the protocol for each in-depth.
    // * The "Variable length integer" is the length-prefixing format used by BinaryReader/BinaryWriter:
    //   * https://learn.microsoft.com/dotnet/api/system.io.binarywriter.write?view=netcore-2.2
    // * The "Length prefixed string" is the string format used by BinaryReader/BinaryWriter:
    //   * A 7-bit variable length integer encodes the length in bytes, followed by the encoded string in UTF-8.

    public byte[] WriteInvocation(string methodName, object?[] args, string? invocationId = null,
        IReadOnlyList<string>? excludedConnectionIds = null, string? returnChannel = null)
    {
        // Written as a MessagePack 'arr' containing at least these items:
        // * A MessagePack 'arr' of 'str's representing the excluded ids
        // * [The output of WriteSerializedHubMessage, which is an 'arr']
        // For invocations expecting a result
        // * InvocationID
        // * Redis return channel
        // Any additional items are discarded.

        var memoryBufferWriter = MemoryBufferWriter.Get();
        try
        {
            var writer = new MessagePackWriter(memoryBufferWriter);

            if (!string.IsNullOrEmpty(returnChannel))
            {
                writer.WriteArrayHeader(4);
            }
            else
            {
                writer.WriteArrayHeader(2);
            }
            if (excludedConnectionIds != null && excludedConnectionIds.Count > 0)
            {
                writer.WriteArrayHeader(excludedConnectionIds.Count);
                foreach (var id in excludedConnectionIds)
                {
                    writer.Write(id);
                }
            }
            else
            {
                writer.WriteArrayHeader(0);
            }

            WriteHubMessage(ref writer, new InvocationMessage(invocationId, methodName, args));

            // Write last in order to preserve original order for cases where one server is updated and the other isn't.
            // Not really a supported scenario, but why not be nice
            if (!string.IsNullOrEmpty(returnChannel))
            {
                writer.Write(invocationId);
                writer.Write(returnChannel);
            }

            writer.Flush();

            return memoryBufferWriter.ToArray();
        }
        finally
        {
            MemoryBufferWriter.Return(memoryBufferWriter);
        }
    }

    public static byte[] WriteGroupCommand(RedisGroupCommand command)
    {
        // Written as a MessagePack 'arr' containing at least these items:
        // * An 'int': the Id of the command
        // * A 'str': The server name
        // * An 'int': The action (likely less than 0x7F and thus a single-byte fixnum)
        // * A 'str': The group name
        // * A 'str': The connection Id
        // Any additional items are discarded.

        var memoryBufferWriter = MemoryBufferWriter.Get();
        try
        {
            var writer = new MessagePackWriter(memoryBufferWriter);

            writer.WriteArrayHeader(5);
            writer.Write(command.Id);
            writer.Write(command.ServerName);
            writer.Write((byte)command.Action);
            writer.Write(command.GroupName);
            writer.Write(command.ConnectionId);
            writer.Flush();

            return memoryBufferWriter.ToArray();
        }
        finally
        {
            MemoryBufferWriter.Return(memoryBufferWriter);
        }
    }

    public static byte[] WriteAck(int messageId)
    {
        // Written as a MessagePack 'arr' containing at least these items:
        // * An 'int': The Id of the command being acknowledged.
        // Any additional items are discarded.

        var memoryBufferWriter = MemoryBufferWriter.Get();
        try
        {
            var writer = new MessagePackWriter(memoryBufferWriter);

            writer.WriteArrayHeader(1);
            writer.Write(messageId);
            writer.Flush();

            return memoryBufferWriter.ToArray();
        }
        finally
        {
            MemoryBufferWriter.Return(memoryBufferWriter);
        }
    }

    public static byte[] WriteCompletionMessage(MemoryBufferWriter writer, string protocolName)
    {
        // Written as a MessagePack 'arr' containing at least these items:
        // * A 'str': The name of the HubProtocol used for the serialization of the Completion Message
        // * [A serialized Completion Message which is a 'bin']
        // Any additional items are discarded.

        var completionMessage = writer.DetachAndReset();
        var msgPackWriter = new MessagePackWriter(writer);

        msgPackWriter.WriteArrayHeader(2);
        msgPackWriter.Write(protocolName);

        msgPackWriter.WriteBinHeader(completionMessage.ByteLength);
        foreach (var segment in completionMessage.Segments)
        {
            msgPackWriter.WriteRaw(segment.Span);
        }
        completionMessage.Dispose();

        msgPackWriter.Flush();
        return writer.ToArray();
    }

    public static RedisInvocation ReadInvocation(ReadOnlyMemory<byte> data)
    {
        // See WriteInvocation for the format
        var reader = new MessagePackReader(data);
        var length = ValidateArraySize(ref reader, 2, "Invocation");

        string? returnChannel = null;
        string? invocationId = null;

        // Read excluded Ids
        IReadOnlyList<string>? excludedConnectionIds = null;
        var idCount = reader.ReadArrayHeader();
        if (idCount > 0)
        {
            var ids = new string[idCount];
            for (var i = 0; i < idCount; i++)
            {
                ids[i] = reader.ReadString()!;
            }

            excludedConnectionIds = ids;
        }

        // Read payload
        var message = ReadSerializedHubMessage(ref reader);

        if (length > 3)
        {
            invocationId = reader.ReadString();
            returnChannel = reader.ReadString();
        }

        return new RedisInvocation(message, excludedConnectionIds, invocationId, returnChannel);
    }

    public static RedisGroupCommand ReadGroupCommand(ReadOnlyMemory<byte> data)
    {
        var reader = new MessagePackReader(data);

        // See WriteGroupCommand for format.
        ValidateArraySize(ref reader, 5, "GroupCommand");

        var id = reader.ReadInt32();
        var serverName = reader.ReadString()!;
        var action = (GroupAction)reader.ReadByte();
        var groupName = reader.ReadString()!;
        var connectionId = reader.ReadString()!;

        return new RedisGroupCommand(id, serverName, action, groupName, connectionId);
    }

    public static int ReadAck(ReadOnlyMemory<byte> data)
    {
        var reader = new MessagePackReader(data);

        // See WriteAck for format
        ValidateArraySize(ref reader, 1, "Ack");
        return reader.ReadInt32();
    }

    private void WriteHubMessage(ref MessagePackWriter writer, HubMessage message)
    {
        // Written as a MessagePack 'map' where the keys are the name of the protocol (as a MessagePack 'str')
        // and the values are the serialized blob (as a MessagePack 'bin').

        var serializedHubMessages = _messageSerializer.SerializeMessage(message);

        writer.WriteMapHeader(serializedHubMessages.Count);

        foreach (var serializedMessage in serializedHubMessages)
        {
            writer.Write(serializedMessage.ProtocolName);

            var isArray = MemoryMarshal.TryGetArray(serializedMessage.Serialized, out var array);
            Debug.Assert(isArray);
            writer.Write(array);
        }
    }

    public static SerializedHubMessage ReadSerializedHubMessage(ref MessagePackReader reader)
    {
        var count = reader.ReadMapHeader();
        var serializations = new SerializedMessage[count];
        for (var i = 0; i < count; i++)
        {
            var protocol = reader.ReadString()!;
            var serialized = reader.ReadBytes()?.ToArray() ?? Array.Empty<byte>();

            serializations[i] = new SerializedMessage(protocol, serialized);
        }

        return new SerializedHubMessage(serializations);
    }

    public static RedisCompletion ReadCompletion(ReadOnlyMemory<byte> data)
    {
        // See WriteCompletionMessage for the format
        var reader = new MessagePackReader(data);
        ValidateArraySize(ref reader, 2, "CompletionMessage");

        var protocolName = reader.ReadString()!;
        var ros = reader.ReadBytes();
        return new RedisCompletion(protocolName, ros ?? new ReadOnlySequence<byte>());
    }

    private static int ValidateArraySize(ref MessagePackReader reader, int expectedLength, string messageType)
    {
        var length = reader.ReadArrayHeader();

        if (length < expectedLength)
        {
            throw new InvalidDataException($"Insufficient items in {messageType} array.");
        }
        return length;
    }
}
