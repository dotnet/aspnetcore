// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Chunks.Generators
{
    public class AddImportChunkGeneratorTest
    {
        public static TheoryData<AddImportChunkGenerator, AddImportChunkGenerator> MatchingTestDataSet
        {
            get
            {
                return new TheoryData<AddImportChunkGenerator, AddImportChunkGenerator>
                {
                    {
                        new AddImportChunkGenerator(ns: null),
                        new AddImportChunkGenerator(ns: null)
                    },
                    {
                        new AddImportChunkGenerator(ns: "Fred"),
                        new AddImportChunkGenerator(ns: "Fred")
                    },
                };
            }
        }

        public static TheoryData<AddImportChunkGenerator, object> NonMatchingTestDataSet
        {
            get
            {
                return new TheoryData<AddImportChunkGenerator, object>
                {
                    {
                        new AddImportChunkGenerator(ns: null),
                        null
                    },
                    {
                        new AddImportChunkGenerator(ns: "Fred"),
                        null
                    },
                    {
                        new AddImportChunkGenerator(ns: "Fred"),
                        new object()
                    },
                    {
                        new AddImportChunkGenerator(ns: "Fred"),
                        SpanChunkGenerator.Null
                    },
                    {
                        new AddImportChunkGenerator(ns: "Fred"),
                        new StatementChunkGenerator()
                    },
                    {
                        // Different Namespace.
                        new AddImportChunkGenerator(ns: "Fred"),
                        new AddImportChunkGenerator(ns: "Ginger")
                    },
                    {
                        // Different Namespace (case sensitive).
                        new AddImportChunkGenerator(ns: "fred"),
                        new AddImportChunkGenerator(ns: "FRED")
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(MatchingTestDataSet))]
        public void Equals_True_WhenExpected(AddImportChunkGenerator leftObject, AddImportChunkGenerator rightObject)
        {
            // Arrange & Act
            var result = leftObject.Equals(rightObject);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [MemberData(nameof(NonMatchingTestDataSet))]
        public void Equals_False_WhenExpected(AddImportChunkGenerator leftObject, object rightObject)
        {
            // Arrange & Act
            var result = leftObject.Equals(rightObject);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [MemberData(nameof(MatchingTestDataSet))]
        public void GetHashCode_ReturnsSameValue_WhenEqual(
            AddImportChunkGenerator leftObject,
            AddImportChunkGenerator rightObject)
        {
            // Arrange & Act
            var leftResult = leftObject.GetHashCode();
            var rightResult = rightObject.GetHashCode();

            // Assert
            Assert.Equal(leftResult, rightResult);
        }
    }
}
