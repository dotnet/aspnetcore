// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;
using Microsoft.AspNet.Mvc.OptionDescriptors;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core
{
    public class OutputFormatterDescriptorTest
    {
        [Fact]
        public void ConstructorThrows_IfTypeIsNotOutputFormatter()
        {
            // Arrange
            var expected = "The type 'System.String' must derive from " +
                            "'Microsoft.AspNet.Mvc.IOutputFormatter'.";

            var type = typeof(string);

            // Act & Assert
            ExceptionAssert.ThrowsArgument(() => new OutputFormatterDescriptor(type), "type", expected);
        }

         [Fact]
        public void ConstructorSets_OutputFormatterType()
        {
            // Arrange
            var type = typeof(TestOutputFormatter);

            // Act
            var descriptor = new OutputFormatterDescriptor(type);

            // Assert
            Assert.Equal(type, descriptor.OptionType);
            Assert.Null(descriptor.Instance);
        }

        [Fact]
        public void ConstructorSets_OutputFormatterInsnaceAndType()
        {
            // Arrange
            var testFormatter = new TestOutputFormatter();

            // Act
            var descriptor = new OutputFormatterDescriptor(testFormatter);

            // Assert
            Assert.Same(testFormatter, descriptor.Instance);
            Assert.Equal(testFormatter.GetType(), descriptor.OptionType);
        }

        private class TestOutputFormatter : IOutputFormatter
        {
            public bool CanWriteResult(OutputFormatterContext context, MediaTypeHeaderValue contentType)
            {
                throw new NotImplementedException();
            }

            public IReadOnlyList<MediaTypeHeaderValue> GetSupportedContentTypes(Type dataType,
                                                                                MediaTypeHeaderValue contentType)
            {
                return null;
            }

            public Task WriteAsync(OutputFormatterContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}