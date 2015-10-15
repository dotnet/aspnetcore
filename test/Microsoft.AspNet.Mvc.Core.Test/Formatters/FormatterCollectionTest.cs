// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.Formatters
{
    public class FormatterCollectionTest
    {
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
            Assert.IsType(typeof(AnotherTestOutputFormatter), formatter);
        }

        private class TestOutputFormatter : OutputFormatter
        {
            public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
            {
                throw new NotImplementedException();
            }
        }

        private class AnotherTestOutputFormatter : OutputFormatter
        {
            public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
            {
                throw new NotImplementedException();
            }
        }
    }
}
