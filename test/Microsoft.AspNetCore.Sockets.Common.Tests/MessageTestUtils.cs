// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO.Pipelines;
using System.Text;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Tests
{
    internal static class MessageTestUtils
    {
        public static void AssertMessage(Message message, MessageType messageType, byte[] payload)
        {
            Assert.True(message.EndOfMessage);
            Assert.Equal(messageType, message.Type);
            Assert.Equal(payload, message.Payload);
        }

        public static void AssertMessage(Message message, MessageType messageType, string payload)
        {
            Assert.True(message.EndOfMessage);
            Assert.Equal(messageType, message.Type);
            Assert.Equal(payload, Encoding.UTF8.GetString(message.Payload));
        }

        public static Message CreateMessage(byte[] payload, MessageType type = MessageType.Binary)
        {
            return new Message(
                payload,
                type,
                endOfMessage: true);
        }

        public static Message CreateMessage(string payload, MessageType type)
        {
            return new Message(
                Encoding.UTF8.GetBytes(payload),
                type,
                endOfMessage: true);
        }
    }
}
