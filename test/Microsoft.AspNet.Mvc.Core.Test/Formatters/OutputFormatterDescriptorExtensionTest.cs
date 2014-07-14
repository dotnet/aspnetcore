// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45
using System;
using System.Collections.Generic;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class OutputFormatterDescriptorExtensionTest
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(5)]
        public void Insert_WithType_ThrowsIfIndexIsOutOfBounds(int index)
        {
            // Arrange
            var collection = new List<OutputFormatterDescriptor>
            {
                new OutputFormatterDescriptor(Mock.Of<OutputFormatter>()),
                new OutputFormatterDescriptor(Mock.Of<OutputFormatter>())
            };

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>("index", 
                                                       () => collection.Insert(index, typeof(OutputFormatter)));
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(3)]
        public void Insert_WithInstance_ThrowsIfIndexIsOutOfBounds(int index)
        {
            // Arrange
            var collection = new List<OutputFormatterDescriptor>
            {
                new OutputFormatterDescriptor(Mock.Of<OutputFormatter>()),
                new OutputFormatterDescriptor(Mock.Of<OutputFormatter>())
            };
            var formatter = Mock.Of<OutputFormatter>();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>("index", () => collection.Insert(index, formatter));
        }

        [InlineData]
        public void OutputFormatterDescriptors_AddsTypesAndInstances()
        {
            // Arrange
            var formatter1 = Mock.Of<OutputFormatter>();
            var formatter2 = Mock.Of<OutputFormatter>();
            var type1 = typeof(JsonOutputFormatter);
            var type2 = typeof(OutputFormatter);
            var collection = new List<OutputFormatterDescriptor>();

            // Act
            collection.Add(formatter1);
            collection.Insert(1, formatter2);
            collection.Add(type1);
            collection.Insert(2, type2);

            // Assert
            Assert.Equal(4, collection.Count);
            Assert.Equal(formatter1, collection[0].OutputFormatter);
            Assert.Equal(formatter2, collection[1].OutputFormatter);
            Assert.Equal(type2, collection[2].OutputFormatterType);
            Assert.Equal(type1, collection[3].OutputFormatterType);
        }
    }
}
#endif