// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using MessagePack;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Internal
{
    internal class RedisProtocol
    {
        private readonly IReadOnlyList<IHubProtocol> _protocols;

        public RedisProtocol(IReadOnlyList<IHubProtocol> protocols)
        {
            _protocols = protocols;
        }

        // The Redis Protocol:
        // * The message type is known in advance because messages are sent to different channels based on type
        // * Invocations are sent to the All, Group, Connection and User channels
        // * Group Commands are sent to the GroupManagement channel
        // * Acks are sent to the Acknowledgement channel.
        // * See the Write[type] methods for a description of the protocol for each in-depth.
        // * The "Variable length integer" is the length-prefixing format used by BinaryReader/BinaryWriter:
        //   * https://docs.microsoft.com/en-us/dotnet/api/system.io.binarywriter.write?view=netstandard-2.0
        // * The "Length prefixed string" is the string format used by BinaryReader/BinaryWriter:
        //   * A 7-bit variable length integer encodes the length in bytes, followed by the encoded string in UTF-8.

        public byte[] WriteInvocation(string methodName, object[] args) =>
            WriteInvocation(methodName, args, excludedConnectionIds: null);

        public byte[] WriteInvocation(string methodName, object[] args, IReadOnlyList<string> excludedConnectionIds)
        {
            // Written as a MessagePack 'arr' containing at least these items:
            // * A MessagePack 'arr' of 'str's representing the excluded ids
            // * [The output of WriteSerializedHubMessage, which is an 'arr']
            // Any additional items are discarded.

            var writer = MemoryBufferWriter.Get();

            try
            {
                MessagePackBinary.WriteArrayHeader(writer, 2);
                if (excludedConnectionIds != null && excludedConnectionIds.Count > 0)
                {
                    MessagePackBinary.WriteArrayHeader(writer, excludedConnectionIds.Count);
                    foreach (var id in excludedConnectionIds)
                    {
                        MessagePackBinary.WriteString(writer, id);
                    }
                }
                else
                {
                    MessagePackBinary.WriteArrayHeader(writer, 0);
                }

                WriteSerializedHubMessage(writer,
                    new SerializedHubMessage(new InvocationMessage(methodName, args)));
                return writer.ToArray();
            }
            finally
            {
                MemoryBufferWriter.Return(writer);
            }
        }

        public byte[] WriteGroupCommand(RedisGroupCommand command)
        {
            // Written as a MessagePack 'arr' containing at least these items:
            // * An 'int': the Id of the command
            // * A 'str': The server name
            // * An 'int': The action (likely less than 0x7F and thus a single-byte fixnum)
            // * A 'str': The group name
            // * A 'str': The connection Id
            // Any additional items are discarded.

            var writer = MemoryBufferWriter.Get();
            try
            {
                MessagePackBinary.WriteArrayHeader(writer, 5);
                MessagePackBinary.WriteInt32(writer, command.Id);
                MessagePackBinary.WriteString(writer, command.ServerName);
                MessagePackBinary.WriteByte(writer, (byte)command.Action);
                MessagePackBinary.WriteString(writer, command.GroupName);
                MessagePackBinary.WriteString(writer, command.ConnectionId);

                return writer.ToArray();
            }
            finally
            {
                MemoryBufferWriter.Return(writer);
            }
        }

        public byte[] WriteAck(int messageId)
        {
            // Written as a MessagePack 'arr' containing at least these items:
            // * An 'int': The Id of the command being acknowledged.
            // Any additional items are discarded.

            var writer = MemoryBufferWriter.Get();
            try
            {
                MessagePackBinary.WriteArrayHeader(writer, 1);
                MessagePackBinary.WriteInt32(writer, messageId);

                return writer.ToArray();
            }
            finally
            {
                MemoryBufferWriter.Return(writer);
            }
        }

        public RedisInvocation ReadInvocation(ReadOnlyMemory<byte> data)
        {
            // See WriteInvocation for the format
            ValidateArraySize(ref data, 2, "Invocation");

            // Read excluded Ids
            IReadOnlyList<string> excludedConnectionIds = null;
            var idCount = MessagePackUtil.ReadArrayHeader(ref data);
            if (idCount > 0)
            {
                var ids = new string[idCount];
                for (var i = 0; i < idCount; i++)
                {
                    ids[i] = MessagePackUtil.ReadString(ref data);
                }

                excludedConnectionIds = ids;
            }

            // Read payload
            var message = ReadSerializedHubMessage(ref data);
            return new RedisInvocation(message, excludedConnectionIds);
        }

        public RedisGroupCommand ReadGroupCommand(ReadOnlyMemory<byte> data)
        {
            // See WriteGroupCommand for format.
            ValidateArraySize(ref data, 5, "GroupCommand");

            var id = MessagePackUtil.ReadInt32(ref data);
            var serverName = MessagePackUtil.ReadString(ref data);
            var action = (GroupAction)MessagePackUtil.ReadByte(ref data);
            var groupName = MessagePackUtil.ReadString(ref data);
            var connectionId = MessagePackUtil.ReadString(ref data);

            return new RedisGroupCommand(id, serverName, action, groupName, connectionId);
        }

        public int ReadAck(ReadOnlyMemory<byte> data)
        {
            // See WriteAck for format
            ValidateArraySize(ref data, 1, "Ack");
            return MessagePackUtil.ReadInt32(ref data);
        }

        private void WriteSerializedHubMessage(Stream stream, SerializedHubMessage message)
        {
            // Written as a MessagePack 'map' where the keys are the name of the protocol (as a MessagePack 'str')
            // and the values are the serialized blob (as a MessagePack 'bin').

            MessagePackBinary.WriteMapHeader(stream, _protocols.Count);

            foreach (var protocol in _protocols)
            {
                MessagePackBinary.WriteString(stream, protocol.Name);

                var serialized = message.GetSerializedMessage(protocol);
                var isArray = MemoryMarshal.TryGetArray(serialized, out var array);
                Debug.Assert(isArray);
                MessagePackBinary.WriteBytes(stream, array.Array, array.Offset, array.Count);
            }
        }

        public static SerializedHubMessage ReadSerializedHubMessage(ref ReadOnlyMemory<byte> data)
        {
            var count = MessagePackUtil.ReadMapHeader(ref data);
            var serializations = new SerializedMessage[count];
            for (var i = 0; i < count; i++)
            {
                var protocol = MessagePackUtil.ReadString(ref data);
                var serialized = MessagePackUtil.ReadBytes(ref data);
                serializations[i] = new SerializedMessage(protocol, serialized);
            }

            return new SerializedHubMessage(serializations);
        }

        private static void ValidateArraySize(ref ReadOnlyMemory<byte> data, int expectedLength, string messageType)
        {
            var length = MessagePackUtil.ReadArrayHeader(ref data);

            if (length < expectedLength)
            {
                throw new InvalidDataException($"Insufficient items in {messageType} array.");
            }
        }
    }
}
