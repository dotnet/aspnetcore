// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Internal;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Formatters;

public class TextMessageFormatterTests
{
    [Fact]
    public void WriteMessage()
    {
        using (var ms = new MemoryStream())
        {
            var buffer = Encoding.UTF8.GetBytes("ABC");
            ms.Write(buffer, 0, buffer.Length);
            ms.WriteByte(TextMessageFormatter.RecordSeparator);
            Assert.Equal("ABC\u001e", Encoding.UTF8.GetString(ms.ToArray()));
        }
    }
}
