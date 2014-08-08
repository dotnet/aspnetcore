// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.OptionDescriptors;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class InputFormatterDescriptorTest
    {
        [Fact]
        public void ConstructorThrows_IfTypeIsNotInputFormatter()
        {
            // Arrange
            var expected = "The type 'System.String' must derive from " +
                            "'Microsoft.AspNet.Mvc.IInputFormatter'.";

            var type = typeof(string);

            // Act & Assert
            ExceptionAssert.ThrowsArgument(() => new InputFormatterDescriptor(type), "type", expected);
        }

        [Fact]
        public void ConstructorSets_InputFormatterType()
        {
            // Arrange
            var type = typeof(TestInputFormatter);

            // Act
            var descriptor = new InputFormatterDescriptor(type);

            // Assert
            Assert.Equal(type, descriptor.OptionType);
            Assert.Null(descriptor.Instance);
        }

        [Fact]
        public void ConstructorSets_InputFormatterInstanceAndType()
        {
            // Arrange
            var testFormatter = new TestInputFormatter();

            // Act
            var descriptor = new InputFormatterDescriptor(testFormatter);

            // Assert
            Assert.Same(testFormatter, descriptor.Instance);
            Assert.Equal(testFormatter.GetType(), descriptor.OptionType);
        }

        private class TestInputFormatter : IInputFormatter
        {
            public bool CanRead(InputFormatterContext context)
            {
                throw new NotImplementedException();
            }

            public Task<object> ReadAsync(InputFormatterContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}