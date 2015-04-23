// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Testing;
using Microsoft.Framework.WebEncoders.Testing;
using Microsoft.Internal.Web.Utils;
using Xunit;

namespace Microsoft.AspNet.Mvc.TagHelpers
{
    public class TagHelperOutputExtensionsTest
    {
        public static TheoryData CopyHtmlAttributeData_MultipleAttributesSameName
        {
            get
            {
                // attributeNameToCopy, allAttributes, expectedAttributes
                return new TheoryData<string, TagHelperAttributeList, IEnumerable<TagHelperAttribute>>
                {
                    {
                        "hello",
                        new TagHelperAttributeList
                        {
                            { "hello", "world" },
                            { "hello", "world2" }
                        },
                        new[]
                        {
                            new TagHelperAttribute("hello", "world"),
                            new TagHelperAttribute("hello", "world2"),
                        }
                    },
                    {
                        "HELLO",
                        new TagHelperAttributeList
                        {
                            { "hello", "world" },
                            { "hello", "world2" }
                        },
                        new[]
                        {
                            new TagHelperAttribute("hello", "world"),
                            new TagHelperAttribute("hello", "world2"),
                        }
                    },
                    {
                        "hello",
                        new TagHelperAttributeList
                        {
                            { "HelLO", "world" },
                            { "HELLO", "world2" }
                        },
                        new[]
                        {
                            new TagHelperAttribute("HelLO", "world"),
                            new TagHelperAttribute("HELLO", "world2"),
                        }
                    },
                    {
                        "hello",
                        new TagHelperAttributeList
                        {
                            { "hello", "world" },
                            { "HELLO", "world2" }
                        },
                        new[]
                        {
                            new TagHelperAttribute("hello", "world"),
                            new TagHelperAttribute("HELLO", "world2"),
                        }
                    },
                    {
                        "HELLO",
                        new TagHelperAttributeList
                        {
                            { "HeLlO", "world" },
                            { "heLLo", "world2" }
                        },
                        new[]
                        {
                            new TagHelperAttribute("HeLlO", "world"),
                            new TagHelperAttribute("heLLo", "world2"),
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(CopyHtmlAttributeData_MultipleAttributesSameName))]
        public void CopyHtmlAttribute_CopiesAllOriginalAttributes(
            string attributeNameToCopy,
            TagHelperAttributeList allAttributes,
            IEnumerable<TagHelperAttribute> expectedAttributes)
        {
            // Arrange
            var output = new TagHelperOutput("p", attributes: new TagHelperAttributeList());
            var context = new TagHelperContext(
                allAttributes,
                items: new Dictionary<object, object>(),
                uniqueId: "test",
                getChildContentAsync: () => Task.FromResult<TagHelperContent>(result: null));

            // Act
            output.CopyHtmlAttribute(attributeNameToCopy, context);

            // Assert
            Assert.Equal(expectedAttributes, output.Attributes, CaseSensitiveTagHelperAttributeComparer.Default);
        }

        [Theory]
        [InlineData("hello", "world")]
        [InlineData("HeLlO", "wOrLd")]
        public void CopyHtmlAttribute_CopiesOriginalAttributes(string attributeName, string attributeValue)
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput(
                "p",
                attributes: new TagHelperAttributeList());
            var tagHelperContext = new TagHelperContext(
                allAttributes: new TagHelperAttributeList
                {
                    { attributeName, attributeValue }
                },
                items: new Dictionary<object, object>(),
                uniqueId: "test",
                getChildContentAsync: () =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.Append("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });
            var expectedAttribute = new TagHelperAttribute(attributeName, attributeValue);

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
                attributes: new TagHelperAttributeList()
                {
                    { attributeName, "world2" }
                });
            var expectedAttribute = new TagHelperAttribute(attributeName, "world2");
            var tagHelperContext = new TagHelperContext(
                allAttributes: new TagHelperAttributeList
                {
                    { attributeName, "world" }
                },
                items: new Dictionary<object, object>(),
                uniqueId: "test",
                getChildContentAsync: () =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.Append("Something Else");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });

            // Act
            tagHelperOutput.CopyHtmlAttribute(attributeName, tagHelperContext);

            // Assert
            var attribute = Assert.Single(tagHelperOutput.Attributes);
            Assert.Equal(expectedAttribute, attribute);
        }

        [Fact]
        public void CopyHtmlAttribute_ThrowsWhenUnknownAttribute()
        {
            // Arrange
            var invalidAttributeName = "hello2";
            var tagHelperOutput = new TagHelperOutput(
                "p",
                attributes: new TagHelperAttributeList());
            var tagHelperContext = new TagHelperContext(
                allAttributes: new TagHelperAttributeList
                {
                    { "hello", "world" }
                },
                items: new Dictionary<object, object>(),
                uniqueId: "test",
                getChildContentAsync: () =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.Append("Something");
                    return Task.FromResult<TagHelperContent>(tagHelperContent);
                });

            // Act & Assert
            ExceptionAssert.ThrowsArgument(
                () => tagHelperOutput.CopyHtmlAttribute(invalidAttributeName, tagHelperContext),
                "attributeName",
                "The attribute 'hello2' does not exist in the TagHelperContext.");
        }

        [Fact]
        public void RemoveRange_RemovesProvidedAttributes()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput(
                "p",
                attributes: new TagHelperAttributeList()
                {
                    { "route-Hello", "World" },
                    { "Route-I", "Am" }
                });
            var expectedAttribute = new TagHelperAttribute("type", "btn");
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
                attributes: new TagHelperAttributeList()
                {
                    { "routeHello", "World" },
                    { "Routee-I", "Am" }
                });

            // Act
            var attributes = tagHelperOutput.FindPrefixedAttributes("route-");

            // Assert
            Assert.Empty(attributes);
            var attribute = Assert.Single(tagHelperOutput.Attributes, attr => attr.Name.Equals("routeHello"));
            Assert.Equal(attribute.Value, "World");
            attribute = Assert.Single(tagHelperOutput.Attributes, attr => attr.Name.Equals("Routee-I"));
            Assert.Equal(attribute.Value, "Am");
        }

        public static TheoryData MultipleAttributeSameNameData
        {
            get
            {
                // tagBuilderAttributes, outputAttributes, expectedAttributes
                return new TheoryData<Dictionary<string, string>, TagHelperAttributeList, TagHelperAttributeList>
                {
                    {
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "class", "btn" }
                        },
                        new TagHelperAttributeList
                        {
                            { "class", "btn2" },
                            { "class", "btn3" }
                        },
                        new TagHelperAttributeList
                        {
                            { "class", "btn2 btn" }
                        }
                    },
                    {
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "ClAsS", "btn" }
                        },
                        new TagHelperAttributeList
                        {
                            { "class", "btn2" },
                            { "class", "btn3" }
                        },
                        new TagHelperAttributeList
                        {
                            { "class", "btn2 btn" }
                        }
                    },
                    {
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "class", "btn" }
                        },
                        new TagHelperAttributeList
                        {
                            { "clASS", "btn2" },
                            { "class", "btn3" }
                        },
                        new TagHelperAttributeList
                        {
                            { "class", "btn2 btn" }
                        }
                    },
                    {
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "class", "btn" }
                        },
                        new TagHelperAttributeList
                        {
                            { "clASS", "btn2" },
                            { "CLass", "btn3" }
                        },
                        new TagHelperAttributeList
                        {
                            { "class", "btn2 btn" }
                        }
                    },
                    {
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "CLASS", "btn" }
                        },
                        new TagHelperAttributeList
                        {
                            { "clASS", "btn2" },
                            { "CLass", "btn3" }
                        },
                        new TagHelperAttributeList
                        {
                            { "class", "btn2 btn" }
                        }
                    },
                    {
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "CLASS", "btn" }
                        },
                        new TagHelperAttributeList
                        {
                            { "before", "before value" },
                            { "clASS", "btn2" },
                            { "mid", "mid value" },
                            { "CLass", "btn3" },
                            { "after", "after value" },
                        },
                        new TagHelperAttributeList
                        {
                            { "before", "before value" },
                            { "class", "btn2 btn" },
                            { "mid", "mid value" },
                            { "after", "after value" },
                        }
                    },
                    {
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "A", "A Value" },
                            { "CLASS", "btn" },
                            { "B", "B Value" },
                        },
                        new TagHelperAttributeList
                        {
                            { "before", "before value" },
                            { "clASS", "btn2" },
                            { "mid", "mid value" },
                            { "CLass", "btn3" },
                            { "after", "after value" },
                        },
                        new TagHelperAttributeList
                        {
                            { "before", "before value" },
                            { "class", "btn2 btn" },
                            { "mid", "mid value" },
                            { "after", "after value" },
                            { "A", "A Value" },
                            { "B", "B Value" },
                        }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(MultipleAttributeSameNameData))]
        public void MergeAttributes_ClearsDuplicateClassNameAttributes(
            Dictionary<string, string> tagBuilderAttributes,
            TagHelperAttributeList outputAttributes,
            TagHelperAttributeList expectedAttributes)
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput("p", outputAttributes);

            var tagBuilder = new TagBuilder("p", new CommonTestEncoder());
            foreach (var attr in tagBuilderAttributes)
            {
                tagBuilder.Attributes.Add(attr.Key, attr.Value);
            }

            // Act
            tagHelperOutput.MergeAttributes(tagBuilder);

            // Assert
            Assert.Equal(
                expectedAttributes,
                tagHelperOutput.Attributes,
                CaseSensitiveTagHelperAttributeComparer.Default);
        }

        [Fact]
        public void MergeAttributes_DoesNotReplace_TagHelperOutputAttributeValues()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput(
                "p",
                attributes: new TagHelperAttributeList());
            var expectedAttribute = new TagHelperAttribute("type", "btn");
            tagHelperOutput.Attributes.Add(expectedAttribute);

            var tagBuilder = new TagBuilder("p", new CommonTestEncoder());
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
                attributes: new TagHelperAttributeList());
            tagHelperOutput.Attributes.Add("class", "Hello");

            var tagBuilder = new TagBuilder("p", new CommonTestEncoder());
            tagBuilder.Attributes.Add("class", "btn");

            var expectedAttribute = new TagHelperAttribute("class", "Hello btn");

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
                attributes: new TagHelperAttributeList());
            tagHelperOutput.Attributes.Add(originalName, "Hello");

            var tagBuilder = new TagBuilder("p", new CommonTestEncoder());
            tagBuilder.Attributes.Add(updateName, "btn");

            // Act
            tagHelperOutput.MergeAttributes(tagBuilder);

            // Assert
            var attribute = Assert.Single(tagHelperOutput.Attributes);
            Assert.Equal(new TagHelperAttribute(originalName, "Hello btn"), attribute);
        }

        [Fact]
        public void MergeAttributes_DoesNotEncode_TagHelperOutputAttributeValues()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput(
                "p",
                attributes: new TagHelperAttributeList());

            var tagBuilder = new TagBuilder("p", new CommonTestEncoder());
            var expectedAttribute = new TagHelperAttribute("visible", "val < 3");
            tagBuilder.Attributes.Add("visible", "val < 3");

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
                attributes: new TagHelperAttributeList());

            var tagBuilder = new TagBuilder("p", new CommonTestEncoder());
            var expectedAttribute1 = new TagHelperAttribute("class", "btn");
            var expectedAttribute2 = new TagHelperAttribute("class2", "btn");
            tagBuilder.Attributes.Add("class", "btn");
            tagBuilder.Attributes.Add("class2", "btn");

            // Act
            tagHelperOutput.MergeAttributes(tagBuilder);

            // Assert
            Assert.Equal(2, tagHelperOutput.Attributes.Count);
            var attribute = Assert.Single(tagHelperOutput.Attributes, attr => attr.Name.Equals("class"));
            Assert.Equal(expectedAttribute1.Value, attribute.Value);
            attribute = Assert.Single(tagHelperOutput.Attributes, attr => attr.Name.Equals("class2"));
            Assert.Equal(expectedAttribute2.Value, attribute.Value);
        }

        [Fact]
        public void MergeAttributes_Maintains_TagHelperOutputAttributeValues()
        {
            // Arrange
            var tagHelperOutput = new TagHelperOutput(
                "p",
                attributes: new TagHelperAttributeList());
            var expectedAttribute = new TagHelperAttribute("class", "btn");
            tagHelperOutput.Attributes.Add(expectedAttribute);

            var tagBuilder = new TagBuilder("p", new CommonTestEncoder());

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
                attributes: new TagHelperAttributeList());
            var expectedOutputAttribute = new TagHelperAttribute("class", "btn");
            tagHelperOutput.Attributes.Add(expectedOutputAttribute);

            var tagBuilder = new TagBuilder("p", new CommonTestEncoder());
            var expectedBuilderAttribute = new TagHelperAttribute("for", "hello");
            tagBuilder.Attributes.Add("for", "hello");

            // Act
            tagHelperOutput.MergeAttributes(tagBuilder);

            // Assert
            Assert.Equal(tagHelperOutput.Attributes.Count, 2);
            var attribute = Assert.Single(tagHelperOutput.Attributes, attr => attr.Name.Equals("class"));
            Assert.Equal(expectedOutputAttribute.Value, attribute.Value);
            attribute = Assert.Single(tagHelperOutput.Attributes, attr => attr.Name.Equals("for"));
            Assert.Equal(expectedBuilderAttribute.Value, attribute.Value);
        }

        private class CaseSensitiveTagHelperAttributeComparer : IEqualityComparer<IReadOnlyTagHelperAttribute>
        {
            public readonly static CaseSensitiveTagHelperAttributeComparer Default =
                new CaseSensitiveTagHelperAttributeComparer();

            private CaseSensitiveTagHelperAttributeComparer()
            {
            }

            public bool Equals(
                IReadOnlyTagHelperAttribute attributeX,
                IReadOnlyTagHelperAttribute attributeY)
            {
                return
                    attributeX == attributeY ||
                    // Normal comparer doesn't care about the Name case, in tests we do.
                    string.Equals(attributeX.Name, attributeY.Name, StringComparison.Ordinal) &&
                    Equals(attributeX.Value, attributeY.Value);
            }

            public int GetHashCode(IReadOnlyTagHelperAttribute attribute)
            {
                return HashCodeCombiner
                    .Start()
                    .Add(attribute.Name, StringComparer.Ordinal)
                    .Add(attribute.Value)
                    .CombinedHash;
            }
        }
    }
}
