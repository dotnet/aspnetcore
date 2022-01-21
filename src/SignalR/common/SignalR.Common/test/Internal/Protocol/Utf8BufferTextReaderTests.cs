// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.SignalR.Internal;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Common.Tests.Internal.Protocol;

public class Utf8BufferTextReaderTests
{
    [Fact]
    public void ReadingWhenCharBufferBigEnough()
    {
        var buffer = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("Hello World"));
        var reader = new Utf8BufferTextReader();
        reader.SetBuffer(buffer);

        var chars = new char[1024];
        var read = reader.Read(chars, 0, chars.Length);

        Assert.Equal("Hello World", new string(chars, 0, read));
    }

    [Fact]
    public void ReadingUnicodeWhenCharBufferBigEnough()
    {
        var buffer = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("a\u00E4\u00E4\u00a9o"));
        var reader = new Utf8BufferTextReader();
        reader.SetBuffer(buffer);

        var chars = new char[1024];
        var read = reader.Read(chars, 0, chars.Length);

        Assert.Equal(5, read);
        Assert.Equal("a\u00E4\u00E4\u00a9o", new string(chars, 0, read));

        read = reader.Read(chars, 0, chars.Length);

        Assert.Equal(0, read);
    }

    [Fact]
    public void ReadingWhenCharBufferBigEnoughAndNotStartingFromZero()
    {
        var buffer = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("Hello World"));
        var reader = new Utf8BufferTextReader();
        reader.SetBuffer(buffer);

        var chars = new char[1024];
        var read = reader.Read(chars, 10, chars.Length - 10);

        Assert.Equal(11, read);
        Assert.Equal("Hello World", new string(chars, 10, read));
    }

    [Fact]
    public void ReadingWhenBufferTooSmall()
    {
        var buffer = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("Hello World"));
        var reader = new Utf8BufferTextReader();
        reader.SetBuffer(buffer);

        var chars = new char[5];
        var read = reader.Read(chars, 0, chars.Length);

        Assert.Equal(5, read);
        Assert.Equal("Hello", new string(chars, 0, read));

        read = reader.Read(chars, 0, chars.Length);

        Assert.Equal(5, read);
        Assert.Equal(" Worl", new string(chars, 0, read));

        read = reader.Read(chars, 0, chars.Length);

        Assert.Equal(1, read);
        Assert.Equal("d", new string(chars, 0, read));

        read = reader.Read(chars, 0, chars.Length);

        Assert.Equal(0, read);

        read = reader.Read(chars, 0, 1);

        Assert.Equal(0, read);
    }

    [Fact]
    public void ReadingUnicodeWhenBufferTooSmall()
    {
        var buffer = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("\u00E4\u00E4\u00E5"));
        var reader = new Utf8BufferTextReader();
        reader.SetBuffer(buffer);

        var chars = new char[2];
        var read = reader.Read(chars, 0, chars.Length);

        Assert.Equal(2, read);
        Assert.Equal("\u00E4\u00E4", new string(chars, 0, read));

        read = reader.Read(chars, 0, chars.Length);

        Assert.Equal(1, read);
        Assert.Equal("\u00E5", new string(chars, 0, read));

        read = reader.Read(chars, 0, chars.Length);

        Assert.Equal(0, read);

        read = reader.Read(chars, 0, 1);

        Assert.Equal(0, read);
    }
}
