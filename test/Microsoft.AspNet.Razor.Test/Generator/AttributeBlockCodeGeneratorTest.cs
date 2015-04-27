// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Text;
using Xunit;

namespace Microsoft.AspNet.Razor.Generator
{
    public class AttributeBlockCodeGeneratorTest
    {
        public static TheoryData<AttributeBlockCodeGenerator, AttributeBlockCodeGenerator> MatchingTestDataSet
        {
            get
            {
                return new TheoryData<AttributeBlockCodeGenerator, AttributeBlockCodeGenerator>
                {
                    {
                        new AttributeBlockCodeGenerator(name: null, prefix: null, suffix: null),
                        new AttributeBlockCodeGenerator(name: null, prefix: null, suffix: null)
                    },
                    {
                        new AttributeBlockCodeGenerator(
                            name: "Fred",
                            prefix: new LocationTagged<string>(value: "Ginger", offset: 0, line: 0, col: 0),
                            suffix: new LocationTagged<string>(value: "George", offset: 0, line: 0, col: 0)),
                        new AttributeBlockCodeGenerator(
                            name: "Fred",
                            prefix: new LocationTagged<string>(value: "Ginger", offset: 0, line: 0, col: 0),
                            suffix: new LocationTagged<string>(value: "George", offset: 0, line: 0, col: 0))
                    },
                    {
                        new AttributeBlockCodeGenerator(
                            name: "Fred",
                            prefix: new LocationTagged<string>(value: "Ginger", offset: 10, line: 11, col: 12),
                            suffix: new LocationTagged<string>(value: "George", offset: 13, line: 14, col: 15)),
                        new AttributeBlockCodeGenerator(
                            name: "Fred",
                            prefix: new LocationTagged<string>(value: "Ginger", offset: 10, line: 11, col: 12),
                            suffix: new LocationTagged<string>(value: "George", offset: 13, line: 14, col: 15))
                    },
                };
            }
        }

        public static TheoryData<AttributeBlockCodeGenerator, object> NonMatchingTestDataSet
        {
            get
            {
                return new TheoryData<AttributeBlockCodeGenerator, object>
                {
                    {
                        new AttributeBlockCodeGenerator(name: null, prefix: null, suffix: null),
                        null
                    },
                    {
                        new AttributeBlockCodeGenerator(
                            name: "Fred",
                            prefix: new LocationTagged<string>(value: "Ginger", offset: 0, line: 0, col: 0),
                            suffix: new LocationTagged<string>(value: "George", offset: 0, line: 0, col: 0)),
                        null
                    },
                    {
                        new AttributeBlockCodeGenerator(
                            name: "Fred",
                            prefix: new LocationTagged<string>(value: "Ginger", offset: 10, line: 11, col: 12),
                            suffix: new LocationTagged<string>(value: "George", offset: 13, line: 14, col: 15)),
                        null
                    },
                    {
                        new AttributeBlockCodeGenerator(
                            name: "Fred",
                            prefix: new LocationTagged<string>(value: "Ginger", offset: 10, line: 11, col: 12),
                            suffix: new LocationTagged<string>(value: "George", offset: 13, line: 14, col: 15)),
                        new object()
                    },
                    {
                        new AttributeBlockCodeGenerator(
                            name: "Fred",
                            prefix: new LocationTagged<string>(value: "Ginger", offset: 10, line: 11, col: 12),
                            suffix: new LocationTagged<string>(value: "George", offset: 13, line: 14, col: 15)),
                        new RazorCommentCodeGenerator()
                    },
                    {
                        // Different Name.
                        new AttributeBlockCodeGenerator(name: "Fred", prefix: null, suffix: null),
                        new AttributeBlockCodeGenerator(name: "Ginger", prefix: null, suffix: null)
                    },
                    {
                        // Different Name (case sensitive).
                        new AttributeBlockCodeGenerator(name: "fred", prefix: null, suffix: null),
                        new AttributeBlockCodeGenerator(name: "FRED", prefix: null, suffix: null)
                    },
                    {
                        // Different Prefix.
                        new AttributeBlockCodeGenerator(
                            name: null,
                            prefix: new LocationTagged<string>(value: "Ginger", offset: 10, line: 11, col: 12),
                            suffix: null),
                        new AttributeBlockCodeGenerator(
                            name: null,
                            prefix: new LocationTagged<string>(value: "George", offset: 10, line: 11, col: 12),
                            suffix: null)
                    },
                    {
                        // Different Suffix.
                        new AttributeBlockCodeGenerator(
                            name: null,
                            prefix: null,
                            suffix: new LocationTagged<string>(value: "Ginger", offset: 10, line: 11, col: 12)),
                        new AttributeBlockCodeGenerator(
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
            AttributeBlockCodeGenerator leftObject,
            AttributeBlockCodeGenerator rightObject)
        {
            // Arrange & Act
            var result = leftObject.Equals(rightObject);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [MemberData(nameof(NonMatchingTestDataSet))]
        public void Equals_False_WhenExpected(AttributeBlockCodeGenerator leftObject, object rightObject)
        {
            // Arrange & Act
            var result = leftObject.Equals(rightObject);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [MemberData(nameof(MatchingTestDataSet))]
        public void GetHashCode_ReturnsSameValue_WhenEqual(
            AttributeBlockCodeGenerator leftObject,
            AttributeBlockCodeGenerator rightObject)
        {
            // Arrange & Act
            var leftResult = leftObject.GetHashCode();
            var rightResult = rightObject.GetHashCode();

            // Assert
            Assert.Equal(leftResult, rightResult);
        }
    }
}
