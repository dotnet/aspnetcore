// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Framework.WebEncoders;
using Microsoft.Framework.WebEncoders.Testing;
using Xunit;

namespace Microsoft.AspNet.Html.Abstractions.Test
{
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
        public void Builder_AppendLineEncoded_String()
        {
            // Arrange
            var builder = new TestHtmlContentBuilder();

            // Act
            builder.AppendLineEncoded("Hi");

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
            builder.SetContent(content);

            // Assert
            Assert.Collection(
                builder.Entries,
                entry => Assert.Same(content, entry));
        }

        [Fact]
        public void Builder_SetContentEncoded_String()
        {
            // Arrange
            var builder = new TestHtmlContentBuilder();
            builder.Append("Existing Content. Will be Cleared.");

            // Act
            builder.SetContentEncoded("Hi");

            // Assert
            Assert.Collection(
                builder.Entries,
                entry => Assert.Equal("Hi", Assert.IsType<EncodedString>(entry).Value));
        }

        private static string HtmlContentToString(IHtmlContent content)
        {
            using (var writer = new StringWriter())
            {
                content.WriteTo(writer, new CommonTestEncoder());
                return writer.ToString();
            }
        }

        private class TestHtmlContentBuilder : IHtmlContentBuilder
        {
            public List<IHtmlContent> Entries { get; } = new List<IHtmlContent>();

            public IHtmlContentBuilder Append(string unencoded)
            {
                Entries.Add(new UnencodedString(unencoded));
                return this;
            }

            public IHtmlContentBuilder Append(IHtmlContent content)
            {
                Entries.Add(content);
                return this;
            }

            public IHtmlContentBuilder AppendEncoded(string encoded)
            {
                Entries.Add(new EncodedString(encoded));
                return this;
            }

            public void Clear()
            {
                Entries.Clear();
            }

            public void WriteTo(TextWriter writer, IHtmlEncoder encoder)
            {
                throw new NotImplementedException();
            }
        }

        private class EncodedString : IHtmlContent
        {
            public EncodedString(string value)
            {
                Value = value;
            }

            public string Value { get; }

            public void WriteTo(TextWriter writer, IHtmlEncoder encoder)
            {
                throw new NotImplementedException();
            }
        }

        private class UnencodedString : IHtmlContent
        {
            public UnencodedString(string value)
            {
                Value = value;
            }

            public string Value { get; }

            public void WriteTo(TextWriter writer, IHtmlEncoder encoder)
            {
                throw new NotImplementedException();
            }
        }

        private class OtherHtmlContent : IHtmlContent
        {
            public OtherHtmlContent(string value)
            {
                Value = value;
            }

            public string Value { get; }

            public void WriteTo(TextWriter writer, IHtmlEncoder encoder)
            {
                throw new NotImplementedException();
            }
        }
    }
}
