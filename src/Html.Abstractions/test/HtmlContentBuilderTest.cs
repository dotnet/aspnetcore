// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.WebEncoders.Testing;

namespace Microsoft.Extensions.Internal;

public class HtmlContentBuilderTest
{
    [Fact]
    public void AppendString_AppendsAString()
    {
        // Arrange
        var content = new HtmlContentBuilder();

        // Act
        content.Append("Hello");

        // Assert
        Assert.Equal(1, content.Count);
        var result = Assert.Single(content.Entries);
        Assert.IsType<string>(result);
    }

    [Fact]
    public void AppendString_WrittenAsEncoded()
    {
        // Arrange
        var content = new HtmlContentBuilder();
        content.Append("Hello");

        var writer = new StringWriter();

        // Act
        content.WriteTo(writer, new HtmlTestEncoder());

        // Assert
        Assert.Equal("HtmlEncode[[Hello]]", writer.ToString());
    }

    [Fact]
    public void AppendHtml_DoesNotGetWrittenAsEncoded()
    {
        // Arrange
        var content = new HtmlContentBuilder();
        content.AppendHtml("Hello");

        var writer = new StringWriter();

        // Act
        content.WriteTo(writer, new HtmlTestEncoder());

        // Assert
        Assert.Equal("Hello", writer.ToString());
    }

    [Fact]
    public void AppendIHtmlContent_AppendsAsIs()
    {
        // Arrange
        var content = new HtmlContentBuilder();
        var writer = new StringWriter();

        // Act
        content.AppendHtml(new TestHtmlContent("Hello"));

        // Assert
        Assert.Equal(1, content.Count);
        var result = Assert.Single(content.Entries);
        var testHtmlContent = Assert.IsType<TestHtmlContent>(result);
        testHtmlContent.WriteTo(writer, new HtmlTestEncoder());
        Assert.Equal("Written from TestHtmlContent: Hello", writer.ToString());
    }

    [Fact]
    public void CanAppendMultipleItems()
    {
        // Arrange
        var content = new HtmlContentBuilder();

        // Act
        content.AppendHtml(new TestHtmlContent("hello"));
        content.Append("Test");

        // Assert
        Assert.Equal(2, content.Count);
        Assert.Collection(
            content.Entries,
            entry => Assert.Equal("Written from TestHtmlContent: hello", entry.ToString()),
            entry => Assert.Equal("Test", entry));
    }

    [Fact]
    public void Clear_DeletesAllItems()
    {
        // Arrange
        var content = new HtmlContentBuilder();
        content.AppendHtml(new TestHtmlContent("hello"));
        content.Append("Test");

        // Act
        content.Clear();

        // Assert
        Assert.Equal(0, content.Count);
        Assert.Empty(content.Entries);
    }

    [Fact]
    public void CopyTo_CopiesAllItems()
    {
        // Arrange
        var source = new HtmlContentBuilder();
        source.AppendHtml(new TestHtmlContent("hello"));
        source.Append("Test");

        var destination = new HtmlContentBuilder();
        destination.Append("some-content");

        // Act
        source.CopyTo(destination);

        // Assert
        Assert.Equal(2, source.Count);
        Assert.Equal(3, destination.Count);
        Assert.Collection(
            destination.Entries,
            entry => Assert.Equal("some-content", Assert.IsType<string>(entry)),
            entry => Assert.Equal(new TestHtmlContent("hello"), Assert.IsType<TestHtmlContent>(entry)),
            entry => Assert.Equal("Test", Assert.IsType<string>(entry)));
    }

    [Fact]
    public void CopyTo_DoesDeepCopy()
    {
        // Arrange
        var source = new HtmlContentBuilder();

        var nested = new HtmlContentBuilder();
        source.AppendHtml(nested);
        nested.AppendHtml(new TestHtmlContent("hello"));
        source.Append("Test");

        var destination = new HtmlContentBuilder();
        destination.Append("some-content");

        // Act
        source.CopyTo(destination);

        // Assert
        Assert.Equal(2, source.Count);
        Assert.Equal(1, nested.Count);
        Assert.Equal(3, destination.Count);
        Assert.Collection(
            destination.Entries,
            entry => Assert.Equal("some-content", Assert.IsType<string>(entry)),
            entry => Assert.Equal(new TestHtmlContent("hello"), Assert.IsType<TestHtmlContent>(entry)),
            entry => Assert.Equal("Test", Assert.IsType<string>(entry)));
    }

    [Fact]
    public void MoveTo_CopiesAllItems_AndClears()
    {
        // Arrange
        var source = new HtmlContentBuilder();
        source.AppendHtml(new TestHtmlContent("hello"));
        source.Append("Test");

        var destination = new HtmlContentBuilder();
        destination.Append("some-content");

        // Act
        source.MoveTo(destination);

        // Assert
        Assert.Equal(0, source.Count);
        Assert.Equal(3, destination.Count);
        Assert.Collection(
            destination.Entries,
            entry => Assert.Equal("some-content", Assert.IsType<string>(entry)),
            entry => Assert.Equal(new TestHtmlContent("hello"), Assert.IsType<TestHtmlContent>(entry)),
            entry => Assert.Equal("Test", Assert.IsType<string>(entry)));
    }

    [Fact]
    public void MoveTo_DoesDeepMove()
    {
        // Arrange
        var source = new HtmlContentBuilder();

        var nested = new HtmlContentBuilder();
        source.AppendHtml(nested);
        nested.AppendHtml(new TestHtmlContent("hello"));
        source.Append("Test");

        var destination = new HtmlContentBuilder();
        destination.Append("some-content");

        // Act
        source.MoveTo(destination);

        // Assert
        Assert.Equal(0, source.Count);
        Assert.Equal(0, nested.Count);
        Assert.Equal(3, destination.Count);
        Assert.Collection(
            destination.Entries,
            entry => Assert.Equal("some-content", Assert.IsType<string>(entry)),
            entry => Assert.Equal(new TestHtmlContent("hello"), Assert.IsType<TestHtmlContent>(entry)),
            entry => Assert.Equal("Test", Assert.IsType<string>(entry)));
    }

    [Fact]
    public void WriteTo_WritesAllItems()
    {
        // Arrange
        var content = new HtmlContentBuilder();
        var writer = new StringWriter();
        content.AppendHtml(new TestHtmlContent("Hello"));
        content.Append("Test");

        // Act
        content.WriteTo(writer, new HtmlTestEncoder());

        // Assert
        Assert.Equal(2, content.Count);
        Assert.Equal("Written from TestHtmlContent: HelloHtmlEncode[[Test]]", writer.ToString());
    }

    private class TestHtmlContent : IHtmlContent, IEquatable<TestHtmlContent>
    {
        private string _content;

        public TestHtmlContent(string content)
        {
            _content = content;
        }

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            writer.Write(ToString());
        }

        public override string ToString()
        {
            return "Written from TestHtmlContent: " + _content;
        }

        public override int GetHashCode()
        {
            return _content.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            var other = obj as TestHtmlContent;
            if (other != null)
            {
                return Equals(other);
            }

            return base.Equals(obj);
        }

        public bool Equals(TestHtmlContent? other)
        {
            return other != null && string.Equals(_content, other._content);
        }
    }
}
