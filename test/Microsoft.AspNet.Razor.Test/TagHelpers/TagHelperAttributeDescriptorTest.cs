// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Razor.TagHelpers;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.TagHelpers
{
    public class TagHelperAttributeDescriptorTest
    {
        public static TheoryData IsStringPropertyData
        {
            get
            {
                // attributeType, isIndexer, expectedIsStringProperty
                return new TheoryData<Type, bool, bool>
                {
                    { typeof(int), false, false },
                    { typeof(string), false, true },
                    { typeof(string), true, true },
                    { typeof(object), false, false },
                    { typeof(IEnumerable<string>), false, false },
                    { typeof(IDictionary<string, string>), false, false },
                    { typeof(IDictionary<string, string>), true, false },
                };
            }
        }

        [Theory]
        [MemberData(nameof(IsStringPropertyData))]
        public void TagHelperAttributeDescriptor_IsStringPropertySetCorrectly(
            Type attributeType,
            bool isIndexer,
            bool expectedIsStringProperty)
        {
            // Arrange
            var attributeDescriptor = new TagHelperAttributeDescriptor
            {
                Name = "someAttribute",
                PropertyName = "someProperty",
                TypeName = attributeType.FullName,
                IsIndexer = isIndexer
            };

            // Assert
            Assert.Equal(expectedIsStringProperty, attributeDescriptor.IsStringProperty);
        }
    }
}
