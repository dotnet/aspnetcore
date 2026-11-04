// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Text;
using Microsoft.AspNetCore.Internal;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Formatters;

public class TextMessageParserTests
{
    [Fact]
    public void ReadMessage()
    {
        var message = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("ABC\u001e"));

        Assert.True(TextMessageParser.TryParseMessage(ref message, out var payload));
        Assert.Equal("ABC", Encoding.UTF8.GetString(payload.ToArray()));
        Assert.False(TextMessageParser.TryParseMessage(ref message, out payload));
    }

    [Fact]
    public void TryReadingIncompleteMessage()
    {
        var message = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("ABC"));
        Assert.False(TextMessageParser.TryParseMessage(ref message, out var payload));
    }

    [Fact]
    public void TryReadingMultipleMessages()
    {
        var message = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("ABC\u001eXYZ\u001e"));
        Assert.True(TextMessageParser.TryParseMessage(ref message, out var payload));
        Assert.Equal("ABC", Encoding.UTF8.GetString(payload.ToArray()));
        Assert.True(TextMessageParser.TryParseMessage(ref message, out payload));
        Assert.Equal("XYZ", Encoding.UTF8.GetString(payload.ToArray()));
    }

    [Fact]
    public void IncompleteTrailingMessage()
    {
        var message = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("ABC\u001eXYZ\u001e123"));
        Assert.True(TextMessageParser.TryParseMessage(ref message, out var payload));
        Assert.Equal("ABC", Encoding.UTF8.GetString(payload.ToArray()));
        Assert.True(TextMessageParser.TryParseMessage(ref message, out payload));
        Assert.Equal("XYZ", Encoding.UTF8.GetString(payload.ToArray()));
        Assert.False(TextMessageParser.TryParseMessage(ref message, out payload));
    }
}
