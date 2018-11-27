// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.TagHelpers.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Razor.TagHelpers
{
    public class TagHelperAttributeListTest
    {
        [Theory]
        [MemberData(
            nameof(ReadOnlyTagHelperAttributeListTest.IntIndexerData),
            MemberType = typeof(ReadOnlyTagHelperAttributeListTest))]
        public void IntIndexer_GetsExpectedAttribute(
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

        public static TheoryData IntIndexerSetData
        {
            get
            {
                var first = new TagHelperAttribute("First", "First Value");
                var second = new TagHelperAttribute("Second", "Second Value");
                var third = new TagHelperAttribute("Third", "Third Value");
                var set = new TagHelperAttribute("Set", "Set Value");

                return new TheoryData<
                    IEnumerable<TagHelperAttribute>, // initialAttributes
                    int, // indexToSet
                    TagHelperAttribute, // setValue
                    IEnumerable<TagHelperAttribute>> // expectedAttributes
                {
                    { new[] { first }, 0, set, new[] { set } },
                    { new[] { first, second }, 0, set, new[] { set, second } },
                    { new[] { first, second }, 1, set, new[] { first, set } },
                    { new[] { first, second, third}, 1, set, new[] { first, set, third } },
                    { new[] { first, second, third }, 2, set, new[] { first, second, set } },
                    { new[] { first, first, second, third}, 1, set, new[] { first, set, second, third } },
                };
            }
        }

        [Theory]
        [MemberData(nameof(IntIndexerSetData))]
        public void IntIndexer_SetsAttributeAtExpectedIndex(
            IEnumerable<TagHelperAttribute> initialAttributes,
            int indexToSet,
            TagHelperAttribute setValue,
            IEnumerable<TagHelperAttribute> expectedAttributes)
        {
            // Arrange
            var attributes = new TagHelperAttributeList(initialAttributes);

            // Act
            attributes[indexToSet] = setValue;

            // Assert
            Assert.Equal(expectedAttributes, attributes, CaseSensitiveTagHelperAttributeComparer.Default);
        }

        [Theory]
        [MemberData(
            nameof(ReadOnlyTagHelperAttributeListTest.IntIndexerThrowData),
            MemberType = typeof(ReadOnlyTagHelperAttributeListTest))]
        public void IntIndexer_Getter_ThrowsIfIndexInvalid(int index)
        {
            // Arrange
            var attributes = new TagHelperAttributeList(new[]
                {
                    new TagHelperAttribute("A", "AV"),
                    new TagHelperAttribute("B", "BV")
                });

            // Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>("index", () => attributes[index]);
        }

        [Theory]
        [MemberData(
            nameof(ReadOnlyTagHelperAttributeListTest.IntIndexerThrowData),
            MemberType = typeof(ReadOnlyTagHelperAttributeListTest))]
        public void IntIndexer_Setter_ThrowsIfIndexInvalid(int index)
        {
            // Arrange
            var attributes = new TagHelperAttributeList(new[]
            {
                new TagHelperAttribute("A", "AV"),
                new TagHelperAttribute("B", "BV")
            });

            // Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>("index", () =>
            {
                attributes[index] = new TagHelperAttribute("C", "CV");
            });
        }

        [Theory]
        [MemberData(
            nameof(ReadOnlyTagHelperAttributeListTest.StringIndexerData),
            MemberType = typeof(ReadOnlyTagHelperAttributeListTest))]
        public void StringIndexer_GetsExpectedAttribute(
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

        public static TheoryData StringIndexerSetData
        {
            get
            {
                var A = new TagHelperAttribute("AName", "AName Value");
                var ASet1 = new TagHelperAttribute("AName", "AName Set Value");
                var ASet2 = new TagHelperAttribute("AnAmE", "AName Set Value");
                var A2 = new TagHelperAttribute("aname", "AName Second Value");
                var A3 = new TagHelperAttribute("AName", "AName Third Value");
                var A3Set = new TagHelperAttribute("aname", "AName Third Set Value");
                var B = new TagHelperAttribute("BName", "BName Value");
                var BSet1 = new TagHelperAttribute("BName", "BName Set Value");
                var BSet2 = new TagHelperAttribute("BnAmE", "BName Set Value");
                var C = new TagHelperAttribute("CName", "CName Value");
                var CSet1 = new TagHelperAttribute("CName", "CName Set Value");
                var CSet2 = new TagHelperAttribute("cnamE", "CName Set Value");
                var set = new TagHelperAttribute("Set", "Set Value");

                return new TheoryData<
                    IEnumerable<TagHelperAttribute>, // initialAttributes
                    string, // keyToSet
                    object, // setValue
                    IEnumerable<TagHelperAttribute>> // expectedAttributes
                {
                    { new[] { A }, "AName", ASet1.Value, new[] { ASet1 } },
                    { new[] { A }, "AnAmE", ASet2.Value, new[] { ASet2 } },
                    { new[] { A, B }, "AName", ASet1.Value, new[] { ASet1, B } },
                    { new[] { A, B }, "AnAmE", ASet2.Value, new[] { ASet2, B } },
                    { new[] { A, B }, "BName", BSet1.Value, new[] { A, BSet1 } },
                    { new[] { A, B }, "BnAmE", BSet2.Value, new[] { A, BSet2 } },
                    { new[] { A, B, C }, "BName", BSet1.Value, new[] { A, BSet1, C } },
                    { new[] { A, B, C }, "BnAmE", BSet2.Value, new[] { A, BSet2, C } },
                    { new[] { A, B, C }, "CName", CSet1.Value, new[] { A, B, CSet1 } },
                    { new[] { A, B, C }, "cnamE", CSet2.Value, new[] { A, B, CSet2 } },
                    { Enumerable.Empty<TagHelperAttribute>(), "Set", set.Value, new[] { set } },
                    { new[] { B }, "Set", set.Value, new[] { B, set } },
                    { new[] { A, B }, "Set", set.Value, new[] { A, B, set } },

                    // Multiple elements same name
                    { new[] { A, B, A2, C }, "AName", ASet1.Value, new[] { ASet1, B, C } },
                    { new[] { A, B, A2, C }, "AnAmE", ASet2.Value, new[] { ASet2, B, C } },
                    { new[] { B, A2, A }, "AName", ASet1.Value, new[] { B, ASet1 } },
                    { new[] { B, A2, A, C }, "AnAmE", ASet2.Value, new[] { B, ASet2, C } },
                    { new[] { A, A3 }, "aname", A3Set.Value, new[] { A3Set } },
                    { new[] { A3, A }, "aname", A3Set.Value, new[] { A3Set } },
                    { new[] { A, A2, A3 }, "AName", ASet1.Value, new[] { ASet1 } },
                    { new[] { A, A2, A3 }, "BName", BSet1.Value, new[] { A, A2, A3, BSet1 } },
                    { new[] { A, A2, A3, B, C }, "Set", set.Value, new[] { A, A2, A3, B, C, set } },
                };
            }
        }

        [Theory]
        [MemberData(nameof(StringIndexerSetData))]
        public void StringIndexer_SetsAttributeAtExpectedLocation(
            IEnumerable<TagHelperAttribute> initialAttributes,
            string keyToSet,
            object setValue,
            IEnumerable<TagHelperAttribute> expectedAttributes)
        {
            // Arrange
            var attributes = new TagHelperAttributeList(initialAttributes);

            // Act
            attributes.SetAttribute(keyToSet, setValue);

            // Assert
            Assert.Equal(expectedAttributes, attributes, CaseSensitiveTagHelperAttributeComparer.Default);
        }

        [Fact]
        public void StringIndexer_Setter_ThrowsIfIndexInvalid()
        {
            // Arrange
            var attributes = new TagHelperAttributeList(new[]
            {
                new TagHelperAttribute("A", "AV"),
                new TagHelperAttribute("B", "BV")
            });

            // Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>("index", () =>
            {
                attributes[2] = new TagHelperAttribute("C", "CV");
            });
        }

        [Fact]
        public void ICollection_IsReadOnly_ReturnsFalse()
        {
            // Arrange
            var attributes = new TagHelperAttributeList() as ICollection<TagHelperAttribute>;

            // Act
            var isReadOnly = attributes.IsReadOnly;

            // Assert
            Assert.False(isReadOnly);
        }

        public static TheoryData AddData
        {
            get
            {
                var A = new TagHelperAttribute("AName", "AName Value");
                var A2 = new TagHelperAttribute("aname", "AName Second Value");
                var B = new TagHelperAttribute("BName", "BName Value");

                return new TheoryData<
                    IEnumerable<TagHelperAttribute>, // initialAttributes
                    TagHelperAttribute, // attributeToAdd
                    IEnumerable<TagHelperAttribute>> // expectedAttributes
                {
                    { Enumerable.Empty<TagHelperAttribute>(), A, new[] { A } },
                    { new[] { A }, B, new[] { A, B } },
                    { new[] { A }, A2, new[] { A, A2 } },
                };
            }
        }

        [Theory]
        [MemberData(nameof(AddData))]
        public void Add_AppendsAttributes(
            IEnumerable<TagHelperAttribute> initialAttributes,
            TagHelperAttribute attributeToAdd,
            IEnumerable<TagHelperAttribute> expectedAttributes)
        {
            // Arrange
            var attributes = new TagHelperAttributeList(initialAttributes);

            // Act
            attributes.Add(attributeToAdd);

            // Assert
            Assert.Equal(expectedAttributes, attributes, CaseSensitiveTagHelperAttributeComparer.Default);
        }

        public static TheoryData InsertData
        {
            get
            {
                var A = new TagHelperAttribute("AName", "AName Value");
                var A2 = new TagHelperAttribute("aname", "AName Second Value");
                var B = new TagHelperAttribute("BName", "BName Value");

                return new TheoryData<
                    IEnumerable<TagHelperAttribute>, // initialAttributes
                    TagHelperAttribute, // attributeToAdd
                    int, // locationToInsert
                    IEnumerable<TagHelperAttribute>> // expectedAttributes
                {
                    { Enumerable.Empty<TagHelperAttribute>(), A, 0, new[] { A } },
                    { new[] { A }, B, 1, new[] { A, B } },
                    { new[] { A }, B, 0, new[] { B, A } },
                    { new[] { A }, A2, 1, new[] { A, A2 } },
                    { new[] { A }, A2, 0, new[] { A2, A } },
                    { new[] { A, B }, A2, 0, new[] { A2, A, B } },
                    { new[] { A, B }, A2, 1, new[] { A, A2, B } },
                    { new[] { A, B }, A2, 2, new[] { A, B, A2 } },
                };
            }
        }

        [Theory]
        [MemberData(nameof(InsertData))]
        public void Insert_InsertsAttributes(
            IEnumerable<TagHelperAttribute> initialAttributes,
            TagHelperAttribute attributeToAdd,
            int locationToInsert,
            IEnumerable<TagHelperAttribute> expectedAttributes)
        {
            // Arrange
            var attributes = new TagHelperAttributeList(initialAttributes);

            // Act
            attributes.Insert(locationToInsert, attributeToAdd);

            // Assert
            Assert.Equal(expectedAttributes, attributes, CaseSensitiveTagHelperAttributeComparer.Default);
        }

        [Fact]
        public void Insert_ThrowsWhenIndexIsOutOfRange()
        {
            // Arrange
            var attributes = new TagHelperAttributeList(
                new[]
                {
                    new TagHelperAttribute("a", "av"),
                    new TagHelperAttribute("b", "bv"),
                });

            // Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>("index",
                () => attributes.Insert(3, new TagHelperAttribute("c", "cb")));
        }

        public static TheoryData CopyToData
        {
            get
            {
                var A = new TagHelperAttribute("AName", "AName Value");
                var A2 = new TagHelperAttribute("aname", "AName Second Value");
                var B = new TagHelperAttribute("BName", "BName Value");

                return new TheoryData<
                    IEnumerable<TagHelperAttribute>, // initialAttributes
                    TagHelperAttribute[], // attributesToCopy
                    int, // locationToCopy
                    IEnumerable<TagHelperAttribute>> // expectedAttributes
                {
                    { Enumerable.Empty<TagHelperAttribute>(), new[] { A }, 0, new[] { A } },
                    { Enumerable.Empty<TagHelperAttribute>(), new[] { A, B }, 0, new[] { A, B } },
                    { new[] { A }, new[] { B }, 1, new[] { A, B } },
                    { new[] { A }, new[] { B }, 0, new[] { B } },
                    { new[] { A }, new[] { A2 }, 1, new[] { A, A2 } },
                    { new[] { A }, new[] { A2 }, 0, new[] { A2 } },
                    { new[] { A, B }, new[] { A2 }, 0, new[] { A2, B } },
                    { new[] { A, B }, new[] { A2 }, 1, new[] { A, A2 } },
                    { new[] { A, B }, new[] { A2 }, 2, new[] { A, B, A2 } },
                    { new[] { A, B }, new[] { A2, A2 }, 0, new[] { A2, A2 } },
                    { new[] { A, B, A2 }, new[] { A2, A2 }, 1, new[] { A, A2, A2 } },
                    { new[] { A, B }, new[] { A2, A2 }, 2, new[] { A, B, A2, A2 } },
                };
            }
        }

        [Theory]
        [MemberData(nameof(CopyToData))]
        public void CopyTo_CopiesAttributes(
            IEnumerable<TagHelperAttribute> initialAttributes,
            TagHelperAttribute[] attributesToCopy,
            int locationToCopy,
            IEnumerable<TagHelperAttribute> expectedAttributes)
        {
            // Arrange
            var attributes = new TagHelperAttributeList(initialAttributes);
            var attributeDestination = new TagHelperAttribute[expectedAttributes.Count()];
            attributes.ToArray().CopyTo(attributeDestination, 0);

            // Act
            attributesToCopy.CopyTo(attributeDestination, locationToCopy);

            // Assert
            Assert.Equal(expectedAttributes, attributeDestination, CaseSensitiveTagHelperAttributeComparer.Default);
        }

        public static TheoryData RemoveAllData
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
                    string, // keyToRemove
                    IEnumerable<TagHelperAttribute>, // expectedAttributes
                    bool> // expectedRemoval
                {
                    { new[] { A }, "AName", Enumerable.Empty<TagHelperAttribute>(), true },
                    { new[] { A }, "AnAmE", Enumerable.Empty<TagHelperAttribute>(), true },
                    { new[] { A, B }, "AName", new[] { B }, true },
                    { new[] { A, B }, "AnAmE", new[] { B }, true },
                    { new[] { A, B }, "BName", new[] { A }, true },
                    { new[] { A, B }, "BnAmE", new[] { A }, true },
                    { new[] { A, B, C }, "BName", new[] { A, C }, true },
                    { new[] { A, B, C }, "bname", new[] { A, C }, true },
                    { new[] { A, B, C }, "CName", new[] { A, B }, true },
                    { new[] { A, B, C }, "cnamE", new[] { A, B }, true },

                    // Multiple elements same name
                    { new[] { A, B, A2, C }, "AName", new[] { B, C }, true },
                    { new[] { A, B, A2, C }, "aname", new[] { B, C }, true },
                    { new[] { B, A2, A }, "aname", new[] { B }, true },
                    { new[] { B, A2, A, C }, "AName", new[] { B, C }, true },
                    { new[] { A, A3 }, "AName", Enumerable.Empty<TagHelperAttribute>(), true },
                    { new[] { A3, A }, "aname", Enumerable.Empty<TagHelperAttribute>(), true },
                    { new[] { A, A2, A3 }, "AName", Enumerable.Empty<TagHelperAttribute>(), true },
                    { new[] { C, B, A3, A }, "AName", new[] { C, B }, true },

                    // No removal expected lookups
                    { Enumerable.Empty<TagHelperAttribute>(), "_0_", Enumerable.Empty<TagHelperAttribute>(), false },
                    { new[] { A }, "_AName_", new[] { A }, false },
                    { new[] { A }, "completely different", new[] { A }, false },
                    { new[] { A, B }, "_AName_", new[] { A, B }, false },
                    { new[] { A, B }, "completely different", new[] { A, B }, false },
                    { new[] { A, B, C }, "_BName_", new[] { A, B, C }, false },
                    { new[] { A, B, C }, "completely different", new[] { A, B, C }, false },
                    { new[] { A, A2, B, C }, "_cnamE_", new[] { A, A2, B, C }, false },
                    { new[] { A, A2, B, C }, "completely different", new[] { A, A2, B, C }, false },
                    { new[] { A, A2, A3, B, C }, "_cnamE_", new[] { A, A2, A3, B, C }, false },
                    { new[] { A, A2, A3, B, C }, "completely different", new[] { A, A2, A3, B, C }, false },
                };
            }
        }

        [Theory]
        [MemberData(nameof(RemoveAllData))]
        public void RemoveAll_RemovesAllExpectedAttributes(
            IEnumerable<TagHelperAttribute> initialAttributes,
            string keyToRemove,
            IEnumerable<TagHelperAttribute> expectedAttributes,
            bool expectedRemoval)
        {
            // Arrange
            var attributes = new TagHelperAttributeList(initialAttributes);

            // Act
            var removed = attributes.RemoveAll(keyToRemove);

            // Assert
            Assert.Equal(expectedRemoval, removed);
            Assert.Equal(expectedAttributes, attributes, CaseSensitiveTagHelperAttributeComparer.Default);
        }

        public static TheoryData RemoveData
        {
            get
            {
                var A = new TagHelperAttribute("AName", "av");
                var A2 = new TagHelperAttribute("aname", "av");
                var A3 = new TagHelperAttribute("AName", "av");
                var B = new TagHelperAttribute("BName", "bv");
                var C = new TagHelperAttribute("CName", "cv");
                var empty = Enumerable.Empty<TagHelperAttribute>();

                return new TheoryData<
                    IEnumerable<TagHelperAttribute>, // initialAttributes
                    TagHelperAttribute, // attributeToRemove
                    IEnumerable<TagHelperAttribute>, // expectedAttributes
                    bool> // expectedResult
                {
                    { new[] { A }, A, empty, true },
                    { new[] { A }, new TagHelperAttribute("AnAmE", "av"), empty, true },
                    { new[] { A, B }, A, new[] { B }, true },
                    { new[] { A, B }, new TagHelperAttribute("AnAmE", "av"), new[] { B }, true },
                    { new[] { A, B }, B, new[] { A }, true },
                    { new[] { A, B }, new TagHelperAttribute("BnAmE", "bv"), new[] { A }, true },
                    { new[] { A, B, C }, B, new[] { A, C }, true },
                    { new[] { A, B, C }, new TagHelperAttribute("bname", "bv"), new[] { A, C }, true },
                    { new[] { A, B, C }, C, new[] { A, B }, true },
                    { new[] { A, B, C }, new TagHelperAttribute("cnamE", "cv"), new[] { A, B }, true },

                    // Multiple elements same name
                    { new[] { A, B, A2, C }, A, new[] { B, A2, C }, true },
                    { new[] { A, B, A2, C }, new TagHelperAttribute("aname", "av"), new[] { B, A2, C }, true },
                    { new[] { B, A2, A }, new TagHelperAttribute("aname", "av"), new[] { B, A }, true },
                    { new[] { B, A2, A, C }, A, new[] { B, A, C }, true },
                    { new[] { A, A3 }, A3, new[] { A3 }, true },
                    { new[] { A3, A }, new TagHelperAttribute("aname", "av"), new[] { A }, true },
                    { new[] { A, A2, A3 }, new TagHelperAttribute("AName", "av"), new[] { A2, A3 }, true },
                    { new[] { C, B, A3, A }, new TagHelperAttribute("AName", "av"), new[] { C, B, A }, true },

                    // Null expected lookups
                    { Enumerable.Empty<TagHelperAttribute>(), new TagHelperAttribute("DoesNotExist", "_0_"), Enumerable.Empty<TagHelperAttribute>(), false },
                    { new[] { A }, new TagHelperAttribute("DoesNotExist", "_AName_"), new[] { A }, false },
                    { new[] { A }, new TagHelperAttribute("DoesNotExist", "completely different"), new[] { A }, false },
                    { new[] { A, B }, new TagHelperAttribute("DoesNotExist", "_AName_"), new[] { A, B }, false },
                    { new[] { A, B }, new TagHelperAttribute("DoesNotExist", "completely different"), new[] { A, B }, false },
                    { new[] { A, B, C }, new TagHelperAttribute("DoesNotExist", "_BName_"), new[] { A, B, C }, false },
                    { new[] { A, B, C }, new TagHelperAttribute("DoesNotExist", "completely different"), new[] { A, B, C }, false },
                    { new[] { A, A2, B, C }, new TagHelperAttribute("DoesNotExist", "_cnamE_"), new[] { A, A2, B, C }, false },
                    { new[] { A, A2, B, C }, new TagHelperAttribute("DoesNotExist", "completely different"), new[] { A, A2, B, C }, false },
                    { new[] { A, A2, A3, B, C }, new TagHelperAttribute("DoesNotExist", "_cnamE_"), new[] { A, A2, A3, B, C }, false },
                    { new[] { A, A2, A3, B, C }, new TagHelperAttribute("DoesNotExist", "completely different"), new[] { A, A2, A3, B, C }, false },
                };
            }
        }

        [Theory]
        [MemberData(nameof(RemoveData))]
        public void Remove_ReturnsExpectedValueAndRemovesFirstAttribute(
            IEnumerable<TagHelperAttribute> initialAttributes,
            TagHelperAttribute attributeToRemove,
            IEnumerable<TagHelperAttribute> expectedAttributes,
            bool expectedResult)
        {
            // Arrange
            var attributes = new TagHelperAttributeList(initialAttributes);

            // Act
            var result = attributes.Remove(attributeToRemove);

            // Assert
            Assert.Equal(expectedResult, result);
            Assert.Equal(expectedAttributes, attributes, CaseSensitiveTagHelperAttributeComparer.Default);
        }

        public static TheoryData RemoveAtData
        {
            get
            {
                var A = new TagHelperAttribute("AName", "AName Value");
                var A2 = new TagHelperAttribute("aname", "AName Second Value");
                var B = new TagHelperAttribute("BName", "BName Value");

                return new TheoryData<
                    IEnumerable<TagHelperAttribute>, // initialAttributes
                    int, // locationToRemove
                    IEnumerable<TagHelperAttribute>> // expectedAttributes
                {
                    { new[] { A }, 0, Enumerable.Empty<TagHelperAttribute>() },
                    { new[] { A, B }, 0, new[] { B } },
                    { new[] { A, B }, 1, new[] { A } },
                    { new[] { A, A2 }, 0, new[] { A2 } },
                    { new[] { A, A2 }, 1, new[] { A } },
                    { new[] { A, B, A2 }, 0, new[] { B, A2 } },
                    { new[] { A, B, A2 }, 1, new[] { A, A2 } },
                    { new[] { A, B, A2 }, 2, new[] { A, B } },
                };
            }
        }

        [Theory]
        [MemberData(nameof(RemoveAtData))]
        public void RemoveAt_RemovesAttributeAtSpecifiedIndex(
            IEnumerable<TagHelperAttribute> initialAttributes,
            int locationToRemove,
            IEnumerable<TagHelperAttribute> expectedAttributes)
        {
            // Arrange
            var attributes = new TagHelperAttributeList(initialAttributes);

            // Act
            attributes.RemoveAt(locationToRemove);

            // Assert
            Assert.Equal(expectedAttributes, attributes, CaseSensitiveTagHelperAttributeComparer.Default);
        }

        [Fact]
        public void RemoveAt_ThrowsWhenIndexIsOutOfRange()
        {
            // Arrange
            var attributes = new TagHelperAttributeList(
                new[]
                {
                    new TagHelperAttribute("a", "av"),
                    new TagHelperAttribute("b", "bv"),
                });

            // Act & Assert
            var exception = Assert.Throws<ArgumentOutOfRangeException>("index",
                () => attributes.RemoveAt(3));
        }

        [Fact]
        public void Clear_RemovesAllAttributes()
        {
            // Arrange
            var attributes = new TagHelperAttributeList(
                new[]
                {
                    new TagHelperAttribute("a", "av"),
                    new TagHelperAttribute("b", "bv"),
                });

            // Act
            attributes.Clear();

            // Assert
            Assert.Empty(attributes);
        }
    }
}