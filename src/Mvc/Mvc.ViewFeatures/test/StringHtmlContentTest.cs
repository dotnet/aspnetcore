// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.Extensions.WebEncoders.Testing;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures;

public class StringHtmlContentTest
{
    [Fact]
    public void WriteTo_WritesContent()
    {
        // Arrange & Act
        var content = new StringHtmlContent("Hello World");

        // Assert
        using (var writer = new StringWriter())
        {
            content.WriteTo(writer, new HtmlTestEncoder());
            Assert.Equal("HtmlEncode[[Hello World]]", writer.ToString());
        }
    }

    [Fact]
    public void Emoji_EncodedCorrectly()
    {
        // Arrange & Act
        var tearsOfJoy = new StringHtmlContent("😂2");

        // Assert
        using (var stringWriter = new StringWriter())
        {
            tearsOfJoy.WriteTo(stringWriter, HtmlEncoder.Default);
            Assert.Equal("&#x1f602;2", stringWriter.ToString(), ignoreCase: true);
        }
    }
}
