// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http.Connections.Internal;
using Xunit;

namespace Microsoft.AspNetCore.Http.Connections.Tests
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
            ServerSentEventsMessageFormatter.WriteMessage(Encoding.UTF8.GetBytes(payload), output);

            Assert.Equal(encoded, Encoding.UTF8.GetString(output.ToArray()));
        }
    }
}
