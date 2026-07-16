// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable
#pragma warning disable ASP0030 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Xunit;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

public class DeviceBoundSessionRegistrationHeaderTests
{
    [Theory]
    [InlineData("", "\"\"")]
    [InlineData(" ", "\" \"")]
    [InlineData(" ~", "\" ~\"")]
    [InlineData("\"", "\"\\\"\"")]
    [InlineData("\\", "\"\\\\\"")]
    [InlineData("a\"b\\c\"\\", "\"a\\\"b\\\\c\\\"\\\\\"")]
    [InlineData("/foo/path%20with%20spaces", "\"/foo/path%20with%20spaces\"")]
    [InlineData("AbCdEf0123-_", "\"AbCdEf0123-_\"")]
    public void SerializeSfString_ValidValue_ReturnsQuotedAndEscapedValue(string value, string expected)
    {
        var result = DeviceBoundSessionRegistrationHeader.SerializeSfString(value);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("\0")]
    [InlineData("\u001F")]
    [InlineData("\t")]
    [InlineData("\u007F")]
    [InlineData("é")]
    public void SerializeSfString_InvalidValue_ThrowsFormatException(string value)
    {
        Assert.Throws<FormatException>(() => DeviceBoundSessionRegistrationHeader.SerializeSfString(value));
    }

    [Fact]
    public void SerializeSfString_LoneHighSurrogate_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => DeviceBoundSessionRegistrationHeader.SerializeSfString("\uD800"));
    }

    [Fact]
    public void SerializeSfString_LoneLowSurrogate_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => DeviceBoundSessionRegistrationHeader.SerializeSfString("\uDC00"));
    }
}