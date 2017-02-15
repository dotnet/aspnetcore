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
            Assert.Equal(payload, message.Payload.Buffer.ToArray());
        }

        public static void AssertMessage(Message message, MessageType messageType, string payload)
        {
            Assert.True(message.EndOfMessage);
            Assert.Equal(messageType, message.Type);
            Assert.Equal(payload, Encoding.UTF8.GetString(message.Payload.Buffer.ToArray()));
        }

        public static Message CreateMessage(byte[] payload, MessageType type = MessageType.Binary)
        {
            return new Message(
                ReadableBuffer.Create(payload).Preserve(),
                type,
                endOfMessage: true);
        }

        public static Message CreateMessage(string payload, MessageType type)
        {
            return new Message(
                ReadableBuffer.Create(Encoding.UTF8.GetBytes(payload)).Preserve(),
                type,
                endOfMessage: true);
        }
    }
}
