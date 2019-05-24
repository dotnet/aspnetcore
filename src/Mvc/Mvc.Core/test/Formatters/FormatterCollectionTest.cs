// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    public class FormatterCollectionTest
    {
        [Fact]
        public void NonGenericRemoveType_RemovesAllOfType()
        {
            // Arrange
            var collection = new FormatterCollection<IOutputFormatter>
            {
                new TestOutputFormatter(),
                new AnotherTestOutputFormatter(),
                new TestOutputFormatter()
            };

            // Act
            collection.RemoveType(typeof(TestOutputFormatter));

            // Assert
            var formatter = Assert.Single(collection);
            Assert.IsType<AnotherTestOutputFormatter>(formatter);
        }

        [Fact]
        public void RemoveType_RemovesAllOfType()
        {
            // Arrange
            var collection = new FormatterCollection<IOutputFormatter>
            {
                new TestOutputFormatter(),
                new AnotherTestOutputFormatter(),
                new TestOutputFormatter()
            };

            // Act
            collection.RemoveType<TestOutputFormatter>();

            // Assert
            var formatter = Assert.Single(collection);
            Assert.IsType<AnotherTestOutputFormatter>(formatter);
        }

        private class TestOutputFormatter : TextOutputFormatter
        {
            public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
            {
                throw new NotImplementedException();
            }
        }

        private class AnotherTestOutputFormatter : TextOutputFormatter
        {
            public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
            {
                throw new NotImplementedException();
            }
        }
    }
}
