// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public class ViewBufferTest
    {
        [Fact]
        public void Append_AddsEncodingWrapper()
        {
            // Arrange
            var buffer = new ViewBuffer(new TestViewBufferScope(), "some-name", pageSize: 32);

            // Act
            buffer.Append("Hello world");

            // Assert
            var page = Assert.Single(buffer.Pages);
            Assert.Equal(1, page.Count);
            Assert.IsAssignableFrom<IHtmlContent>(page.Buffer[0].Value);
        }

        [Fact]
        public void AppendHtml_AddsHtmlContentRazorValue()
        {
            // Arrange
            var buffer = new ViewBuffer(new TestViewBufferScope(), "some-name", pageSize: 32);
            var content = new HtmlString("hello-world");

            // Act
            buffer.AppendHtml(content);

            // Assert
            var page = Assert.Single(buffer.Pages);
            Assert.Equal(1, page.Count);
            Assert.Same(content, page.Buffer[0].Value);
        }

        [Fact]
        public void AppendHtml_AddsString()
        {
            // Arrange
            var buffer = new ViewBuffer(new TestViewBufferScope(), "some-name", pageSize: 32);
            var value = "Hello world";

            // Act
            buffer.AppendHtml(value);

            // Assert
            var page = Assert.Single(buffer.Pages);
            Assert.Equal(1, page.Count);
            Assert.Equal("Hello world", Assert.IsType<string>(page.Buffer[0].Value));
        }

        [Fact]
        public void Append_CreatesNewPages_WhenCurrentPageIsFull()
        {
            // Arrange
            var buffer = new ViewBuffer(new TestViewBufferScope(), "some-name", pageSize: 32);
            var expected = Enumerable.Range(0, 32).Select(i => i.ToString());

            // Act
            foreach (var item in expected)
            {
                buffer.AppendHtml(item);
            }
            buffer.AppendHtml("Hello");
            buffer.AppendHtml("world");

            // Assert
            Assert.Equal(2, buffer.Pages.Count);
            Assert.Collection(buffer.Pages,
                page => Assert.Equal(expected, page.Buffer.Select(v => v.Value)),
                page =>
                {
                    var array = page.Buffer;
                    Assert.Equal("Hello", array[0].Value);
                    Assert.Equal("world", array[1].Value);
                });
        }

        [Theory]
        [InlineData(1)]
        [InlineData(35)]
        public void Clear_ResetsBackingBufferAndIndex(int valuesToWrite)
        {
            // Arrange
            var buffer = new ViewBuffer(new TestViewBufferScope(), "some-name", pageSize: 32);

            // Act
            for (var i = 0; i < valuesToWrite; i++)
            {
                buffer.AppendHtml("Hello");
            }
            buffer.Clear();
            buffer.AppendHtml("world");

            // Assert
            var page = Assert.Single(buffer.Pages);
            Assert.Equal(1, page.Count);
            Assert.Equal("world", page.Buffer[0].Value);
        }

        [Fact]
        public void WriteTo_WritesRazorValues_ToTextWriter()
        {
            // Arrange
            var buffer = new ViewBuffer(new TestViewBufferScope(), "some-name", pageSize: 32);
            var writer = new StringWriter();

            // Act
            buffer.Append("Hello");
            buffer.AppendHtml(new HtmlString(" world"));
            buffer.AppendHtml(" 123");
            buffer.WriteTo(writer, new HtmlTestEncoder());

            // Assert
            Assert.Equal("HtmlEncode[[Hello]] world 123", writer.ToString());
        }

        [Theory]
        [InlineData(9)]
        [InlineData(10)]
        [InlineData(11)]
        [InlineData(23)]
        public void WriteTo_WritesRazorValuesFromAllBuffers(int valuesToWrite)
        {
            // Arrange
            var buffer = new ViewBuffer(new TestViewBufferScope(), "some-name", pageSize: 4);
            var writer = new StringWriter();
            var expected = string.Join("", Enumerable.Range(0, valuesToWrite).Select(_ => "abc"));

            // Act
            for (var i = 0; i < valuesToWrite; i++)
            {
                buffer.AppendHtml("abc");
            }
            buffer.WriteTo(writer, new HtmlTestEncoder());

            // Assert
            Assert.Equal(expected, writer.ToString());
        }

        [Fact]
        public async Task WriteToAsync_WritesRazorValues_ToTextWriter()
        {
            // Arrange
            var buffer = new ViewBuffer(new TestViewBufferScope(), "some-name", pageSize: 128);
            var writer = new StringWriter();

            // Act
            buffer.Append("Hello");
            buffer.AppendHtml(new HtmlString(" world"));
            buffer.AppendHtml(" 123");

            await buffer.WriteToAsync(writer, new HtmlTestEncoder());

            // Assert
            Assert.Equal("HtmlEncode[[Hello]] world 123", writer.ToString());
        }

        [Theory]
        [InlineData(9)]
        [InlineData(10)]
        [InlineData(11)]
        [InlineData(23)]
        public async Task WriteToAsync_WritesRazorValuesFromAllBuffers(int valuesToWrite)
        {
            // Arrange
            var buffer = new ViewBuffer(new TestViewBufferScope(), "some-name", pageSize: 4);
            var writer = new StringWriter();
            var expected = string.Join("", Enumerable.Range(0, valuesToWrite).Select(_ => "abc"));

            // Act
            for (var i = 0; i < valuesToWrite; i++)
            {
                buffer.AppendHtml("abc");
            }

            await buffer.WriteToAsync(writer, new HtmlTestEncoder());

            // Assert
            Assert.Equal(expected, writer.ToString());
        }

        [Fact]
        public void CopyTo_Flattens()
        {
            // Arrange
            var buffer = new ViewBuffer(new TestViewBufferScope(), "some-name", pageSize: 4);

            var nestedItems = new List<object>();
            var nested = new HtmlContentBuilder(nestedItems);
            nested.AppendHtml("Hello");
            buffer.AppendHtml(nested);

            var destinationItems = new List<object>();
            var destination = new HtmlContentBuilder(destinationItems);

            // Act
            buffer.CopyTo(destination);

            // Assert
            Assert.Same(nested, buffer.Pages[0].Buffer[0].Value);
            Assert.Equal("Hello", Assert.IsType<HtmlString>(nestedItems[0]).Value);
            Assert.Equal("Hello", Assert.IsType<HtmlString>(destinationItems[0]).Value);
        }

        [Fact]
        public void MoveTo_FlattensAndClears()
        {
            // Arrange
            var buffer = new ViewBuffer(new TestViewBufferScope(), "some-name", pageSize: 4);

            var nestedItems = new List<object>();
            var nested = new HtmlContentBuilder(nestedItems);
            nested.AppendHtml("Hello");
            buffer.AppendHtml(nested);

            var destinationItems = new List<object>();
            var destination = new HtmlContentBuilder(destinationItems);

            // Act
            buffer.MoveTo(destination);

            // Assert
            Assert.Empty(nestedItems);
            Assert.Empty(buffer.Pages);
            Assert.Equal("Hello", Assert.IsType<HtmlString>(destinationItems[0]).Value);
        }

        [Fact]
        public void MoveTo_ViewBuffer_TakesPage_IfOriginalIsEmpty()
        {
            // Arrange
            var scope = new TestViewBufferScope();

            var original = new ViewBuffer(scope, "original", pageSize: 4);
            var other = new ViewBuffer(scope, "other", pageSize: 4);

            other.AppendHtml("Hi");

            var page = other.Pages[0];

            // Act
            other.MoveTo(original);

            // Assert
            Assert.Empty(other.Pages); // Page was taken
            Assert.Same(page, Assert.Single(original.Pages));
        }

        [Fact]
        public void MoveTo_ViewBuffer_TakesPage_IfCurrentPageInOriginalIsFull()
        {
            // Arrange
            var scope = new TestViewBufferScope();

            var original = new ViewBuffer(scope, "original", pageSize: 4);
            var other = new ViewBuffer(scope, "other", pageSize: 4);

            for (var i = 0; i < 4; i++)
            {
                original.AppendHtml($"original-{i}");
            }

            other.AppendHtml("Hi");

            var page = other.Pages[0];

            // Act
            other.MoveTo(original);

            // Assert
            Assert.Empty(other.Pages); // Page was taken
            Assert.Equal(2, original.Pages.Count);
            Assert.Same(page, original.Pages[1]);
        }

        [Fact]
        public void MoveTo_ViewBuffer_TakesPage_IfCurrentPageDoesNotHaveCapacity()
        {
            // Arrange
            var scope = new TestViewBufferScope();

            var original = new ViewBuffer(scope, "original", pageSize: 4);
            var other = new ViewBuffer(scope, "other", pageSize: 4);

            for (var i = 0; i < 3; i++)
            {
                original.AppendHtml($"original-{i}");
            }

            // With two items, we'd try to copy the items, but there's no room in the current page.
            // So we just take over the page.
            for (var i = 0; i < 2; i++)
            {
                other.AppendHtml($"other-{i}");
            }

            var page = other.Pages[0];

            // Act
            other.MoveTo(original);

            // Assert
            Assert.Empty(other.Pages); // Page was taken
            Assert.Equal(2, original.Pages.Count);
            Assert.Same(page, original.Pages[1]);
        }

        [Fact]
        public void MoveTo_ViewBuffer_CopiesItems_IfCurrentPageHasRoom()
        {
            // Arrange
            var scope = new TestViewBufferScope();

            var original = new ViewBuffer(scope, "original", pageSize: 4);
            var other = new ViewBuffer(scope, "other", pageSize: 4);

            for (var i = 0; i < 2; i++)
            {
                original.AppendHtml($"original-{i}");
            }

            // With two items, this is half full so we try to copy the items.
            for (var i = 0; i < 2; i++)
            {
                other.AppendHtml($"other-{i}");
            }

            var page = other.Pages[0];

            // Act
            other.MoveTo(original);

            // Assert
            Assert.Empty(other.Pages); // Other is cleared
            Assert.Contains(page.Buffer, scope.ReturnedBuffers); // Buffer was returned

            Assert.Collection(
                Assert.Single(original.Pages).Buffer,
                item => Assert.Equal("original-0", item.Value),
                item => Assert.Equal("original-1", item.Value),
                item => Assert.Equal("other-0", item.Value),
                item => Assert.Equal("other-1", item.Value));
        }

        [Fact]
        public void MoveTo_ViewBuffer_CanAddToTakenPage()
        {
            // Arrange
            var scope = new TestViewBufferScope();

            var original = new ViewBuffer(scope, "original", pageSize: 4);
            var other = new ViewBuffer(scope, "other", pageSize: 4);

            for (var i = 0; i < 3; i++)
            {
                original.AppendHtml($"original-{i}");
            }

            // More than half full, so we take the page
            for (var i = 0; i < 3; i++)
            {
                other.AppendHtml($"other-{i}");
            }

            var page = other.Pages[0];
            other.MoveTo(original);

            // Act
            original.AppendHtml("after-merge");

            // Assert
            Assert.Empty(other.Pages); // Other is cleared

            Assert.Equal(2, original.Pages.Count);
            Assert.Collection(
                original.Pages[0].Buffer,
                item => Assert.Equal("original-0", item.Value),
                item => Assert.Equal("original-1", item.Value),
                item => Assert.Equal("original-2", item.Value),
                item => Assert.Null(item.Value));
            Assert.Collection(
                original.Pages[1].Buffer,
                item => Assert.Equal("other-0", item.Value),
                item => Assert.Equal("other-1", item.Value),
                item => Assert.Equal("other-2", item.Value),
                item => Assert.Equal("after-merge", item.Value));
        }

        [Fact]
        public void MoveTo_ViewBuffer_MultiplePages()
        {
            // Arrange
            var scope = new TestViewBufferScope();

            var original = new ViewBuffer(scope, "original", pageSize: 4);
            var other = new ViewBuffer(scope, "other", pageSize: 4);

            for (var i = 0; i < 2; i++)
            {
                original.AppendHtml($"original-{i}");
            }
            
            for (var i = 0; i < 9; i++)
            {
                other.AppendHtml($"other-{i}");
            }

            var pages = new List<ViewBufferPage>(other.Pages);

            // Act
            other.MoveTo(original);

            // Assert
            Assert.Empty(other.Pages); // Other is cleared

            Assert.Equal(4, original.Pages.Count);
            Assert.Collection(
                original.Pages[0].Buffer,
                item => Assert.Equal("original-0", item.Value),
                item => Assert.Equal("original-1", item.Value),
                item => Assert.Null(item.Value),
                item => Assert.Null(item.Value));
            Assert.Collection(
                original.Pages[1].Buffer,
                item => Assert.Equal("other-0", item.Value),
                item => Assert.Equal("other-1", item.Value),
                item => Assert.Equal("other-2", item.Value),
                item => Assert.Equal("other-3", item.Value));
            Assert.Collection(
                original.Pages[2].Buffer,
                item => Assert.Equal("other-4", item.Value),
                item => Assert.Equal("other-5", item.Value),
                item => Assert.Equal("other-6", item.Value),
                item => Assert.Equal("other-7", item.Value));
            Assert.Collection(
                original.Pages[3].Buffer,
                item => Assert.Equal("other-8", item.Value),
                item => Assert.Null(item.Value),
                item => Assert.Null(item.Value),
                item => Assert.Null(item.Value));
        }
    }
}
