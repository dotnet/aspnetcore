// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Sockets.Internal.Formatters;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Common.Tests.Internal.Formatters
{
    public class ServerSentEventsParserTests
    {
        [Theory]
        [InlineData("data: T\r\n\r\n", "")]
        [InlineData("data: T\r\ndata: \r\r\n\r\n", "\r")]
        [InlineData("data: T\r\ndata: A\rB\r\n\r\n", "A\rB")]
        [InlineData("data: T\r\ndata: Hello, World\r\n\r\n", "Hello, World")]
        [InlineData("data: T\r\ndata: Hello, World\r\n\r\n", "Hello, World")]
        [InlineData("data: T\r\ndata: Hello, World\r\n\r\ndata: ", "Hello, World")]
        public void ParseSSEMessageSuccessCases(string encodedMessage, string expectedMessage)
        {
            var buffer = Encoding.UTF8.GetBytes(encodedMessage);
            var readableBuffer = ReadableBuffer.Create(buffer);
            var parser = new ServerSentEventsMessageParser();

            var parseResult = parser.ParseMessage(readableBuffer, out var consumed, out var examined, out Message message);
            Assert.Equal(ServerSentEventsMessageParser.ParseResult.Completed, parseResult);
            Assert.Equal(MessageType.Text, message.Type);
            Assert.Equal(consumed, examined);

            var result = Encoding.UTF8.GetString(message.Payload);
            Assert.Equal(expectedMessage, result);
        }

        [Theory]
        [InlineData("data: X\r\n", "Unknown message type: 'X'")]
        [InlineData("data: T\n", "Unexpected '\n' in message. A '\n' character can only be used as part of the newline sequence '\r\n'")]
        [InlineData("data: X\r\n\r\n", "Unknown message type: 'X'")]
        [InlineData("data: Not the message type\r\n\r\n", "Unknown message type: 'N'")]
        [InlineData("data: T\r\ndata: Hello, World\r\r\n\n", "There was an error in the frame format")]
        [InlineData("data: Not the message type\r\r\n", "Unknown message type: 'N'")]
        [InlineData("data: T\r\ndata: Hello, World\n\n", "Unexpected '\n' in message. A '\n' character can only be used as part of the newline sequence '\r\n'")]
        [InlineData("data: T\r\nfoo: Hello, World\r\n\r\n", "Expected the message prefix 'data: '")]
        [InlineData("foo: T\r\ndata: Hello, World\r\n\r\n", "Expected the message prefix 'data: '")]
        [InlineData("food: T\r\ndata: Hello, World\r\n\r\n", "Expected the message prefix 'data: '")]
        [InlineData("data: T\r\ndata: Hello, World\r\n\n", "There was an error in the frame format")]
        [InlineData("data: T\r\ndata: Hello\n, World\r\n\r\n", "Unexpected '\n' in message. A '\n' character can only be used as part of the newline sequence '\r\n'")]
        [InlineData("data: data: \r\n", "Unknown message type: 'd'")]
        [InlineData("data: T\r\ndata: Hello, World\r\n\r\\", "Expected a \\r\\n frame ending")]
        [InlineData("data: T\r\ndata: Major\r\ndata:  Key\rndata:  Alert\r\n\r\\", "Expected a \\r\\n frame ending")]
        [InlineData("data: T\r\ndata: Major\r\ndata:  Key\r\ndata:  Alert\r\n\r\\", "Expected a \\r\\n frame ending")]
        public void ParseSSEMessageFailureCases(string encodedMessage, string expectedExceptionMessage)
        {
            var buffer = Encoding.UTF8.GetBytes(encodedMessage);
            var readableBuffer = ReadableBuffer.Create(buffer);
            var parser = new ServerSentEventsMessageParser();

            var ex = Assert.Throws<FormatException>(() => { parser.ParseMessage(readableBuffer, out var consumed, out var examined, out Message message); });
            Assert.Equal(expectedExceptionMessage, ex.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("data:")]
        [InlineData("data: \r")]
        [InlineData("data: T\r\nda")]
        [InlineData("data: T\r\ndata:")]
        [InlineData("data: T\r\ndata: Hello, World")]
        [InlineData("data: T\r\ndata: Hello, World\r")]
        [InlineData("data: T\r\ndata: Hello, World\r\n")]
        [InlineData("data: T\r\ndata: Hello, World\r\n\r")]
        public void ParseSSEMessageIncompleteParseResult(string encodedMessage)
        {
            var buffer = Encoding.UTF8.GetBytes(encodedMessage);
            var readableBuffer = ReadableBuffer.Create(buffer);
            var parser = new ServerSentEventsMessageParser();

            var parseResult = parser.ParseMessage(readableBuffer, out var consumed, out var examined, out Message message);

            Assert.Equal(ServerSentEventsMessageParser.ParseResult.Incomplete, parseResult);
        }

        [Theory]
        [InlineData("d", "ata: T\r\ndata: Hello, World\r\n\r\n", "Hello, World")]
        [InlineData("data: T", "\r\ndata: Hello, World\r\n\r\n", "Hello, World")]
        [InlineData("data: T\r", "\ndata: Hello, World\r\n\r\n", "Hello, World")]
        [InlineData("data: T\r\n", "data: Hello, World\r\n\r\n", "Hello, World")]
        [InlineData("data: T\r\nd", "ata: Hello, World\r\n\r\n", "Hello, World")]
        [InlineData("data: T\r\ndata: ", "Hello, World\r\n\r\n", "Hello, World")]
        [InlineData("data: T\r\ndata: Hello, World", "\r\n\r\n", "Hello, World")]
        [InlineData("data: T\r\ndata: Hello, World\r\n", "\r\n", "Hello, World")]
        [InlineData("data: T", "\r\ndata: Hello, World\r\n\r\n", "Hello, World")]
        [InlineData("data: ", "T\r\ndata: Hello, World\r\n\r\n", "Hello, World")]
        public async Task ParseMessageAcrossMultipleReadsSuccess(string encodedMessagePart1, string encodedMessagePart2, string expectedMessage)
        {
            using (var pipeFactory = new PipeFactory())
            {
                var pipe = pipeFactory.Create();

                // Read the first part of the message
                await pipe.Writer.WriteAsync(Encoding.UTF8.GetBytes(encodedMessagePart1));

                var result = await pipe.Reader.ReadAsync();
                var parser = new ServerSentEventsMessageParser();

                var parseResult = parser.ParseMessage(result.Buffer, out var consumed, out var examined, out Message message);
                Assert.Equal(ServerSentEventsMessageParser.ParseResult.Incomplete, parseResult);

                pipe.Reader.Advance(consumed, examined);

                // Send the rest of the data and parse the complete message
                await pipe.Writer.WriteAsync(Encoding.UTF8.GetBytes(encodedMessagePart2));
                result = await pipe.Reader.ReadAsync();

                parseResult = parser.ParseMessage(result.Buffer, out consumed, out examined, out message);
                Assert.Equal(ServerSentEventsMessageParser.ParseResult.Completed, parseResult);
                Assert.Equal(MessageType.Text, message.Type);
                Assert.Equal(consumed, examined);

                var resultMessage = Encoding.UTF8.GetString(message.Payload);
                Assert.Equal(expectedMessage, resultMessage);
            }
        }

        [Theory]
        [InlineData("data: ", "X\r\n", "Unknown message type: 'X'")]
        [InlineData("data: T", "\n", "Unexpected '\n' in message. A '\n' character can only be used as part of the newline sequence '\r\n'")]
        [InlineData("data: ", "X\r\n\r\n", "Unknown message type: 'X'")]
        [InlineData("data: ", "Not the message type\r\n\r\n", "Unknown message type: 'N'")]
        [InlineData("data: T\r\n", "data: Hello, World\r\r\n\n", "There was an error in the frame format")]
        [InlineData("data:", " Not the message type\r\r\n", "Unknown message type: 'N'")]
        [InlineData("data: T\r\n", "data: Hello, World\n\n", "Unexpected '\n' in message. A '\n' character can only be used as part of the newline sequence '\r\n'")]
        [InlineData("data: T\r\nf", "oo: Hello, World\r\n\r\n", "Expected the message prefix 'data: '")]
        [InlineData("foo", ": T\r\ndata: Hello, World\r\n\r\n", "Expected the message prefix 'data: '")]
        [InlineData("food:", " T\r\ndata: Hello, World\r\n\r\n", "Expected the message prefix 'data: '")]
        [InlineData("data: T\r\ndata: Hello, W", "orld\r\n\n", "There was an error in the frame format")]
        [InlineData("data: T\r\nda", "ta: Hello\n, World\r\n\r\n", "Unexpected '\n' in message. A '\n' character can only be used as part of the newline sequence '\r\n'")]
        [InlineData("data:", " data: \r\n", "Unknown message type: 'd'")]
        [InlineData("data: ", "T\r\ndata: Major\r\ndata:  Key\r\ndata:  Alert\r\n\r\\", "Expected a \\r\\n frame ending")]
        public async Task ParseMessageAcrossMultipleReadsFailure(string encodedMessagePart1, string encodedMessagePart2, string expectedMessage)
        {
            using (var pipeFactory = new PipeFactory())
            {
                var pipe = pipeFactory.Create();

                // Read the first part of the message
                await pipe.Writer.WriteAsync(Encoding.UTF8.GetBytes(encodedMessagePart1));

                var result = await pipe.Reader.ReadAsync();
                var parser = new ServerSentEventsMessageParser();

                var parseResult = parser.ParseMessage(result.Buffer, out var consumed, out var examined, out Message message);
                Assert.Equal(ServerSentEventsMessageParser.ParseResult.Incomplete, parseResult);

                pipe.Reader.Advance(consumed, examined);

                // Send the rest of the data and parse the complete message
                await pipe.Writer.WriteAsync(Encoding.UTF8.GetBytes(encodedMessagePart2));
                result = await pipe.Reader.ReadAsync();

                var ex = Assert.Throws<FormatException>(() => parser.ParseMessage(result.Buffer, out consumed, out examined, out message));
                Assert.Equal(expectedMessage, ex.Message);

            }
        }

        [Fact]
        public async Task ParseMultipleMessages()
        {
            using (var pipeFactory = new PipeFactory())
            {
                var pipe = pipeFactory.Create();

                var message1 = "data: T\r\ndata: foo\r\n\r\n";
                var message2 = "data: T\r\ndata: bar\r\n\r\n";
                // Read the first part of the message
                await pipe.Writer.WriteAsync(Encoding.UTF8.GetBytes(message1 + message2));

                var result = await pipe.Reader.ReadAsync();
                var parser = new ServerSentEventsMessageParser();

                var parseResult = parser.ParseMessage(result.Buffer, out var consumed, out var examined, out var message);
                Assert.Equal(ServerSentEventsMessageParser.ParseResult.Completed, parseResult);
                Assert.Equal(MessageType.Text, message.Type);
                Assert.Equal("foo", Encoding.UTF8.GetString(message.Payload));
                Assert.Equal(consumed, result.Buffer.Move(result.Buffer.Start, message1.Length));
                pipe.Reader.Advance(consumed, examined);
                Assert.Equal(consumed, examined);

                parser.Reset();

                result = await pipe.Reader.ReadAsync();
                parseResult = parser.ParseMessage(result.Buffer, out consumed, out examined, out message);
                Assert.Equal(ServerSentEventsMessageParser.ParseResult.Completed, parseResult);
                Assert.Equal(MessageType.Text, message.Type);
                Assert.Equal("bar", Encoding.UTF8.GetString(message.Payload));
                pipe.Reader.Advance(consumed, examined);
            }
        }

        public static IEnumerable<object[]> MultilineMessages
        {
            get
            {
                yield return new object[] { "data: T\r\ndata: Shaolin\r\ndata:  Fantastic\r\n\r\n", "Shaolin" + Environment.NewLine + " Fantastic" };
                yield return new object[] { "data: T\r\ndata: The\r\ndata: Get\r\ndata: Down\r\n\r\n", "The" + Environment.NewLine + "Get" + Environment.NewLine + "Down" };
            }
        }

        [Theory]
        [MemberData(nameof(MultilineMessages))]
        public void ParseMessagesWithMultipleDataLines(string encodedMessage, string expectedMessage)
        {
            var buffer = Encoding.UTF8.GetBytes(encodedMessage);
            var readableBuffer = ReadableBuffer.Create(buffer);
            var parser = new ServerSentEventsMessageParser();

            var parseResult = parser.ParseMessage(readableBuffer, out var consumed, out var examined, out Message message);
            Assert.Equal(ServerSentEventsMessageParser.ParseResult.Completed, parseResult);
            Assert.Equal(MessageType.Text, message.Type);
            Assert.Equal(consumed, examined);

            var result = Encoding.UTF8.GetString(message.Payload);
            Assert.Equal(expectedMessage, result);
        }

        [Fact]
        public void ParseSSEMessageBinaryNotSupported()
        {
            var encodedMessage = "data: B\r\ndata: \r\n\r\n";
            var buffer = Encoding.UTF8.GetBytes(encodedMessage);
            var readableBuffer = ReadableBuffer.Create(buffer);
            var parser = new ServerSentEventsMessageParser();

            var ex = Assert.Throws<NotSupportedException>(() => { parser.ParseMessage(readableBuffer, out var consumed, out var examined, out Message message); });
            Assert.Equal("Support for binary messages has not been implemented yet", ex.Message);
        }
    }
}
