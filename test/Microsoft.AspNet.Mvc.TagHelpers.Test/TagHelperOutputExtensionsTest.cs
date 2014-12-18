// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Xunit;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    public class TagHelperOutputExtensionsTest
    {
        [Theory]
        [InlineData("hello", "world")]
        [InlineData("HeLlO", "wOrLd")]
        public void CopyHtmlAttribute_CopiesOriginalAttributes(string attributeName, string attributeValue)
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput(
                "p",
                attributes: new Dictionary<string, string>());
            var tagHelperContext = new TagHelperContext(
                allAttributes: new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    { attributeName, attributeValue }
                },
                uniqueId: "test",
                getChildContentAsync: () => Task.FromResult("Something"));
            var expectedAttribute = new KeyValuePair<string, string>(attributeName, attributeValue);

            // Act
            tagHelperOutput.CopyHtmlAttribute("hello", tagHelperContext);

            // Assert
            var attribute = Assert.Single(tagHelperOutput.Attributes);
            Assert.Equal(expectedAttribute, attribute);
        }

        [Fact]
        public void CopyHtmlAttribute_DoesNotOverrideAttributes()
        {
            // Arrange
            var attributeName = "hello";
            var tagHelperOutput = new TagHelperOutput(
                "p",
                attributes: new Dictionary<string, string>()
                {
                    { attributeName, "world2" }
                });
            var expectedAttribute = new KeyValuePair<string, string>(attributeName, "world2");
            var tagHelperContext = new TagHelperContext(
                allAttributes: new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    { attributeName, "world" }
                },
                uniqueId: "test",
                getChildContentAsync: () => Task.FromResult("Something"));

            // Act
            tagHelperOutput.CopyHtmlAttribute(attributeName, tagHelperContext);

            // Assert
            var attribute = Assert.Single(tagHelperOutput.Attributes);
            Assert.Equal(expectedAttribute, attribute);
        }

        [Fact]
        public void RemoveRange_RemovesProvidedAttributes()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput(
                "p",
                attributes: new Dictionary<string, string>()
                {
                    { "route-Hello", "World" },
                    { "Route-I", "Am" }
                });
            var expectedAttribute = new KeyValuePair<string, string>("type", "btn");
            tagHelperOutput.Attributes.Add(expectedAttribute);
            var attributes = tagHelperOutput.FindPrefixedAttributes("route-");

            // Act
            tagHelperOutput.RemoveRange(attributes);

            // Assert
            var attribute = Assert.Single(tagHelperOutput.Attributes);
            Assert.Equal(expectedAttribute, attribute);
        }

        [Fact]
        public void FindPrefixedAttributes_ReturnsEmpty_AttributeListIfNoAttributesPrefixed()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput(
                "p",
                attributes: new Dictionary<string, string>()
                {
                    { "routeHello", "World" },
                    { "Routee-I", "Am" }
                });

            // Act
            var attributes = tagHelperOutput.FindPrefixedAttributes("route-");

            // Assert
            Assert.Empty(attributes);
            var attribute = Assert.Single(tagHelperOutput.Attributes, kvp => kvp.Key.Equals("routeHello"));
            Assert.Equal(attribute.Value, "World");
            attribute = Assert.Single(tagHelperOutput.Attributes, kvp => kvp.Key.Equals("Routee-I"));
            Assert.Equal(attribute.Value, "Am");
        }

        [Fact]
        public void MergeAttributes_DoesNotReplace_TagHelperOutputAttributeValues()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput(
                "p",
                attributes: new Dictionary<string, string>());
            var expectedAttribute = new KeyValuePair<string, string>("type", "btn");
            tagHelperOutput.Attributes.Add(expectedAttribute);

            var tagBuilder = new TagBuilder("p");
            tagBuilder.Attributes.Add("type", "hello");

            // Act
            tagHelperOutput.MergeAttributes(tagBuilder);

            // Assert
            var attribute = Assert.Single(tagHelperOutput.Attributes);
            Assert.Equal(expectedAttribute, attribute);
        }

        [Fact]
        public void MergeAttributes_AppendsClass_TagHelperOutputAttributeValues()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput(
                "p",
                attributes: new Dictionary<string, string>());
            tagHelperOutput.Attributes.Add("class", "Hello");

            var tagBuilder = new TagBuilder("p");
            tagBuilder.Attributes.Add("class", "btn");

            var expectedAttribute = new KeyValuePair<string, string>("class", "Hello btn");

            // Act
            tagHelperOutput.MergeAttributes(tagBuilder);

            // Assert
            var attribute = Assert.Single(tagHelperOutput.Attributes);
            Assert.Equal(expectedAttribute, attribute);
        }

        [Theory]
        [InlineData("class", "CLAss")]
        [InlineData("ClaSS", "class")]
        [InlineData("ClaSS", "cLaSs")]
        public void MergeAttributes_AppendsClass_TagHelperOutputAttributeValues_IgnoresCase(
            string originalName, string updateName)
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput(
                "p",
                attributes: new Dictionary<string, string>());
            tagHelperOutput.Attributes.Add(originalName, "Hello");

            var tagBuilder = new TagBuilder("p");
            tagBuilder.Attributes.Add(updateName, "btn");

            // Act
            tagHelperOutput.MergeAttributes(tagBuilder);

            // Assert
            var attribute = Assert.Single(tagHelperOutput.Attributes);
            Assert.Equal(new KeyValuePair<string, string>(originalName, "Hello btn"), attribute);
        }

        [Fact]
        public void MergeAttributes_DoesNotEncode_TagHelperOutputAttributeValues()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput(
                "p",
                attributes: new Dictionary<string, string>());

            var tagBuilder = new TagBuilder("p");
            var expectedAttribute = new KeyValuePair<string, string>("visible", "val < 3");
            tagBuilder.Attributes.Add(expectedAttribute);

            // Act
            tagHelperOutput.MergeAttributes(tagBuilder);

            // Assert
            var attribute = Assert.Single(tagHelperOutput.Attributes);
            Assert.Equal(expectedAttribute, attribute);
        }

        [Fact]
        public void MergeAttributes_CopiesMultiple_TagHelperOutputAttributeValues()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput(
                "p",
                attributes: new Dictionary<string, string>());

            var tagBuilder = new TagBuilder("p");
            var expectedAttribute1 = new KeyValuePair<string, string>("class", "btn");
            var expectedAttribute2 = new KeyValuePair<string, string>("class2", "btn");
            tagBuilder.Attributes.Add(expectedAttribute1);
            tagBuilder.Attributes.Add(expectedAttribute2);

            // Act
            tagHelperOutput.MergeAttributes(tagBuilder);

            // Assert
            Assert.Equal(2, tagHelperOutput.Attributes.Count);
            var attribute = Assert.Single(tagHelperOutput.Attributes, kvp => kvp.Key.Equals("class"));
            Assert.Equal(expectedAttribute1.Value, attribute.Value);
            attribute = Assert.Single(tagHelperOutput.Attributes, kvp => kvp.Key.Equals("class2"));
            Assert.Equal(expectedAttribute2.Value, attribute.Value);
        }

        [Fact]
        public void MergeAttributes_Maintains_TagHelperOutputAttributeValues()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput(
                "p",
                attributes: new Dictionary<string, string>());
            var expectedAttribute = new KeyValuePair<string, string>("class", "btn");
            tagHelperOutput.Attributes.Add(expectedAttribute);

            var tagBuilder = new TagBuilder("p");

            // Act
            tagHelperOutput.MergeAttributes(tagBuilder);

            // Assert
            var attribute = Assert.Single(tagHelperOutput.Attributes);
            Assert.Equal(expectedAttribute, attribute);
        }

        [Fact]
        public void MergeAttributes_Combines_TagHelperOutputAttributeValues()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput(
                "p",
                attributes: new Dictionary<string, string>());
            var expectedOutputAttribute = new KeyValuePair<string, string>("class", "btn");
            tagHelperOutput.Attributes.Add(expectedOutputAttribute);

            var tagBuilder = new TagBuilder("p");
            var expectedBuilderAttribute = new KeyValuePair<string, string>("for", "hello");
            tagBuilder.Attributes.Add(expectedBuilderAttribute);

            // Act
            tagHelperOutput.MergeAttributes(tagBuilder);

            // Assert
            Assert.Equal(tagHelperOutput.Attributes.Count, 2);
            var attribute = Assert.Single(tagHelperOutput.Attributes, kvp => kvp.Key.Equals("class"));
            Assert.Equal(expectedOutputAttribute.Value, attribute.Value);
            attribute = Assert.Single(tagHelperOutput.Attributes, kvp => kvp.Key.Equals("for"));
            Assert.Equal(expectedBuilderAttribute.Value, attribute.Value);
        }
    }
}
