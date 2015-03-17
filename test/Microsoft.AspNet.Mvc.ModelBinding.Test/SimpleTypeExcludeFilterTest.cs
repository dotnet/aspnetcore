// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class SimpleTypeExcluceFilterTest
    {
        [Theory]
        [MemberData(nameof(ExcludedTypes))]
        public void SimpleTypeExcluceFilter_ExcludedTypes(Type type)
        {
            // Arrange
            var filter = new SimpleTypesExcludeFilter();

            // Act & Assert
            Assert.True(filter.IsTypeExcluded(type));
        }

        [Theory]
        [MemberData(nameof(IncludedTypes))]
        public void SimpleTypeExcluceFilter_IncludedTypes(Type type)
        {
            // Arrange
            var filter = new SimpleTypesExcludeFilter();

            // Act & Assert
            Assert.False(filter.IsTypeExcluded(type));
        }

        private class TestType
        {

        }

        public static TheoryData<Type> ExcludedTypes
        {
            get
            {
                return new TheoryData<Type>()
                {
                    // Simple types
                    typeof(int[]),
                    typeof(int),
                    typeof(List<decimal>),
                    typeof(SortedSet<int>),

                    // Nullable types
                    typeof(ICollection<string>),
                    typeof(int?[]),
                    typeof(SortedSet<int?>),
                    typeof(HashSet<Uri>),
                    typeof(HashSet<string>),

                    // Value types
                    typeof(IList<DateTime>),

                    // KeyValue types
                    typeof(Dictionary<int, string>),
                    typeof(IReadOnlyDictionary<int?, char>)
                };
            }
        }

        public static TheoryData<Type> IncludedTypes
        {
            get
            {
                return new TheoryData<Type>()
                {
                    typeof(TestType),
                    typeof(TestType[]),
                    typeof(SortedSet<TestType>),
                    typeof(Dictionary<int, TestType>),
                    typeof(Dictionary<TestType, int>),
                    typeof(Dictionary<TestType, TestType>)
                };
            }
        }
    }
}
