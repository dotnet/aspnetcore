// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Text;
using Xunit;

namespace Microsoft.AspNet.Razor.Generator
{
    public class DynamicAttributeBlockCodeGeneratorTest
    {
        public static TheoryData<DynamicAttributeBlockCodeGenerator, DynamicAttributeBlockCodeGenerator> MatchingTestDataSet
        {
            get
            {
                return new TheoryData<DynamicAttributeBlockCodeGenerator, DynamicAttributeBlockCodeGenerator>
                {
                    {
                        new DynamicAttributeBlockCodeGenerator(prefix: null, offset: 0, line: 0, col: 0),
                        new DynamicAttributeBlockCodeGenerator(prefix: null, offset: 0, line: 0, col: 0)
                    },
                    {
                        new DynamicAttributeBlockCodeGenerator(
                            prefix: new LocationTagged<string>(value: "Fred", offset: 0, line: 0, col: 0),
                            offset: 10,
                            line: 11,
                            col: 12),
                        new DynamicAttributeBlockCodeGenerator(
                            prefix: new LocationTagged<string>(value: "Fred", offset: 0, line: 0, col: 0),
                            offset: 10,
                            line: 11,
                            col: 12)
                    },
                    // ValueStart not involved in equality check or hash code calculation.
                    {
                        new DynamicAttributeBlockCodeGenerator(
                            prefix: new LocationTagged<string>(value: "Ginger", offset: 10, line: 11, col: 12),
                            offset: 10,
                            line: 11,
                            col: 12),
                        new DynamicAttributeBlockCodeGenerator(
                            prefix: new LocationTagged<string>(value: "Ginger", offset: 10, line: 11, col: 12),
                            offset: 100,
                            line: 11,
                            col: 12)
                    },
                    {
                        new DynamicAttributeBlockCodeGenerator(
                            prefix: new LocationTagged<string>(value: "George", offset: 10, line: 11, col: 12),
                            offset: 10,
                            line: 11,
                            col: 12),
                        new DynamicAttributeBlockCodeGenerator(
                            prefix: new LocationTagged<string>(value: "George", offset: 10, line: 11, col: 12),
                            offset: 10,
                            line: 110,
                            col: 12)
                    },
                    {
                        new DynamicAttributeBlockCodeGenerator(
                            prefix: new LocationTagged<string>(value: "Dean", offset: 10, line: 11, col: 12),
                            offset: 10,
                            line: 11,
                            col: 12),
                        new DynamicAttributeBlockCodeGenerator(
                            prefix: new LocationTagged<string>(value: "Dean", offset: 10, line: 11, col: 12),
                            offset: 10,
                            line: 11,
                            col: 120)
                    },
                };
            }
        }

        public static TheoryData<DynamicAttributeBlockCodeGenerator, object> NonMatchingTestDataSet
        {
            get
            {
                return new TheoryData<DynamicAttributeBlockCodeGenerator, object>
                {
                    {
                        new DynamicAttributeBlockCodeGenerator(prefix: null, offset: 0, line: 0, col: 0),
                        null
                    },
                    {
                        new DynamicAttributeBlockCodeGenerator(
                            prefix: new LocationTagged<string>(value: "Ginger", offset: 0, line: 0, col: 0),
                            offset: 10,
                            line: 11,
                            col: 12),
                        null
                    },
                    {
                        new DynamicAttributeBlockCodeGenerator(
                            prefix: new LocationTagged<string>(value: "Ginger", offset: 10, line: 11, col: 12),
                            offset: 10,
                            line: 11,
                            col: 12),
                        new object()
                    },
                    {
                        new DynamicAttributeBlockCodeGenerator(
                            prefix: new LocationTagged<string>(value: "George", offset: 10, line: 11, col: 12),
                            offset: 10,
                            line: 11,
                            col: 12),
                        new AttributeBlockCodeGenerator(
                            name: "Fred",
                            prefix: new LocationTagged<string>(value: "Ginger", offset: 10, line: 11, col: 12),
                            suffix: new LocationTagged<string>(value: "George", offset: 13, line: 14, col: 15))
                    },
                    {
                        // Different Prefix.
                        new DynamicAttributeBlockCodeGenerator(
                            prefix: new LocationTagged<string>(value: "Ginger", offset: 10, line: 11, col: 12),
                            offset: 10,
                            line: 11,
                            col: 12),
                        new DynamicAttributeBlockCodeGenerator(
                            prefix: new LocationTagged<string>(value: "George", offset: 10, line: 11, col: 12),
                            offset: 10,
                            line: 11,
                            col: 12)
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(MatchingTestDataSet))]
        public void Equals_True_WhenExpected(
            DynamicAttributeBlockCodeGenerator leftObject,
            DynamicAttributeBlockCodeGenerator rightObject)
        {
            // Arrange & Act
            var result = leftObject.Equals(rightObject);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [MemberData(nameof(NonMatchingTestDataSet))]
        public void Equals_False_WhenExpected(DynamicAttributeBlockCodeGenerator leftObject, object rightObject)
        {
            // Arrange & Act
            var result = leftObject.Equals(rightObject);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [MemberData(nameof(MatchingTestDataSet))]
        public void GetHashCode_ReturnsSameValue_WhenEqual(
            DynamicAttributeBlockCodeGenerator leftObject,
            DynamicAttributeBlockCodeGenerator rightObject)
        {
            // Arrange & Act
            var leftResult = leftObject.GetHashCode();
            var rightResult = rightObject.GetHashCode();

            // Assert
            Assert.Equal(leftResult, rightResult);
        }
    }
}
