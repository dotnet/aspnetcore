// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Text;
using Xunit;

namespace Microsoft.AspNet.Razor.Chunks.Generators
{
    public class AttributeBlockChunkGeneratorTest
    {
        public static TheoryData<AttributeBlockChunkGenerator, AttributeBlockChunkGenerator> MatchingTestDataSet
        {
            get
            {
                return new TheoryData<AttributeBlockChunkGenerator, AttributeBlockChunkGenerator>
                {
                    {
                        new AttributeBlockChunkGenerator(name: null, prefix: null, suffix: null),
                        new AttributeBlockChunkGenerator(name: null, prefix: null, suffix: null)
                    },
                    {
                        new AttributeBlockChunkGenerator(
                            name: "Fred",
                            prefix: new LocationTagged<string>(value: "Ginger", offset: 0, line: 0, col: 0),
                            suffix: new LocationTagged<string>(value: "George", offset: 0, line: 0, col: 0)),
                        new AttributeBlockChunkGenerator(
                            name: "Fred",
                            prefix: new LocationTagged<string>(value: "Ginger", offset: 0, line: 0, col: 0),
                            suffix: new LocationTagged<string>(value: "George", offset: 0, line: 0, col: 0))
                    },
                    {
                        new AttributeBlockChunkGenerator(
                            name: "Fred",
                            prefix: new LocationTagged<string>(value: "Ginger", offset: 10, line: 11, col: 12),
                            suffix: new LocationTagged<string>(value: "George", offset: 13, line: 14, col: 15)),
                        new AttributeBlockChunkGenerator(
                            name: "Fred",
                            prefix: new LocationTagged<string>(value: "Ginger", offset: 10, line: 11, col: 12),
                            suffix: new LocationTagged<string>(value: "George", offset: 13, line: 14, col: 15))
                    },
                };
            }
        }

        public static TheoryData<AttributeBlockChunkGenerator, object> NonMatchingTestDataSet
        {
            get
            {
                return new TheoryData<AttributeBlockChunkGenerator, object>
                {
                    {
                        new AttributeBlockChunkGenerator(name: null, prefix: null, suffix: null),
                        null
                    },
                    {
                        new AttributeBlockChunkGenerator(
                            name: "Fred",
                            prefix: new LocationTagged<string>(value: "Ginger", offset: 0, line: 0, col: 0),
                            suffix: new LocationTagged<string>(value: "George", offset: 0, line: 0, col: 0)),
                        null
                    },
                    {
                        new AttributeBlockChunkGenerator(
                            name: "Fred",
                            prefix: new LocationTagged<string>(value: "Ginger", offset: 10, line: 11, col: 12),
                            suffix: new LocationTagged<string>(value: "George", offset: 13, line: 14, col: 15)),
                        null
                    },
                    {
                        new AttributeBlockChunkGenerator(
                            name: "Fred",
                            prefix: new LocationTagged<string>(value: "Ginger", offset: 10, line: 11, col: 12),
                            suffix: new LocationTagged<string>(value: "George", offset: 13, line: 14, col: 15)),
                        new object()
                    },
                    {
                        new AttributeBlockChunkGenerator(
                            name: "Fred",
                            prefix: new LocationTagged<string>(value: "Ginger", offset: 10, line: 11, col: 12),
                            suffix: new LocationTagged<string>(value: "George", offset: 13, line: 14, col: 15)),
                        new RazorCommentChunkGenerator()
                    },
                    {
                        // Different Name.
                        new AttributeBlockChunkGenerator(name: "Fred", prefix: null, suffix: null),
                        new AttributeBlockChunkGenerator(name: "Ginger", prefix: null, suffix: null)
                    },
                    {
                        // Different Name (case sensitive).
                        new AttributeBlockChunkGenerator(name: "fred", prefix: null, suffix: null),
                        new AttributeBlockChunkGenerator(name: "FRED", prefix: null, suffix: null)
                    },
                    {
                        // Different Prefix.
                        new AttributeBlockChunkGenerator(
                            name: null,
                            prefix: new LocationTagged<string>(value: "Ginger", offset: 10, line: 11, col: 12),
                            suffix: null),
                        new AttributeBlockChunkGenerator(
                            name: null,
                            prefix: new LocationTagged<string>(value: "George", offset: 10, line: 11, col: 12),
                            suffix: null)
                    },
                    {
                        // Different Suffix.
                        new AttributeBlockChunkGenerator(
                            name: null,
                            prefix: null,
                            suffix: new LocationTagged<string>(value: "Ginger", offset: 10, line: 11, col: 12)),
                        new AttributeBlockChunkGenerator(
                            name: null,
                            prefix: null,
                            suffix: new LocationTagged<string>(value: "George", offset: 10, line: 11, col: 12))
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(MatchingTestDataSet))]
        public void Equals_True_WhenExpected(
            AttributeBlockChunkGenerator leftObject,
            AttributeBlockChunkGenerator rightObject)
        {
            // Arrange & Act
            var result = leftObject.Equals(rightObject);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [MemberData(nameof(NonMatchingTestDataSet))]
        public void Equals_False_WhenExpected(AttributeBlockChunkGenerator leftObject, object rightObject)
        {
            // Arrange & Act
            var result = leftObject.Equals(rightObject);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [MemberData(nameof(MatchingTestDataSet))]
        public void GetHashCode_ReturnsSameValue_WhenEqual(
            AttributeBlockChunkGenerator leftObject,
            AttributeBlockChunkGenerator rightObject)
        {
            // Arrange & Act
            var leftResult = leftObject.GetHashCode();
            var rightResult = rightObject.GetHashCode();

            // Assert
            Assert.Equal(leftResult, rightResult);
        }
    }
}
