// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Http.Connections.Tests
{
    public class ServerSentEventsMessageFormatterTests
    {
        [Theory]
        [MemberData(nameof(PayloadData))]
        public async Task WriteTextMessageFromSingleSegment(string encoded, string payload)
        {
            var buffer = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(payload));

            var output = new MemoryStream();
            await ServerSentEventsMessageFormatter.WriteMessageAsync(buffer, output, default);

            Assert.Equal(encoded, Encoding.UTF8.GetString(output.ToArray()));
        }

        [Theory]
        [MemberData(nameof(PayloadData))]
        public async Task WriteTextMessageFromMultipleSegments(string encoded, string payload)
        {
            var buffer = ReadOnlySequenceFactory.SegmentPerByteFactory.CreateWithContent(Encoding.UTF8.GetBytes(payload));

            var output = new MemoryStream();
            await ServerSentEventsMessageFormatter.WriteMessageAsync(buffer, output, default);

            Assert.Equal(encoded, Encoding.UTF8.GetString(output.ToArray()));
        }

        public static IEnumerable<object[]> PayloadData => new List<object[]>
        {
            new object[] { "\r\n", "" },
            new object[] { "data: Hello, World\r\n\r\n", "Hello, World" },
            new object[] { "data: Hello\r\ndata: World\r\n\r\n", "Hello\r\nWorld" },
            new object[] { "data: Hello\r\ndata: World\r\n\r\n", "Hello\nWorld" },
            new object[] { "data: Hello\r\ndata: \r\n\r\n", "Hello\n" },
            new object[] { "data: Hello\r\ndata: \r\n\r\n", "Hello\r\n" },
        };
    }
}
