// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.WebEncoders.Testing;

namespace Microsoft.AspNetCore.Html.Test;

public class HtmlContentBuilderExtensionsTest
{
    [Fact]
    public void Builder_AppendLine_Empty()
    {
        // Arrange
        var builder = new TestHtmlContentBuilder();

        // Act
        builder.AppendLine();

        // Assert
        Assert.Collection(
            builder.Entries,
            entry => Assert.Equal(Environment.NewLine, HtmlContentToString(entry)));
    }

    [Fact]
    public void Builder_AppendLine_String()
    {
        // Arrange
        var builder = new TestHtmlContentBuilder();

        // Act
        builder.AppendLine("Hi");

        // Assert
        Assert.Collection(
            builder.Entries,
            entry => Assert.Equal("Hi", Assert.IsType<UnencodedString>(entry).Value),
            entry => Assert.Equal(Environment.NewLine, HtmlContentToString(entry)));
    }

    [Fact]
    public void Builder_AppendLine_IHtmlContent()
    {
        // Arrange
        var builder = new TestHtmlContentBuilder();
        var content = new OtherHtmlContent("Hi");

        // Act
        builder.AppendLine(content);

        // Assert
        Assert.Collection(
            builder.Entries,
            entry => Assert.Same(content, entry),
            entry => Assert.Equal(Environment.NewLine, HtmlContentToString(entry)));
    }

    [Fact]
    public void Builder_AppendHtmlLine_String()
    {
        // Arrange
        var builder = new TestHtmlContentBuilder();

        // Act
        builder.AppendHtmlLine("Hi");

        // Assert
        Assert.Collection(
            builder.Entries,
            entry => Assert.Equal("Hi", Assert.IsType<EncodedString>(entry).Value),
            entry => Assert.Equal(Environment.NewLine, HtmlContentToString(entry)));
    }

    [Fact]
    public void Builder_SetContent_String()
    {
        // Arrange
        var builder = new TestHtmlContentBuilder();
        builder.Append("Existing Content. Will be Cleared.");

        // Act
        builder.SetContent("Hi");

        // Assert
        Assert.Collection(
            builder.Entries,
            entry => Assert.Equal("Hi", Assert.IsType<UnencodedString>(entry).Value));
    }

    [Fact]
    public void Builder_SetContent_IHtmlContent()
    {
        // Arrange
        var builder = new TestHtmlContentBuilder();
        builder.Append("Existing Content. Will be Cleared.");

        var content = new OtherHtmlContent("Hi");

        // Act
        builder.SetHtmlContent(content);

        // Assert
        Assert.Collection(
            builder.Entries,
            entry => Assert.Same(content, entry));
    }

    [Fact]
    public void Builder_SetHtmlContent_String()
    {
        // Arrange
        var builder = new TestHtmlContentBuilder();
        builder.Append("Existing Content. Will be Cleared.");

        // Act
        builder.SetHtmlContent("Hi");

        // Assert
        Assert.Collection(
            builder.Entries,
            entry => Assert.Equal("Hi", Assert.IsType<EncodedString>(entry).Value));
    }

    [Fact]
    public void Builder_AppendFormat()
    {
        // Arrange
        var builder = new TestHtmlContentBuilder();

        // Act
        builder.AppendFormat("{0} {1} {2} {3}!", "First", "Second", "Third", "Fourth");

        // Assert
        Assert.Equal(
            "HtmlEncode[[First]] HtmlEncode[[Second]] HtmlEncode[[Third]] HtmlEncode[[Fourth]]!",
            HtmlContentToString(builder));
    }

    [Fact]
    public void Builder_AppendFormat_HtmlContent()
    {
        // Arrange
        var builder = new TestHtmlContentBuilder();

        // Act
        builder.AppendFormat("{0}!", new EncodedString("First"));

        // Assert
        Assert.Equal(
            "First!",
            HtmlContentToString(builder));
    }

    [Fact]
    public void Builder_AppendFormat_HtmlString()
    {
        // Arrange
        var builder = new TestHtmlContentBuilder();

        // Act
        builder.AppendFormat("{0}!", new HtmlString("First"));

        // Assert
        Assert.Equal("First!", HtmlContentToString(builder));
    }

    [Fact]
    public void Builder_AppendFormatContent_With1Argument()
    {
        // Arrange
        var builder = new TestHtmlContentBuilder();

        // Act
        builder.AppendFormat("0x{0:X} - hex equivalent for 50.", 50);

        // Assert
        Assert.Equal(
            "0xHtmlEncode[[32]] - hex equivalent for 50.",
            HtmlContentToString(builder));
    }

    [Fact]
    public void Builder_AppendFormatContent_With2Arguments()
    {
        // Arrange
        var builder = new TestHtmlContentBuilder();

        // Act
        builder.AppendFormat("0x{0:X} - hex equivalent for {1}.", 50, 50);

        // Assert
        Assert.Equal(
            "0xHtmlEncode[[32]] - hex equivalent for HtmlEncode[[50]].",
            HtmlContentToString(builder));
    }

    [Fact]
    public void Builder_AppendFormatContent_With3Arguments()
    {
        // Arrange
        var builder = new TestHtmlContentBuilder();

        // Act
        builder.AppendFormat("0x{0:X} - {1} equivalent for {2}.", 50, "hex", 50);

        // Assert
        Assert.Equal(
            "0xHtmlEncode[[32]] - HtmlEncode[[hex]] equivalent for HtmlEncode[[50]].",
            HtmlContentToString(builder));
    }

    [Fact]
    public void Builder_AppendFormat_WithAlignmentComponent()
    {
        // Arrange
        var builder = new TestHtmlContentBuilder();

        // Act
        builder.AppendFormat("{0, -25} World!", "Hello");

        // Assert
        Assert.Equal(
            "HtmlEncode[[Hello]]       World!",
            HtmlContentToString(builder));
    }

    [Fact]
    public void Builder_AppendFormat_WithFormatStringComponent()
    {
        // Arrange
        var builder = new TestHtmlContentBuilder();

        // Act
        builder.AppendFormat("0x{0:X}", 50);

        // Assert
        Assert.Equal("0xHtmlEncode[[32]]", HtmlContentToString(builder));
    }

    [Fact]
    public void Builder_AppendFormat_WithCulture()
    {
        // Arrange
        var builder = new TestHtmlContentBuilder();

        // Act
        builder.AppendFormat(
            CultureInfo.InvariantCulture,
            "Numbers in InvariantCulture - {0, -5:N} {1} {2} {3}!",
            1.1,
            2.98,
            145.82,
            32.86);

        // Assert
        Assert.Equal(
            "Numbers in InvariantCulture - HtmlEncode[[1.10]] HtmlEncode[[2.98]] " +
                "HtmlEncode[[145.82]] HtmlEncode[[32.86]]!",
            HtmlContentToString(builder));
    }

    [Fact]
    public void Builder_AppendFormat_WithCulture_1Argument()
    {
        // Arrange
        var builder = new TestHtmlContentBuilder();

        // Act
        builder.AppendFormat(
            CultureInfo.InvariantCulture,
            "Numbers in InvariantCulture - {0:N}!",
            1.1);

        // Assert
        Assert.Equal(
            "Numbers in InvariantCulture - HtmlEncode[[1.10]]!",
            HtmlContentToString(builder));
    }

    [Fact]
    public void Builder_AppendFormat_WithCulture_2Arguments()
    {
        // Arrange
        var builder = new TestHtmlContentBuilder();

        // Act
        builder.AppendFormat(
            CultureInfo.InvariantCulture,
            "Numbers in InvariantCulture - {0:N} {1}!",
            1.1,
            2.98);

        // Assert
        Assert.Equal(
            "Numbers in InvariantCulture - HtmlEncode[[1.10]] HtmlEncode[[2.98]]!",
            HtmlContentToString(builder));
    }

    [Fact]
    public void Builder_AppendFormat_WithCulture_3Arguments()
    {
        // Arrange
        var builder = new TestHtmlContentBuilder();

        // Act
        builder.AppendFormat(
            CultureInfo.InvariantCulture,
            "Numbers in InvariantCulture - {0:N} {1} {2}!",
            1.1,
            2.98,
            3.12);

        // Assert
        Assert.Equal(
            "Numbers in InvariantCulture - HtmlEncode[[1.10]] HtmlEncode[[2.98]] HtmlEncode[[3.12]]!",
            HtmlContentToString(builder));
    }

    [Fact]
    public void Builder_AppendFormat_WithDifferentCulture()
    {
        // Arrange
        var builder = new TestHtmlContentBuilder();
        var culture = new CultureInfo("fr-FR");

        // Act
        builder.AppendFormat(culture, "{0} in french!", 1.21);

        // Assert
        Assert.Equal(
            "HtmlEncode[[1,21]] in french!",
            HtmlContentToString(builder));
    }

    [Fact]
    [ReplaceCulture("de-DE", "de-DE")]
    public void Builder_AppendFormat_WithDifferentCurrentCulture()
    {
        // Arrange
        var builder = new TestHtmlContentBuilder();

        // Act
        builder.AppendFormat(CultureInfo.CurrentCulture, "{0:D}", new DateTime(2015, 02, 01));

        // Assert
        Assert.Equal(
            "HtmlEncode[[Sonntag, 1. Februar 2015]]",
            HtmlContentToString(builder));
    }

    private static string HtmlContentToString(IHtmlContent content)
    {
        using var writer = new StringWriter();
        content.WriteTo(writer, new HtmlTestEncoder());
        return writer.ToString();
    }

    private class TestHtmlContentBuilder : IHtmlContentBuilder
    {
        public List<IHtmlContent> Entries { get; } = new List<IHtmlContent>();

        public IHtmlContentBuilder Append(string unencoded)
        {
            Entries.Add(new UnencodedString(unencoded));
            return this;
        }

        public IHtmlContentBuilder AppendHtml(IHtmlContent content)
        {
            Entries.Add(content);
            return this;
        }

        public IHtmlContentBuilder AppendHtml(string encoded)
        {
            Entries.Add(new EncodedString(encoded));
            return this;
        }

        public IHtmlContentBuilder Clear()
        {
            Entries.Clear();
            return this;
        }

        public void CopyTo(IHtmlContentBuilder destination)
        {
            foreach (var entry in Entries)
            {
                destination.AppendHtml(entry);
            }
        }

        public void MoveTo(IHtmlContentBuilder destination)
        {
            CopyTo(destination);
            Clear();
        }

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            foreach (var entry in Entries)
            {
                entry.WriteTo(writer, encoder);
            }
        }
    }

    private class EncodedString : IHtmlContent
    {
        public EncodedString(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            writer.Write(Value);
        }
    }

    private class UnencodedString : IHtmlContent
    {
        public UnencodedString(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            encoder.Encode(writer, Value);
        }
    }

    private class OtherHtmlContent : IHtmlContent
    {
        public OtherHtmlContent(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            throw new NotImplementedException();
        }
    }
}
