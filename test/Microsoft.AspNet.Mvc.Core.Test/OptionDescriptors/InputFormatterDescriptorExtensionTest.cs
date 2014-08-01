// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.OptionDescriptors;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class InputFormatterDescriptorExtensionTest
    {
        [Theory]
        [InlineData(-1)]
        [InlineData(5)]
        public void Insert_WithType_ThrowsIfIndexIsOutOfBounds(int index)
        {
            // Arrange
            var collection = new List<InputFormatterDescriptor>
            {
                new InputFormatterDescriptor(Mock.Of<IInputFormatter>()),
                new InputFormatterDescriptor(Mock.Of<IInputFormatter>())
            };

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>("index", 
                                                       () => collection.Insert(index, typeof(IInputFormatter)));
        }

        [Theory]
        [InlineData(-2)]
        [InlineData(3)]
        public void Insert_WithInstance_ThrowsIfIndexIsOutOfBounds(int index)
        {
            // Arrange
            var collection = new List<InputFormatterDescriptor>
            {
                new InputFormatterDescriptor(Mock.Of<IInputFormatter>()),
                new InputFormatterDescriptor(Mock.Of<IInputFormatter>())
            };
            var formatter = Mock.Of<IInputFormatter>();

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>("index", () => collection.Insert(index, formatter));
        }

        [InlineData]
        public void InputFormatterDescriptors_AddsTypesAndInstances()
        {
            // Arrange
            var formatter1 = Mock.Of<IInputFormatter>();
            var formatter2 = Mock.Of<IInputFormatter>();
            var type1 = typeof(JsonInputFormatter);
            var type2 = typeof(IInputFormatter);
            var collection = new List<InputFormatterDescriptor>();

            // Act
            collection.Add(formatter1);
            collection.Insert(1, formatter2);
            collection.Add(type1);
            collection.Insert(2, type2);

            // Assert
            Assert.Equal(4, collection.Count);
            Assert.Equal(formatter1, collection[0].Instance);
            Assert.Equal(formatter2, collection[1].Instance);
            Assert.Equal(type2, collection[2].OptionType);
            Assert.Equal(type1, collection[3].OptionType);
        }
    }
}
