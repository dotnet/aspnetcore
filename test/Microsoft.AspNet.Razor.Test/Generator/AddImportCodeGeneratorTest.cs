// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNet.Razor.Generator
{
    public class AddImportCodeGeneratorTest
    {
        public static TheoryData<AddImportCodeGenerator, AddImportCodeGenerator> MatchingTestDataSet
        {
            get
            {
                return new TheoryData<AddImportCodeGenerator, AddImportCodeGenerator>
                {
                    {
                        new AddImportCodeGenerator(ns: null, namespaceKeywordLength: 3),
                        new AddImportCodeGenerator(ns: null, namespaceKeywordLength: 3)
                    },
                    {
                        new AddImportCodeGenerator(ns: "Fred", namespaceKeywordLength: 23),
                        new AddImportCodeGenerator(ns: "Fred", namespaceKeywordLength: 23)
                    },
                };
            }
        }

        public static TheoryData<AddImportCodeGenerator, object> NonMatchingTestDataSet
        {
            get
            {
                return new TheoryData<AddImportCodeGenerator, object>
                {
                    {
                        new AddImportCodeGenerator(ns: null, namespaceKeywordLength: 0),
                        null
                    },
                    {
                        new AddImportCodeGenerator(ns: "Fred", namespaceKeywordLength: 23),
                        null
                    },
                    {
                        new AddImportCodeGenerator(ns: "Fred", namespaceKeywordLength: 23),
                        new object()
                    },
                    {
                        new AddImportCodeGenerator(ns: "Fred", namespaceKeywordLength: 23),
                        SpanCodeGenerator.Null
                    },
                    {
                        new AddImportCodeGenerator(ns: "Fred", namespaceKeywordLength: 23),
                        new StatementCodeGenerator()
                    },
                    {
                        // Different Namespace.
                        new AddImportCodeGenerator(ns: "Fred", namespaceKeywordLength: 3),
                        new AddImportCodeGenerator(ns: "Ginger", namespaceKeywordLength: 3)
                    },
                    {
                        // Different Namespace (case sensitive).
                        new AddImportCodeGenerator(ns: "fred", namespaceKeywordLength: 9),
                        new AddImportCodeGenerator(ns: "FRED", namespaceKeywordLength: 9)
                    },
                    {
                        // Different NamespaceKeywordLength.
                        new AddImportCodeGenerator(ns: null, namespaceKeywordLength: 0),
                        new AddImportCodeGenerator(ns: null, namespaceKeywordLength: 23)
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(MatchingTestDataSet))]
        public void Equals_True_WhenExpected(AddImportCodeGenerator leftObject, AddImportCodeGenerator rightObject)
        {
            // Arrange & Act
            var result = leftObject.Equals(rightObject);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [MemberData(nameof(NonMatchingTestDataSet))]
        public void Equals_False_WhenExpected(AddImportCodeGenerator leftObject, object rightObject)
        {
            // Arrange & Act
            var result = leftObject.Equals(rightObject);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [MemberData(nameof(MatchingTestDataSet))]
        public void GetHashCode_ReturnsSameValue_WhenEqual(
            AddImportCodeGenerator leftObject,
            AddImportCodeGenerator rightObject)
        {
            // Arrange & Act
            var leftResult = leftObject.GetHashCode();
            var rightResult = rightObject.GetHashCode();

            // Assert
            Assert.Equal(leftResult, rightResult);
        }
    }
}
