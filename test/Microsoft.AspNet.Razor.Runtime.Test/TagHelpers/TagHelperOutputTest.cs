// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class TagHelperOutputTest
    {
        [Fact]
        public void TagName_CannotSetToNullInCtor()
        {
            // Arrange & Act
            var tagHelperOutput = new TagHelperOutput(null);

            // Assert
            Assert.Empty(tagHelperOutput.TagName);
        }

        [Fact]
        public void TagName_CannotSetToNull()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p");

            // Act
            tagHelperOutput.TagName = null;

            // Assert
            Assert.Empty(tagHelperOutput.TagName);
        }

        [Fact]
        public void Content_CannotSetToNull()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p");

            // Act
            tagHelperOutput.Content = null;

            // Assert
            Assert.Empty(tagHelperOutput.Content);
        }

        [Fact]
        public void GenerateStartTag_ReturnsFullStartTag()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p", attributes:
                new Dictionary<string, string>
                {
                    { "class", "btn" },
                    { "something", "   spaced    " }
                });

            // Act
            var output = tagHelperOutput.GenerateStartTag();

            // Assert
            Assert.Equal("<p class=\"btn\" something=\"   spaced    \">", output);
        }

        [Fact]
        public void GenerateStartTag_ReturnsNoAttributeStartTag()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p");

            // Act
            var output = tagHelperOutput.GenerateStartTag();

            // Assert
            Assert.Equal("<p>", output);
        }

        [Fact]
        public void GenerateStartTag_ReturnsSelfClosingStartTag_Attributes()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p", 
                attributes: new Dictionary<string, string>
                {
                    { "class", "btn" },
                    { "something", "   spaced    " }
                });

            tagHelperOutput.SelfClosing = true;

            // Act
            var output = tagHelperOutput.GenerateStartTag();

            // Assert
            Assert.Equal("<p class=\"btn\" something=\"   spaced    \" />", output);
        }

        [Fact]
        public void GenerateStartTag_ReturnsSelfClosingStartTag_NoAttributes()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p");

            tagHelperOutput.SelfClosing = true;

            // Act
            var output = tagHelperOutput.GenerateStartTag();

            // Assert
            Assert.Equal("<p />", output);
        }

        [Fact]
        public void GenerateStartTag_ReturnsNothingIfWhitespaceTagName()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("  ", 
                attributes: new Dictionary<string, string>
                {
                    { "class", "btn" },
                    { "something", "   spaced    " }
                });

            tagHelperOutput.SelfClosing = true;

            // Act
            var output = tagHelperOutput.GenerateStartTag();

            // Assert
            Assert.Empty(output);
        }


        [Fact]
        public void GenerateEndTag_ReturnsNothingIfWhitespaceTagName()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput(" "); ;

            tagHelperOutput.Content = "Hello World";

            // Act
            var output = tagHelperOutput.GenerateEndTag();

            // Assert
            Assert.Empty(output);
        }

        [Fact]
        public void GenerateContent_ReturnsContent()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p");

            tagHelperOutput.Content = "Hello World";

            // Act
            var output = tagHelperOutput.GenerateContent();

            // Assert
            Assert.Equal("Hello World", output);
        }


        [Fact]
        public void GenerateContent_ReturnsNothingIfSelfClosing()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p")
            {
                SelfClosing = true
            };

            tagHelperOutput.Content = "Hello World";

            // Act
            var output = tagHelperOutput.GenerateContent();

            // Assert
            Assert.Empty(output);
        }

        [Fact]
        public void GenerateEndTag_ReturnsEndTag()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p");

            // Act
            var output = tagHelperOutput.GenerateEndTag();

            // Assert
            Assert.Equal("</p>", output);
        }

        [Fact]
        public void GenerateEndTag_ReturnsNothingIfSelfClosing()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p")
            {
                SelfClosing = true
            };

            // Act
            var output = tagHelperOutput.GenerateEndTag();

            // Assert
            Assert.Empty(output);
        }
    }
}