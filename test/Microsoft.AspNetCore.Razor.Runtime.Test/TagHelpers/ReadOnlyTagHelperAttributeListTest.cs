// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.TagHelpers
{
    public class ReadOnlyTagHelperAttributeListTest
    {
        public static TheoryData IndexOfNameData
        {
            get
            {
                var first = new TagHelperAttribute("First", "First Value");
                var second = new TagHelperAttribute("Second", "Second Value");
                var third = new TagHelperAttribute("Third", "Third Value");

                return new TheoryData<
                    IEnumerable<TagHelperAttribute>, // initialAttributes
                    string, // nameToLookup
                    int> // expectedIndex
                {
                    { new[] { first }, first.Name, 0 },
                    { new[] { first, second }, first.Name, 0 },
                    { new[] { first, second }, second.Name.ToUpper(), 1 },
                    { new[] { first, second, third}, second.Name, 1 },
                    { new[] { first, second, third }, third.Name.ToLower(), 2 },
                    { new[] { first, first, second, third}, first.Name, 0 },

                    // Bad lookups
                    { new[] { first, second, third}, "bad", -1 },
                    { new[] { first, first, first}, first.Name + "bad", -1 },
                };
            }
        }

        [Theory]
        [MemberData(nameof(IndexOfNameData))]
        public void IndexOfName_ReturnsExpectedValue(
            IEnumerable<TagHelperAttribute> initialAttributes,
            string nameToLookup,
            int expectedIndex)
        {
            // Arrange
            var attributes = new TagHelperAttributeList(initialAttributes);

            // Act
            var index = attributes.IndexOfName(nameToLookup);

            // Assert
            Assert.Equal(expectedIndex, index);
        }

        public static TheoryData IntIndexerData
        {
            get
            {
                var first = new TagHelperAttribute("First", "First Value");
                var second = new TagHelperAttribute("Second", "Second Value");
                var third = new TagHelperAttribute("Third", "Third Value");

                return new TheoryData<
                    IEnumerable<TagHelperAttribute>, // initialAttributes
                    int, // indexToLookup
                    TagHelperAttribute> // expectedAttribute
                {
                    { new[] { first }, 0, first },
                    { new[] { first, second }, 0, first },
                    { new[] { first, second }, 1, second },
                    { new[] { first, second, third}, 1, second },
                    { new[] { first, second, third }, 2, third },
                    { new[] { first, first, second, third}, 1, first },
                };
            }
        }

        [Theory]
        [MemberData(nameof(IntIndexerData))]
        public void IntIndexer_ReturnsExpectedAttribute(
            IEnumerable<TagHelperAttribute> initialAttributes,
            int indexToLookup,
            TagHelperAttribute expectedAttribute)
        {
            // Arrange
            var attributes = new TagHelperAttributeList(initialAttributes);

            // Act
            var attribute = attributes[indexToLookup];

            // Assert
            Assert.Equal(expectedAttribute, attribute, CaseSensitiveTagHelperAttributeComparer.Default);
        }

        public static TheoryData IntIndexerThrowData
        {
            get
            {
                return new TheoryData<int> { 2, -1, 20 };
            }
        }

        [Theory]
        [MemberData(nameof(IntIndexerThrowData))]
        public void IntIndexer_ThrowsIfInvalidIndex(int index)
        {
            // Arrange
            var attributes = new TagHelperAttributeList(
                new[]
                {
                    new TagHelperAttribute("a", "av"),
                    new TagHelperAttribute("b", "bv")
                });

            // Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>("index", () => attributes[index]);
        }

        public static TheoryData StringIndexerData
        {
            get
            {
                var A = new TagHelperAttribute("AName", "AName Value");
                var A2 = new TagHelperAttribute("aname", "AName Second Value");
                var A3 = new TagHelperAttribute("AName", "AName Third Value");
                var B = new TagHelperAttribute("BName", "BName Value");
                var C = new TagHelperAttribute("CName", "CName Value");

                return new TheoryData<
                    IEnumerable<TagHelperAttribute>, // initialAttributes
                    string, // nameToLookup
                    TagHelperAttribute> // expectedAttribute
                {
                    { new[] { A }, "AName", A },
                    { new[] { A }, "AnAmE", A },
                    { new[] { A, B }, "AName", A },
                    { new[] { A, B }, "AnAmE", A },
                    { new[] { A, B }, "BName", B },
                    { new[] { A, B }, "BnAmE", B },
                    { new[] { A, B, C }, "BName", B },
                    { new[] { A, B, C }, "bname", B },
                    { new[] { A, B, C }, "CName", C },
                    { new[] { A, B, C }, "cnamE", C },

                    // Multiple elements same name
                    { new[] { A, B, A2, C }, "AName", A },
                    { new[] { A, B, A2, C }, "aname", A },
                    { new[] { B, A2, A }, "aname", A2 },
                    { new[] { B, A2, A, C }, "AName", A2 },
                    { new[] { A, A3 }, "AName", A },
                    { new[] { A3, A }, "aname", A3 },
                    { new[] { A, A2, A3 }, "AName", A },
                    { new[] { C, B, A3, A }, "AName", A3 },

                    // Null expected lookups
                    { new[] { A }, "_AName_", null },
                    { new[] { A }, "completely different", null },
                    { new[] { A, B }, "_AName_", null },
                    { new[] { A, B }, "completely different", null },
                    { new[] { A, B, C }, "_BName_", null },
                    { new[] { A, B, C }, "completely different", null },
                    { new[] { A, A2, B, C }, "_cnamE_", null },
                    { new[] { A, A2, B, C }, "completely different", null },
                    { new[] { A, A2, A3, B, C }, "_cnamE_", null },
                    { new[] { A, A2, A3, B, C }, "completely different", null },
                };
            }
        }

        [Theory]
        [MemberData(nameof(StringIndexerData))]
        public void StringIndexer_ReturnsExpectedAttribute(
            IEnumerable<TagHelperAttribute> initialAttributes,
            string nameToLookup,
            TagHelperAttribute expectedAttribute)
        {
            // Arrange
            var attributes = new TagHelperAttributeList(initialAttributes);

            // Act
            var attribute = attributes[nameToLookup];

            // Assert
            Assert.Equal(expectedAttribute, attribute, CaseSensitiveTagHelperAttributeComparer.Default);
        }

        [Fact]
        public void Count_ReturnsNumberOfAttributes()
        {
            // Arrange
            var attributes = new TagHelperAttributeList(
                new[]
                {
                    new TagHelperAttribute("A"),
                    new TagHelperAttribute("B"),
                    new TagHelperAttribute("C")
                });

            // Act
            var count = attributes.Count;

            // Assert
            Assert.Equal(3, count);
        }

        public static TheoryData ContainsData
        {
            get
            {
                var A = new TagHelperAttribute("AName", "AName Value");
                var A2 = new TagHelperAttribute("aname", "AName Second Value");
                var A3 = new TagHelperAttribute("AName", "AName Third Value");
                var B = new TagHelperAttribute("BName", "BName Value");
                var C = new TagHelperAttribute("CName", "CName Value");

                return new TheoryData<
                    IEnumerable<TagHelperAttribute>, // initialAttributes
                    TagHelperAttribute, // attributeToLookup
                    bool> // expected
                {
                    { new[] { A }, A, true },
                    { new[] { A }, new TagHelperAttribute(A.Name, A.Value), true },
                    { new[] { A }, new TagHelperAttribute("aname", A.Value), true },
                    { new[] { A, B }, A, true },
                    { new[] { A, B }, new TagHelperAttribute(A.Name, A.Value), true },
                    { new[] { A, B }, new TagHelperAttribute("AnaMe", A.Value), true },
                    { new[] { A, B }, B, true },
                    { new[] { A, B }, new TagHelperAttribute(B.Name, B.Value), true },
                    { new[] { A, B }, new TagHelperAttribute("BNAME", B.Value), true },
                    { new[] { A, B, C }, B, true },
                    { new[] { A, B, C }, new TagHelperAttribute(B.Name, B.Value), true },
                    { new[] { A, B, C }, new TagHelperAttribute("bname", B.Value), true },
                    { new[] { A, B, C }, C, true },
                    { new[] { A, B, C }, new TagHelperAttribute(C.Name, C.Value), true },
                    { new[] { A, B, C }, new TagHelperAttribute("CNAme", C.Value), true },
                    { new[] { A }, B, false },
                    { new[] { A }, new TagHelperAttribute(A.Name, "different value"), false },
                    { new[] { A }, new TagHelperAttribute("aname_not", "different value"), false },
                    { new[] { A, B }, A2, false },
                    { new[] { A, B }, new TagHelperAttribute(A.Name, "different value"), false },
                    { new[] { A, B }, new TagHelperAttribute("AnaMe_not", "different value"), false },
                    { new[] { A, B }, new TagHelperAttribute(B.Name, "different value"), false },
                    { new[] { A, B }, new TagHelperAttribute("BNAME_not", "different value"), false },
                    { new[] { A, B, C }, A2, false },
                    { new[] { A, B, C }, new TagHelperAttribute(B.Name, "different value"), false },
                    { new[] { A, B, C }, new TagHelperAttribute("bname_not", "different value"), false },
                    { new[] { A, B, C }, new TagHelperAttribute(C.Name, "different value"), false },
                    { new[] { A, B, C }, new TagHelperAttribute("CNAme_not", "different value"), false },

                    // Multiple elements same name
                    { new[] { A, B, A2, C }, A, true },
                    { new[] { A, B, A2, C }, new TagHelperAttribute(A.Name, A.Value), true },
                    { new[] { A, B, A2, C }, new TagHelperAttribute("aname", A.Value), true },
                    { new[] { B, A2, A }, A2, true },
                    { new[] { B, A2, A }, new TagHelperAttribute(A.Name, A.Value), true },
                    { new[] { B, A2, A }, new TagHelperAttribute("AnAME", A2.Value), true },
                    { new[] { B, A2, A, C }, A, true },
                    { new[] { B, A2, A, C }, new TagHelperAttribute(A.Name, A.Value), true },
                    { new[] { B, A2, A, C }, new TagHelperAttribute("ANAME", A.Value), true },
                    { new[] { A, A3 }, A, true },
                    { new[] { A, A3 }, new TagHelperAttribute(A.Name, A.Value), true },
                    { new[] { A, A3 }, new TagHelperAttribute("ANamE", A.Value), true },
                    { new[] { A3, A }, A3, true },
                    { new[] { A3, A }, new TagHelperAttribute(A3.Name, A3.Value), true },
                    { new[] { A3, A }, new TagHelperAttribute("anamE", A3.Value), true },
                    { new[] { A, A2, A3 }, A, true },
                    { new[] { A, A2, A3 }, new TagHelperAttribute(A.Name, A.Value), true },
                    { new[] { A, A2, A3 }, new TagHelperAttribute("ANAme", A.Value), true },
                    { new[] { C, B, A3, A }, A3, true },
                    { new[] { C, B, A3, A }, new TagHelperAttribute(A3.Name, A3.Value), true },
                    { new[] { C, B, A3, A }, new TagHelperAttribute("aname", A3.Value), true },
                    { new[] { A, B, A2, C }, A3, false },
                    { new[] { A, B, A2, C }, new TagHelperAttribute(A.Name, A3.Value), false },
                    { new[] { A, B, A2, C }, new TagHelperAttribute("aname_not", "different value"), false },
                    { new[] { B, A2, A }, A3, false },
                    { new[] { B, A2, A }, new TagHelperAttribute(A.Name, A3.Value), false },
                    { new[] { B, A2, A }, new TagHelperAttribute("AnAME_not", "different value"), false },
                    { new[] { B, A2, A, C }, A3, false },
                    { new[] { B, A2, A, C }, new TagHelperAttribute(A.Name, A3.Value), false },
                    { new[] { B, A2, A, C }, new TagHelperAttribute("ANAME_not", "different value"), false },
                    { new[] { A, A3 }, B, false },
                    { new[] { A, A3 }, new TagHelperAttribute(A.Name, A2.Value), false },
                    { new[] { A, A3 }, new TagHelperAttribute("ANamE_not", "different value"), false },
                    { new[] { A3, A }, B, false },
                    { new[] { A3, A }, new TagHelperAttribute(A3.Name, A2.Value), false },
                    { new[] { A3, A }, new TagHelperAttribute("anamE_not", "different value"), false },
                    { new[] { A, A2, A3 }, B, false },
                    { new[] { A, A2, A3 }, new TagHelperAttribute(A.Name, B.Value), false },
                    { new[] { A, A2, A3 }, new TagHelperAttribute("ANAme_not", "different value"), false },
                    { new[] { C, B, A3, A }, A2, false },
                    { new[] { C, B, A3, A }, new TagHelperAttribute(A3.Name, A2.Value), false },
                    { new[] { C, B, A3, A }, new TagHelperAttribute("aname_not", "different value"), false },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ContainsData))]
        public void Contains_ReturnsExpectedResult(
            IEnumerable<TagHelperAttribute> initialAttributes,
            TagHelperAttribute attributeToLookup,
            bool expected)
        {
            // Arrange
            var attributes = new TagHelperAttributeList(initialAttributes);

            // Act
            var contains = attributes.Contains(attributeToLookup);

            // Assert
            Assert.Equal(expected, contains);
        }

        public static TheoryData ContainsNameData
        {
            get
            {
                var A = new TagHelperAttribute("AName", "AName Value");
                var A2 = new TagHelperAttribute("aname", "AName Second Value");
                var A3 = new TagHelperAttribute("AName", "AName Third Value");
                var B = new TagHelperAttribute("BName", "BName Value");
                var C = new TagHelperAttribute("CName", "CName Value");

                return new TheoryData<
                    IEnumerable<TagHelperAttribute>, // initialAttributes
                    string, // nameToLookup
                    bool> // expected
                {
                    { new[] { A }, A.Name, true },
                    { new[] { A }, "aname", true },
                    { new[] { A, B }, A.Name, true },
                    { new[] { A, B }, "AnaMe", true },
                    { new[] { A, B }, B.Name, true },
                    { new[] { A, B }, "BNAME", true },
                    { new[] { A, B, C }, B.Name, true },
                    { new[] { A, B, C }, "bname", true },
                    { new[] { A, B, C }, C.Name, true },
                    { new[] { A, B, C }, "CNAme", true },
                    { new[] { A }, B.Name, false },
                    { new[] { A, B }, C.Name, false },
                    { new[] { A, B, C }, "different", false },

                    // Multiple elements same name
                    { new[] { A, B, A2, C }, A.Name, true },
                    { new[] { A, B, A2, C }, "aname", true },
                    { new[] { B, A2, A }, A2.Name, true },
                    { new[] { B, A2, A }, "AnAME", true },
                    { new[] { B, A2, A, C }, A.Name, true },
                    { new[] { B, A2, A, C }, "ANAME", true },
                    { new[] { A, A3 }, A.Name, true },
                    { new[] { A, A3 }, "ANamE", true },
                    { new[] { A, A2, A3 }, A.Name, true },
                    { new[] { A, A2, A3 }, "ANAme", true },
                    { new[] { C, B, A3, A }, A3.Name, true },
                    { new[] { C, B, A3, A }, "aname", true },
                    { new[] { A, B, A2, C }, "aname_not", false },
                    { new[] { B, A2, A }, C.Name, false },
                    { new[] { B, A2, A }, "AnAME_not", false },
                    { new[] { B, A2, A, C }, "different", false },
                    { new[] { B, A2, A, C }, "ANAME_not", false },
                    { new[] { A, A3 }, B.Name, false },
                    { new[] { A, A3 }, "ANamE_not", false },
                    { new[] { A3, A }, B.Name, false },
                    { new[] { A3, A }, "anamE_not", false },
                    { new[] { A, A2, A3 }, B.Name, false },
                    { new[] { A, A2, A3 }, "ANAme_not", false },
                    { new[] { C, B, A3, A }, "different", false },
                    { new[] { C, B, A3, A }, "aname_not", false },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ContainsNameData))]
        public void ContainsName_ReturnsExpectedResult(
            IEnumerable<TagHelperAttribute> initialAttributes,
            string nameToLookup,
            bool expected)
        {
            // Arrange
            var attributes = new TagHelperAttributeList(initialAttributes);

            // Act
            var contains = attributes.ContainsName(nameToLookup);

            // Assert
            Assert.Equal(expected, contains);
        }

        public static TheoryData IndexOfData
        {
            get
            {
                var A = new TagHelperAttribute("AName", "AName Value");
                var A2 = new TagHelperAttribute("aname", "AName Second Value");
                var A3 = new TagHelperAttribute("AName", "AName Third Value");
                var B = new TagHelperAttribute("BName", "BName Value");
                var C = new TagHelperAttribute("CName", "CName Value");

                return new TheoryData<
                    IEnumerable<TagHelperAttribute>, // initialAttributes
                    TagHelperAttribute, // attributeToLookup
                    int> // expected
                {
                    { new[] { A }, A, 0 },
                    { new[] { A }, new TagHelperAttribute(A.Name, A.Value), 0 },
                    { new[] { A }, new TagHelperAttribute("aname", A.Value), 0 },
                    { new[] { A, B }, A, 0 },
                    { new[] { A, B }, new TagHelperAttribute(A.Name, A.Value), 0 },
                    { new[] { A, B }, new TagHelperAttribute("AnaMe", A.Value), 0 },
                    { new[] { A, B }, B, 1 },
                    { new[] { A, B }, new TagHelperAttribute(B.Name, B.Value), 1 },
                    { new[] { A, B }, new TagHelperAttribute("BNAME", B.Value), 1 },
                    { new[] { A, B, C }, B, 1 },
                    { new[] { A, B, C }, new TagHelperAttribute(B.Name, B.Value), 1 },
                    { new[] { A, B, C }, new TagHelperAttribute("bname", B.Value), 1 },
                    { new[] { A, B, C }, C, 2 },
                    { new[] { A, B, C }, new TagHelperAttribute(C.Name, C.Value), 2 },
                    { new[] { A, B, C }, new TagHelperAttribute("CNAme", C.Value), 2 },
                    { new[] { A }, B, -1 },
                    { new[] { A }, new TagHelperAttribute(A.Name, "different value"), -1 },
                    { new[] { A }, new TagHelperAttribute("aname_not", "different value"), -1 },
                    { new[] { A, B }, A2, -1 },
                    { new[] { A, B }, new TagHelperAttribute(A.Name, "different value"), -1 },
                    { new[] { A, B }, new TagHelperAttribute("AnaMe_not", "different value"), -1 },
                    { new[] { A, B }, new TagHelperAttribute(B.Name, "different value"), -1 },
                    { new[] { A, B }, new TagHelperAttribute("BNAME_not", "different value"), -1 },
                    { new[] { A, B, C }, A2, -1 },
                    { new[] { A, B, C }, new TagHelperAttribute(B.Name, "different value"), -1 },
                    { new[] { A, B, C }, new TagHelperAttribute("bname_not", "different value"), -1 },
                    { new[] { A, B, C }, new TagHelperAttribute(C.Name, "different value"), -1 },
                    { new[] { A, B, C }, new TagHelperAttribute("CNAme_not", "different value"), -1 },

                    // Multiple elements same name
                    { new[] { A, B, A2, C }, A, 0 },
                    { new[] { A, B, A2, C }, new TagHelperAttribute(A.Name, A.Value), 0 },
                    { new[] { A, B, A2, C }, new TagHelperAttribute("aname", A.Value), 0 },
                    { new[] { B, A2, A }, A2, 1 },
                    { new[] { B, A2, A }, new TagHelperAttribute(A.Name, A.Value), 2 },
                    { new[] { B, A2, A }, new TagHelperAttribute("AnAME", A2.Value), 1 },
                    { new[] { B, A2, A, C }, A, 2 },
                    { new[] { B, A2, A, C }, new TagHelperAttribute(A.Name, A.Value), 2 },
                    { new[] { B, A2, A, C }, new TagHelperAttribute("ANAME", A.Value), 2 },
                    { new[] { A, A3 }, A, 0 },
                    { new[] { A, A3 }, new TagHelperAttribute(A.Name, A.Value), 0 },
                    { new[] { A, A3 }, new TagHelperAttribute("ANamE", A.Value), 0 },
                    { new[] { A3, A }, A3, 0 },
                    { new[] { A3, A }, new TagHelperAttribute(A3.Name, A3.Value), 0 },
                    { new[] { A3, A }, new TagHelperAttribute("anamE", A3.Value), 0 },
                    { new[] { A, A2, A3 }, A, 0 },
                    { new[] { A, A2, A3 }, new TagHelperAttribute(A.Name, A.Value), 0 },
                    { new[] { A, A2, A3 }, new TagHelperAttribute("ANAme", A.Value), 0 },
                    { new[] { C, B, A3, A }, A3, 2 },
                    { new[] { C, B, A3, A }, new TagHelperAttribute(A3.Name, A3.Value), 2 },
                    { new[] { C, B, A3, A }, new TagHelperAttribute("aname", A3.Value), 2 },
                    { new[] { A, B, A2, C }, A3, -1 },
                    { new[] { A, B, A2, C }, new TagHelperAttribute(A.Name, A3.Value), -1 },
                    { new[] { A, B, A2, C }, new TagHelperAttribute("aname_not", "different value"), -1 },
                    { new[] { B, A2, A }, A3, -1 },
                    { new[] { B, A2, A }, new TagHelperAttribute(A.Name, A3.Value), -1 },
                    { new[] { B, A2, A }, new TagHelperAttribute("AnAME_not", "different value"), -1 },
                    { new[] { B, A2, A, C }, A3, -1 },
                    { new[] { B, A2, A, C }, new TagHelperAttribute(A.Name, A3.Value), -1 },
                    { new[] { B, A2, A, C }, new TagHelperAttribute("ANAME_not", "different value"), -1 },
                    { new[] { A, A3 }, B, -1 },
                    { new[] { A, A3 }, new TagHelperAttribute(A.Name, A2.Value), -1 },
                    { new[] { A, A3 }, new TagHelperAttribute("ANamE_not", "different value"), -1 },
                    { new[] { A3, A }, B, -1 },
                    { new[] { A3, A }, new TagHelperAttribute(A3.Name, A2.Value), -1 },
                    { new[] { A3, A }, new TagHelperAttribute("anamE_not", "different value"), -1 },
                    { new[] { A, A2, A3 }, B, -1 },
                    { new[] { A, A2, A3 }, new TagHelperAttribute(A.Name, B.Value), -1 },
                    { new[] { A, A2, A3 }, new TagHelperAttribute("ANAme_not", "different value"), -1 },
                    { new[] { C, B, A3, A }, A2, -1 },
                    { new[] { C, B, A3, A }, new TagHelperAttribute(A3.Name, A2.Value), -1 },
                    { new[] { C, B, A3, A }, new TagHelperAttribute("aname_not", "different value"), -1 },
                };
            }
        }

        [Theory]
        [MemberData(nameof(IndexOfData))]
        public void IndexOf_ReturnsExpectedResult(
            IEnumerable<TagHelperAttribute> initialAttributes,
            TagHelperAttribute attributeToLookup,
            int expected)
        {
            // Arrange
            var attributes = new TagHelperAttributeList(initialAttributes);

            // Act
            var index = attributes.IndexOf(attributeToLookup);

            // Assert
            Assert.Equal(expected, index);
        }

        public static TheoryData TryGetAttributeData
        {
            get
            {
                var A = new TagHelperAttribute("AName", "AName Value");
                var A2 = new TagHelperAttribute("aname", "AName Second Value");
                var A3 = new TagHelperAttribute("AName", "AName Third Value");
                var B = new TagHelperAttribute("BName", "BName Value");
                var C = new TagHelperAttribute("CName", "CName Value");

                return new TheoryData<
                    IEnumerable<TagHelperAttribute>, // initialAttributes
                    string, // nameToLookup
                    TagHelperAttribute, // expectedAttribute
                    bool> // expectedResult
                {
                    { new[] { A }, "AName", A, true },
                    { new[] { A }, "AnAmE", A, true },
                    { new[] { A, B }, "AName", A, true },
                    { new[] { A, B }, "AnAmE", A, true },
                    { new[] { A, B }, "BName", B, true },
                    { new[] { A, B }, "BnAmE", B, true },
                    { new[] { A, B, C }, "BName", B, true },
                    { new[] { A, B, C }, "bname", B, true },
                    { new[] { A, B, C }, "CName", C, true },
                    { new[] { A, B, C }, "cnamE", C, true },

                    // Multiple elements same name
                    { new[] { A, B, A2, C }, "AName", A, true },
                    { new[] { A, B, A2, C }, "aname", A, true },
                    { new[] { B, A2, A }, "aname", A2, true },
                    { new[] { B, A2, A, C }, "AName", A2, true },
                    { new[] { A, A3 }, "AName", A, true },
                    { new[] { A3, A }, "aname", A3, true },
                    { new[] { A, A2, A3 }, "AName", A, true },
                    { new[] { C, B, A3, A }, "AName", A3, true },

                    // Null expected lookups
                    { new[] { A }, "_AName_", null, false },
                    { new[] { A }, "completely different", null, false },
                    { new[] { A, B }, "_AName_", null, false },
                    { new[] { A, B }, "completely different", null, false },
                    { new[] { A, B, C }, "_BName_", null, false },
                    { new[] { A, B, C }, "completely different", null, false },
                    { new[] { A, A2, B, C }, "_cnamE_", null, false },
                    { new[] { A, A2, B, C }, "completely different", null, false },
                    { new[] { A, A2, A3, B, C }, "_cnamE_", null, false },
                    { new[] { A, A2, A3, B, C }, "completely different", null, false },
                };
            }
        }

        [Theory]
        [MemberData(nameof(TryGetAttributeData))]
        public void TryGetAttribute_ReturnsExpectedValueAndAttribute(
            IEnumerable<TagHelperAttribute> initialAttributes,
            string nameToLookup,
            TagHelperAttribute expectedAttribute,
            bool expectedResult)
        {
            // Arrange
            var attributes = new TagHelperAttributeList(initialAttributes);
            TagHelperAttribute attribute;

            // Act
            var result = attributes.TryGetAttribute(nameToLookup, out attribute);

            // Assert
            Assert.Equal(expectedResult, result);
            Assert.Equal(expectedAttribute, attribute, CaseSensitiveTagHelperAttributeComparer.Default);
        }

        public static TheoryData TryGetAttributesData
        {
            get
            {
                var A = new TagHelperAttribute("AName", "AName Value");
                var A2 = new TagHelperAttribute("aname", "AName Second Value");
                var A3 = new TagHelperAttribute("AName", "AName Third Value");
                var B = new TagHelperAttribute("BName", "BName Value");
                var C = new TagHelperAttribute("CName", "CName Value");

                return new TheoryData<
                    IEnumerable<TagHelperAttribute>, // initialAttributes
                    string, // nameToLookup
                    IEnumerable<TagHelperAttribute>, // expectedAttributes
                    bool> // expectedResult
                {
                    { new[] { A }, "AName", new[] { A }, true },
                    { new[] { A }, "AnAmE", new[] { A }, true },
                    { new[] { A, B }, "AName", new[] { A }, true },
                    { new[] { A, B }, "AnAmE", new[] { A }, true },
                    { new[] { A, B }, "BName", new[] { B }, true },
                    { new[] { A, B }, "BnAmE", new[] { B }, true },
                    { new[] { A, B, C }, "BName", new[] { B }, true },
                    { new[] { A, B, C }, "bname", new[] { B }, true },
                    { new[] { A, B, C }, "CName", new[] { C }, true },
                    { new[] { A, B, C }, "cnamE", new[] { C }, true },

                    // Multiple elements same name
                    { new[] { A, B, A2, C }, "AName", new[] { A, A2 }, true },
                    { new[] { A, B, A2, C }, "aname", new[] { A, A2 }, true },
                    { new[] { B, A2, A }, "aname", new[] { A2, A }, true },
                    { new[] { B, A2, A, C }, "AName", new[] { A2, A }, true },
                    { new[] { A, A3 }, "AName", new[] { A, A3 }, true },
                    { new[] { A3, A }, "aname", new[] { A3, A }, true },
                    { new[] { A, A2, A3 }, "AName", new[] { A, A2, A3 }, true },
                    { new[] { C, B, A3, A }, "AName", new[] { A3, A }, true },

                    // Null expected lookups
                    { new[] { A }, "_AName_", Enumerable.Empty<TagHelperAttribute>(), false },
                    { new[] { A }, "completely different", Enumerable.Empty<TagHelperAttribute>(), false },
                    { new[] { A, B }, "_AName_", Enumerable.Empty<TagHelperAttribute>(), false },
                    { new[] { A, B }, "completely different", Enumerable.Empty<TagHelperAttribute>(), false },
                    { new[] { A, B, C }, "_BName_", Enumerable.Empty<TagHelperAttribute>(), false },
                    { new[] { A, B, C }, "way different", Enumerable.Empty<TagHelperAttribute>(), false },
                    { new[] { A, A2, B, C }, "_cnamE_", Enumerable.Empty<TagHelperAttribute>(), false },
                    { new[] { A, A2, B, C }, "way different", Enumerable.Empty<TagHelperAttribute>(), false },
                    { new[] { A, A2, A3, B, C }, "_cnamE_", Enumerable.Empty<TagHelperAttribute>(), false },
                    { new[] { A, A2, A3, B, C }, "different", Enumerable.Empty<TagHelperAttribute>(), false },
                };
            }
        }

        [Theory]
        [MemberData(nameof(TryGetAttributesData))]
        public void TryGetAttributes_ReturnsExpectedValueAndAttribute(
            IEnumerable<TagHelperAttribute> initialAttributes,
            string nameToLookup,
            IEnumerable<TagHelperAttribute> expectedAttributes,
            bool expectedResult)
        {
            // Arrange
            var attributes = new TagHelperAttributeList(initialAttributes);
            IReadOnlyList<TagHelperAttribute> resolvedAttributes;

            // Act
            var result = attributes.TryGetAttributes(nameToLookup, out resolvedAttributes);

            // Assert
            Assert.Equal(expectedResult, result);
            Assert.Equal(expectedAttributes, resolvedAttributes, CaseSensitiveTagHelperAttributeComparer.Default);
        }

        [Fact]
        public void Attributes_EqualsInitialAttributes()
        {
            // Arrange
            var expectedAttributes = new[]
            {
                new TagHelperAttribute("A", "AV"),
                new TagHelperAttribute("B", "BV")
            };

            // Act
            var attributes = new TestableReadOnlyTagHelperAttributes(expectedAttributes);

            // Assert
            Assert.Equal(expectedAttributes, attributes.PublicAttributes);
        }

        [Fact]
        public void GetEnumerator_ReturnsUnderlyingAttributesEnumerator()
        {
            // Arrange & Act
            var attributes = new TestableReadOnlyTagHelperAttributes(new[]
            {
                new TagHelperAttribute("A", "AV"),
                new TagHelperAttribute("B", "BV")
            });

            // Assert
            Assert.Equal(attributes.GetEnumerator(), attributes.PublicAttributes.GetEnumerator());
        }

        [Fact]
        public void ModifyingUnderlyingAttributes_AffectsExposedAttributes()
        {
            // Arrange
            var attributes = new TestableReadOnlyTagHelperAttributes(Enumerable.Empty<TagHelperAttribute>());
            var expectedAttributes = new[]
            {
                new TagHelperAttribute("A", "AV"),
                new TagHelperAttribute("B", "BV")
            };

            // Act
            attributes.PublicAttributes.AddRange(expectedAttributes);

            // Assert
            Assert.Equal(attributes, expectedAttributes, CaseSensitiveTagHelperAttributeComparer.Default);
        }


        [Theory]
        [MemberData(nameof(IntIndexerData))]
        public void ModifyingUnderlyingAttributes_IntIndexer_ReturnsExpectedResult(
            IEnumerable<TagHelperAttribute> initialAttributes,
            int indexToLookup,
            TagHelperAttribute expectedAttribute)
        {
            // Arrange
            var attributes = new TestableReadOnlyTagHelperAttributes(Enumerable.Empty<TagHelperAttribute>());
            attributes.PublicAttributes.AddRange(initialAttributes);

            // Act
            var attribute = attributes[indexToLookup];

            // Assert
            Assert.Equal(expectedAttribute, attribute, CaseSensitiveTagHelperAttributeComparer.Default);
        }

        [Theory]
        [MemberData(nameof(StringIndexerData))]
        public void ModifyingUnderlyingAttributes_StringIndexer_ReturnsExpectedResult(
            IEnumerable<TagHelperAttribute> initialAttributes,
            string nameToLookup,
            TagHelperAttribute expectedAttribute)
        {
            // Arrange
            var attributes = new TestableReadOnlyTagHelperAttributes(Enumerable.Empty<TagHelperAttribute>());
            attributes.PublicAttributes.AddRange(initialAttributes);

            // Act
            var attribute = attributes[nameToLookup];

            // Assert
            Assert.Equal(expectedAttribute, attribute, CaseSensitiveTagHelperAttributeComparer.Default);
        }

        private class TestableReadOnlyTagHelperAttributes : ReadOnlyTagHelperAttributeList
        {
            public TestableReadOnlyTagHelperAttributes(IEnumerable<TagHelperAttribute> attributes)
                : base(new List<TagHelperAttribute>(attributes))
            {
            }

            public List<TagHelperAttribute> PublicAttributes
            {
                get
                {
                    return (List<TagHelperAttribute>)Items;
                }
            }
        }
    }
}