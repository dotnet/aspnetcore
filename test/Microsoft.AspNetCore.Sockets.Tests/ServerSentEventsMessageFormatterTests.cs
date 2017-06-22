// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Sockets.Internal.Formatters;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Tests.Internal.Formatters
{
    public class ServerSentEventsMessageFormatterTests
    {
        [Theory]
        [InlineData("\r\n", "")]
        [InlineData("data: Hello, World\r\n\r\n", "Hello, World")]
        [InlineData("data: Hello\r\ndata: World\r\n\r\n", "Hello\r\nWorld")]
        [InlineData("data: Hello\r\ndata: World\r\n\r\n", "Hello\nWorld")]
        [InlineData("data: Hello\r\ndata: \r\n\r\n", "Hello\n")]
        [InlineData("data: Hello\r\ndata: \r\n\r\n", "Hello\r\n")]
        public void WriteTextMessage(string encoded, string payload)
        {
            var output = new MemoryStream();
            Assert.True(ServerSentEventsMessageFormatter.TryWriteMessage(Encoding.UTF8.GetBytes(payload), output));

            Assert.Equal(encoded, Encoding.UTF8.GetString(output.ToArray()));
        }
    }
}
