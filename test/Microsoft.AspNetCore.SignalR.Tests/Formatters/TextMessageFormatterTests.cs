// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Sockets.Internal.Formatters;
using Xunit;

namespace Microsoft.AspNetCore.Sockets.Tests.Internal.Formatters
{
    public class TextMessageFormatterTests
    {
        [Fact]
        public void WriteMultipleMessages()
        {
            const string expectedEncoding = "0:;14:Hello,\r\nWorld!;";
            var messages = new[]
            {
                new byte[0],
                Encoding.UTF8.GetBytes("Hello,\r\nWorld!")
            };

            var output = new MemoryStream();
            foreach (var message in messages)
            {
                TextMessageFormatter.WriteMessage(message, output);
            }

            Assert.Equal(expectedEncoding, Encoding.UTF8.GetString(output.ToArray()));
        }
        
        [Theory]
        [InlineData(8, "0:;", "")]
        [InlineData(8, "3:ABC;", "ABC")]
        [InlineData(8, "11:A\nR\rC\r\n;DEF;", "A\nR\rC\r\n;DEF")]
        [InlineData(256, "11:A\nR\rC\r\n;DEF;", "A\nR\rC\r\n;DEF")]
        public void WriteMessage(int chunkSize, string encoded, string payload)
        {
            var message = Encoding.UTF8.GetBytes(payload);
            var output = new MemoryStream();

            TextMessageFormatter.WriteMessage(message, output);

            Assert.Equal(encoded, Encoding.UTF8.GetString(output.ToArray()));
        }
    }
}
