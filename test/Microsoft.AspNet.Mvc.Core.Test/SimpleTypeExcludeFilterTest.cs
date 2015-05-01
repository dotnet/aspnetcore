// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
    public class SimpleTypeExcludeFilterTest
    {
        [Theory]
        [MemberData(nameof(ExcludedTypes))]
        public void SimpleTypeExcludeFilter_ExcludedTypes(Type type)
        {
            // Arrange
            var filter = new SimpleTypesExcludeFilter();

            // Act & Assert
            Assert.True(filter.IsTypeExcluded(type));
        }

        [Theory]
        [MemberData(nameof(IncludedTypes))]
        public void SimpleTypeExcludeFilter_IncludedTypes(Type type)
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
                    typeof(int),
                    typeof(DateTime),

                    // Nullable types
                    typeof(int?),

                    // KeyValue types
                    typeof(KeyValuePair<string, string>)
                };
            }
        }

        public static TheoryData<Type> IncludedTypes
        {
            get
            {
                return new TheoryData<Type>()
                {
                    // Enumerable types
                    typeof(int[]),
                    typeof(List<decimal>),
                    typeof(SortedSet<int>),
                    typeof(ICollection<string>),
                    typeof(int?[]),
                    typeof(SortedSet<int?>),
                    typeof(HashSet<Uri>),
                    typeof(HashSet<string>),
                    typeof(IList<DateTime>),
                    typeof(Dictionary<int, string>),
                    typeof(IReadOnlyDictionary<int?, char>),
                    
                    // Complex types
                    typeof(TestType),
                    typeof(TestType[]),
                    typeof(SortedSet<TestType>),
                    typeof(Dictionary<int, TestType>),
                    typeof(Dictionary<TestType, int>),
                    typeof(Dictionary<TestType, TestType>),
                    typeof(KeyValuePair<string, TestType>)
                };
            }
        }
    }
}
