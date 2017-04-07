// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class PageDirectiveFeatureTest
    {
        [Fact]
        public void TryGetPageDirective_FindsTemplate()
        {
            // Arrange
            string template;
            var projectItem = new TestRazorProjectItem(@"@page ""Some/Path/{value}""
The rest of the thing");

            // Act & Assert
            Assert.True(PageDirectiveFeature.TryGetPageDirective(projectItem, out template));
            Assert.Equal("Some/Path/{value}", template);
        }

        [Fact]
        public void TryGetPageDirective_NoNewLine()
        {
            // Arrange
            string template;
            var projectItem = new TestRazorProjectItem(@"@page ""Some/Path/{value}""");

            // Act & Assert
            Assert.True(PageDirectiveFeature.TryGetPageDirective(projectItem, out template));
            Assert.Equal("Some/Path/{value}", template);
        }

        [Fact]
        public void TryGetPageDirective_JunkBeforeDirective()
        {
            // Arrange
            string template;
            var projectItem = new TestRazorProjectItem(@"Not a directive @page ""Some/Path/{value}""");

            // Act & Assert
            Assert.False(PageDirectiveFeature.TryGetPageDirective(projectItem, out template));
            Assert.Null(template);
        }

        [Fact]
        public void TryGetPageDirective_MultipleQuotes()
        {
            // Arrange
            string template;
            var projectItem = new TestRazorProjectItem(@"@page """"template""""");

            // Act & Assert
            Assert.True(PageDirectiveFeature.TryGetPageDirective(projectItem, out template));
            Assert.Equal(@"""template""", template);
        }

        [Theory]
        [InlineData(@"""Some/Path/{value}")]
        [InlineData(@"Some/Path/{value}""")]
        public void TryGetPageDirective_RequiresBothQuotes(string inTemplate)
        {
            // Arrange
            string template;
            var projectItem = new TestRazorProjectItem($@"@page {inTemplate}");

            // Act & Assert
            Assert.True(PageDirectiveFeature.TryGetPageDirective(projectItem, out template));
            Assert.Equal(string.Empty, template);
        }


        [Fact]
        public void TryGetPageDirective_NoQuotesAroundPath_IsNotTemplate()
        {
            // Arrange
            string template;
            var projectItem = new TestRazorProjectItem(@"@page Some/Path/{value}");

            // Act & Assert
            Assert.True(PageDirectiveFeature.TryGetPageDirective(projectItem, out template));
            Assert.Equal(string.Empty, template);
        }

        [Fact]
        public void TryGetPageDirective_WrongNewLine()
        {
            // Arrange
            var wrongNewLine = Environment.NewLine == "\r\n" ? "\n" : "\r\n";

            string template;
            var projectItem = new TestRazorProjectItem($"@page \"Some/Path/{{value}}\" {wrongNewLine}");

            // Act & Assert
            Assert.True(PageDirectiveFeature.TryGetPageDirective(projectItem, out template));
            Assert.Equal("Some/Path/{value}", template);
        }

        [Fact]
        public void TryGetPageDirective_NewLineBeforeDirective()
        {
            // Arrange
            string template;
            var projectItem = new TestRazorProjectItem("\n @page \"Some/Path/{value}\"");

            // Act
            Assert.True(PageDirectiveFeature.TryGetPageDirective(projectItem, out template));
            Assert.Equal("Some/Path/{value}", template);
        }

        [Fact]
        public void TryGetPageDirective_WhitespaceBeforeDirective()
        {
            // Arrange
            string template;
            var projectItem = new TestRazorProjectItem(@"   @page ""Some/Path/{value}""
");

            // Act & Assert
            Assert.True(PageDirectiveFeature.TryGetPageDirective(projectItem, out template));
            Assert.Equal("Some/Path/{value}", template);
        }

        [Fact(Skip = "Re-evaluate this scenario after we use Razor to parse this stuff")]
        public void TryGetPageDirective_JunkBeforeNewline()
        {
            // Arrange
            string template;
            var projectItem = new TestRazorProjectItem(@"@page ""Some/Path/{value}"" things that are not the path
a new line");

            // Act & Assert
            Assert.True(PageDirectiveFeature.TryGetPageDirective(projectItem, out template));
            Assert.Empty(template);
        }

        [Fact]
        public void TryGetPageDirective_Directive_WithoutPathOrContent()
        {
            // Arrange
            string template;
            var projectItem = new TestRazorProjectItem(@"@page");

            // Act & Assert
            Assert.True(PageDirectiveFeature.TryGetPageDirective(projectItem, out template));
            Assert.Empty(template);
        }

        [Fact]
        public void TryGetPageDirective_DirectiveWithContent_WithoutPath()
        {
            // Arrange
            string template;
            var projectItem = new TestRazorProjectItem(@"@page
Non-path things");

            // Act & Assert
            Assert.True(PageDirectiveFeature.TryGetPageDirective(projectItem, out template));
            Assert.Empty(template);
        }

        [Fact]
        public void TryGetPageDirective_NoDirective()
        {
            // Arrange
            string template;
            var projectItem = new TestRazorProjectItem(@"This is junk
Nobody will use it");

            // Act & Assert
            Assert.False(PageDirectiveFeature.TryGetPageDirective(projectItem, out template));
            Assert.Null(template);
        }

        [Fact]
        public void TryGetPageDirective_EmptyStream()
        {
            // Arrange
            string template;
            var projectItem = new TestRazorProjectItem(string.Empty);

            // Act
            Assert.False(PageDirectiveFeature.TryGetPageDirective(projectItem, out template));
            Assert.Null(template);
        }

        [Fact]
        public void TryGetPageDirective_NullProject()
        {
            // Arrange
            string template;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => PageDirectiveFeature.TryGetPageDirective(projectItem: null, template: out template));
        }

        [Fact]
        public void TryGetPageDirective_NullStream()
        {
            // Arrange
            string template;
            var projectItem = new TestRazorProjectItem(content: null);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => PageDirectiveFeature.TryGetPageDirective(projectItem, out template));
        }
    }

    public class TestRazorProjectItem : RazorProjectItem
    {
        private string _content;

        public TestRazorProjectItem(string content)
        {
            _content = content;
        }

        public override string BasePath
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool Exists
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string Path
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string PhysicalPath
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override Stream Read()
        {
            if (_content == null)
            {
                return null;
            }
            else
            {
                return new MemoryStream(Encoding.UTF8.GetBytes(_content));
            }
        }
    }
}
