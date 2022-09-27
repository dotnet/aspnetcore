// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Authentication.Test;

public class JsonDocumentAuthExtensionsTests
{
    [Theory]
    [InlineData("{ \"foo\": null }", null)]
    [InlineData("{ \"foo\": \"\" }", "")]
    [InlineData("{ \"foo\": \"bar\" }", "bar")]
    [InlineData("{ \"foo\": 1 }", "1")]
    [InlineData("{ \"bar\": \"baz\" }", null)]
    public void GetStringReturnsCorrectValue(string json, string expected)
    {
        using (var document = JsonDocument.Parse(json))
        {
            var value = document.RootElement.GetString("foo");
            Assert.Equal(expected, value);
        }
    }
}
